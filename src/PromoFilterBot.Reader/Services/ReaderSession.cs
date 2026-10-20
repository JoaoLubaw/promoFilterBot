/*
 * File: ReaderSession.cs
 * Project: Services
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

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using PromoFilterBot.Infrastructure.Dto;
using PromoFilterBot.Reader.Common;
using PromoFilterBot.Reader.Interfaces;
using PromoFilterBot.Reader.Options;

using TL;

using WTelegram;

namespace PromoFilterBot.Reader.Services;

[ExcludeFromCodeCoverage]
public sealed class ReaderSession : IReaderSession, IAsyncDisposable {
    // Telegram answers channels.getChannels for up to 100 channels at a time
    private const int ChannelsPerRequest = 100;

    private readonly ReaderOptions _options;
    private readonly ILogger<ReaderSession> _logger;
    private readonly object _lock = new object();

    private Client? _client;
    private UpdateManager? _updateManager;
    private TaskCompletionSource<Client> _ready = NewReadySource();
    private TaskCompletionSource _disconnected = NewDisconnectedSource();

    public ReaderSession(IOptions<ReaderOptions> options, ILogger<ReaderSession> logger) {
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConnected => _client is { Disconnected: false } && _ready.Task.IsCompletedSuccessfully;

    public async Task<User> ConnectAsync(Func<Update, Task> onUpdate, IReadOnlyCollection<ChannelPeerDto> channels, CancellationToken cancellationToken) {
        await DisconnectAsync();

        ReaderClientConfig.EnsureDirectories(_options);

        Client client = new Client(ReaderClientConfig.CreateHeadless(_options));
        client.FloodRetryThreshold = _options.FloodRetryThresholdSeconds;
        client.OnOther += OnOtherAsync;

        try {
            UpdateManager updateManager = client.WithUpdateManager(onUpdate, _options.UpdatesStatePath);

            User user = await client.LoginUserIfNeeded().WaitAsync(cancellationToken);

            await LoadChannelsAsync(client, updateManager, channels, cancellationToken);

            lock (_lock) {
                _client = client;
                _updateManager = updateManager;
                _disconnected = NewDisconnectedSource();
                _ready.TrySetResult(client);
            }

            return user;
        }
        catch {
            await client.DisposeAsync();
            throw;
        }
    }

    public Task WaitForDisconnectionAsync(CancellationToken cancellationToken) {
        return _disconnected.Task.WaitAsync(cancellationToken);
    }

    public Task<Client> WaitUntilReadyAsync(CancellationToken cancellationToken) {
        return _ready.Task.WaitAsync(cancellationToken);
    }

    public void SaveState() {
        try {
            _updateManager?.SaveState(_options.UpdatesStatePath);
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "The update state of the reader account could not be saved");
        }
    }

    public async Task DisconnectAsync() {
        Client? client;

        lock (_lock) {
            client = _client;
            _client = null;

            if (_ready.Task.IsCompleted) _ready = NewReadySource();
        }

        if (client == null) return;

        SaveState();
        _updateManager = null;
        client.OnOther -= OnOtherAsync;

        await client.DisposeAsync();
    }

    public async ValueTask DisposeAsync() {
        await DisconnectAsync();
    }

    // Gives the update manager the monitored channels, so it can fetch what they posted while the app was down. Only
    // these channels: the dialogs of the account (its private chats) are never listed
    private async Task LoadChannelsAsync(Client client, UpdateManager updateManager, IReadOnlyCollection<ChannelPeerDto> channels, CancellationToken cancellationToken) {
        foreach (ChannelPeerDto[] batch in channels.Chunk(ChannelsPerRequest)) {
            try {
                InputChannelBase[] inputs = batch.Select(c => (InputChannelBase)new InputChannel(c.TelegramId, c.AccessHash)).ToArray();
                Messages_Chats chats = await client.Channels_GetChannels(inputs).WaitAsync(cancellationToken);

                foreach ((long id, ChatBase chat) in chats.chats) updateManager.Chats[id] = chat;
            }
            catch (RpcException ex) {
                // The posts still arrive; only the ones of a downtime may be missed for these channels
                _logger.LogWarning("{Count} monitored channels could not be loaded: {Error}", batch.Length, ex.Message);
            }
        }
    }

    // WTelegramClient reconnects by itself a few times; a ReactorError means it gave up, so the client is replaced
    private Task OnOtherAsync(IObject notification) {
        if (notification is ReactorError reactorError) {
            _logger.LogWarning(reactorError.Exception, "The reader account lost its connection");
            _disconnected.TrySetResult();
        }

        return Task.CompletedTask;
    }

    private static TaskCompletionSource<Client> NewReadySource() => new TaskCompletionSource<Client>(TaskCreationOptions.RunContinuationsAsynchronously);

    private static TaskCompletionSource NewDisconnectedSource() => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
}