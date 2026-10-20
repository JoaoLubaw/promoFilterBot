/*
 * File: ChannelJoinWorker.cs
 * Project: BackgroundServices
 * Create Date: Tuesday, 20th October 2026
 * Author: João Lubawaski (joaolubawaski@gmail.com)
 * -----
 * Last Modified: Tuesday, 20th October 2026 4:30:00 pm
 * Modified by: João Lubawaski (joaolubawaski@gmail.com)
 * -----
 * Copyright (c) 2026 João Lubawaski
 * ----
 * HISTORY:
 * Date      	By	Comments
 * ----------	---	---------------------------------------------------------
 */

using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using PromoFilterBot.Infrastructure.Common;
using PromoFilterBot.Infrastructure.Dto;
using PromoFilterBot.Infrastructure.Interfaces;
using PromoFilterBot.Reader.Interfaces;
using PromoFilterBot.Reader.Options;

using TL;

using WTelegram;

namespace PromoFilterBot.Reader.BackgroundServices;

/// <summary>
/// Starts reading the channels the admin registered. A channel the account is already in only needs to be looked up;
/// the others are joined slowly (one per interval, with a random delay and a daily cap, the way a person would), then
/// muted and archived, so they do not disturb the account.
/// </summary>
[ExcludeFromCodeCoverage]
public class ChannelJoinWorker : BackgroundService {
    // Archive, in the folders of Telegram
    private const int ArchiveFolderId = 1;

    // "Muted forever" for Telegram: the highest date of its 32-bit timestamps
    private static readonly DateTime MutedForever = DateTime.UnixEpoch.AddSeconds(int.MaxValue);

    private readonly IReaderSession _readerSession;
    private readonly IActiveChannelCache _activeChannelCache;
    private readonly IReaderStatusService _readerStatusService;
    private readonly IBotNotifier _botNotifier;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ReaderOptions _options;
    private readonly ILogger<ChannelJoinWorker> _logger;
    private readonly Queue<DateTime> _recentJoins = new Queue<DateTime>();

    private bool _channelLimitAlertSent;

    public ChannelJoinWorker(
        IReaderSession readerSession,
        IActiveChannelCache activeChannelCache,
        IReaderStatusService readerStatusService,
        IBotNotifier botNotifier,
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        IOptions<ReaderOptions> options,
        ILogger<ChannelJoinWorker> logger) {
        _readerSession = readerSession;
        _activeChannelCache = activeChannelCache;
        _readerStatusService = readerStatusService;
        _botNotifier = botNotifier;
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _options = options.Value;
        _logger = logger;
    }

    private TimeSpan ResolveInterval => TimeSpan.FromSeconds(_options.ResolveIntervalSeconds);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        if (!_options.Enabled || !_options.IsConfigured) return;

        while (!stoppingToken.IsCancellationRequested) {
            TimeSpan delay;

            try {
                Client client = await _readerSession.WaitUntilReadyAsync(stoppingToken);
                delay = await JoinNextChannelAsync(client, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                break;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "The reader account failed to join a channel");
                delay = JoinDelay();
            }

            try {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException) {
                break;
            }
        }
    }

    // Returns how long to wait for the next channel: a short while when nothing was joined, the join interval after a
    // join, and what Telegram asked for on a FLOOD_WAIT
    private async Task<TimeSpan> JoinNextChannelAsync(Client client, CancellationToken stoppingToken) {
        DateTime now = _timeProvider.GetUtcNow().UtcDateTime;

        while (_recentJoins.Count > 0 && _recentJoins.Peek() < now.AddDays(-1)) _recentJoins.Dequeue();

        using IServiceScope scope = _scopeFactory.CreateScope();
        IChannelService channelService = scope.ServiceProvider.GetRequiredService<IChannelService>();

        ChannelDto? next = await channelService.GetNextChannelToJoinAsync();

        if (next == null) return ResolveInterval;

        if (_activeChannelCache.Count >= _options.MaxChannels) {
            if (!_channelLimitAlertSent) {
                _channelLimitAlertSent = true;
                await _botNotifier.NotifyAdminAsync(
                    $"⚠️ A conta leitora chegou a {_activeChannelCache.Count} canais (limite {_options.MaxChannels}). " +
                    "Os canais novos ficam na fila; remova alguns com /admin remover @canal.", stoppingToken);
            }

            return ResolveInterval;
        }

        try {
            Contacts_ResolvedPeer resolved = await client.Contacts_ResolveUsername(next.ChannelUsername);

            // Only public broadcast channels: the account never enters groups, where people talk
            if (resolved.Chat is not TL.Channel channel || !channel.IsChannel) {
                await MarkUnavailableAsync(channelService, next, "Não é um canal público (é um grupo ou um usuário)", stoppingToken);
                return ResolveInterval;
            }

            bool mustJoin = channel.flags.HasFlag(TL.Channel.Flags.left);

            if (mustJoin && _recentJoins.Count >= _options.MaxJoinsPerDay) {
                _logger.LogDebug("The reader account joined {Count} channels in the last day, waiting", _recentJoins.Count);
                return ResolveInterval;
            }

            if (mustJoin) {
                await client.Channels_JoinChannel(new InputChannel(channel.id, channel.access_hash));
                _recentJoins.Enqueue(now);
                _logger.LogInformation("The reader account joined @{Username}", next.ChannelUsername);

                await QuietChannelAsync(client, channel);
            }

            ServiceResult<ChannelDto> result = await channelService.MarkChannelJoinedAsync(next.ChannelId, channel.id, channel.access_hash, channel.title);
            await _activeChannelCache.RefreshAsync(stoppingToken);

            if (result.IsSuccess) await _botNotifier.NotifyAdminAsync($"📡 Lendo @{next.ChannelUsername}.", stoppingToken);

            return mustJoin ? JoinDelay() : ResolveInterval;
        }
        catch (RpcException ex) when (ex.Code == 420) {
            _logger.LogWarning("FLOOD_WAIT of {Seconds}s while joining @{Username}", ex.X, next.ChannelUsername);
            _readerStatusService.ReportFloodWait(ex.X);

            if (ex.X >= _options.LongFloodWaitAlertSeconds) {
                await _botNotifier.NotifyAdminAsync(
                    $"⏳ O Telegram pediu para a conta leitora esperar {TimeSpan.FromSeconds(ex.X):g} antes de entrar em " +
                    $"@{next.ChannelUsername}. As entradas em canais ficam pausadas até lá.", stoppingToken);
            }

            TimeSpan floodWait = TimeSpan.FromSeconds(ex.X);

            return floodWait > ResolveInterval ? floodWait : ResolveInterval;
        }
        catch (RpcException ex) when (ex.Message is "USERNAME_NOT_OCCUPIED" or "USERNAME_INVALID") {
            await MarkUnavailableAsync(channelService, next, "Esse username não existe no Telegram", stoppingToken);
            return ResolveInterval;
        }
        catch (RpcException ex) when (ex.Message is "CHANNEL_PRIVATE" or "CHANNEL_INVALID" or "INVITE_REQUEST_SENT") {
            await MarkUnavailableAsync(channelService, next, "Canal privado ou que exige aprovação para entrar", stoppingToken);
            return ResolveInterval;
        }
        catch (RpcException ex) when (ex.Message == "CHANNELS_TOO_MUCH") {
            _channelLimitAlertSent = true;
            await _botNotifier.NotifyAdminAsync(
                "⚠️ O Telegram não deixa a conta leitora entrar em mais canais (CHANNELS_TOO_MUCH).", stoppingToken);
            return TimeSpan.FromHours(6);
        }
    }

    // Only the channels the reader joined: the ones the account already followed stay as the person left them
    private async Task QuietChannelAsync(Client client, TL.Channel channel) {
        InputPeerChannel peer = new InputPeerChannel(channel.id, channel.access_hash);

        try {
            if (_options.MuteJoinedChannels) {
                await client.Account_UpdateNotifySettings(
                    new InputNotifyPeer { peer = peer },
                    new InputPeerNotifySettings { flags = InputPeerNotifySettings.Flags.has_mute_until, mute_until = MutedForever });
            }

            if (_options.ArchiveJoinedChannels) {
                await client.Folders_EditPeerFolders(new InputFolderPeer { peer = peer, folder_id = ArchiveFolderId });
            }
        }
        catch (RpcException ex) {
            _logger.LogWarning("Channel {ChannelId} could not be muted or archived: {Error}", channel.id, ex.Message);
        }
    }

    private async Task MarkUnavailableAsync(IChannelService channelService, ChannelDto channel, string reason, CancellationToken stoppingToken) {
        await channelService.MarkChannelUnavailableAsync(channel.ChannelId, reason);
        await _botNotifier.NotifyAdminAsync($"🚫 O canal @{channel.ChannelUsername} não pode ser lido: {reason}.", stoppingToken);
    }

    private TimeSpan JoinDelay() {
        double jitterMinutes = Random.Shared.NextDouble() * _options.JoinJitterMinutes;

        return TimeSpan.FromMinutes(_options.JoinIntervalMinutes + jitterMinutes);
    }
}