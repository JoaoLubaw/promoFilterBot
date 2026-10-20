/*
 * File: ReaderWorker.cs
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

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using PromoFilterBot.Infrastructure.Interfaces;
using PromoFilterBot.Reader.Common;
using PromoFilterBot.Reader.Interfaces;
using PromoFilterBot.Reader.Options;

using TL;

namespace PromoFilterBot.Reader.BackgroundServices;

/// <summary>
/// Keeps the reader account connected and its posts flowing to the matcher, reconnecting when the connection is lost.
/// </summary>
[ExcludeFromCodeCoverage]
public class ReaderWorker : BackgroundService {
    private readonly IReaderSession _readerSession;
    private readonly IReaderUpdateHandler _updateHandler;
    private readonly IActiveChannelCache _activeChannelCache;
    private readonly IReaderStatusService _readerStatusService;
    private readonly IBotNotifier _botNotifier;
    private readonly ReaderOptions _options;
    private readonly ILogger<ReaderWorker> _logger;

    public ReaderWorker(
        IReaderSession readerSession,
        IReaderUpdateHandler updateHandler,
        IActiveChannelCache activeChannelCache,
        IReaderStatusService readerStatusService,
        IBotNotifier botNotifier,
        IOptions<ReaderOptions> options,
        ILogger<ReaderWorker> logger) {
        _readerSession = readerSession;
        _updateHandler = updateHandler;
        _activeChannelCache = activeChannelCache;
        _readerStatusService = readerStatusService;
        _botNotifier = botNotifier;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        if (!_options.Enabled || !_options.IsConfigured) {
            _logger.LogWarning("The reader account is disabled or not configured (Reader:ApiId, ApiHash, PhoneNumber). No channel is read.");
            _readerStatusService.ReportDisabled();
            return;
        }

        WTelegramLogging.RedirectTo(_logger);

        // Without a session, Telegram would send a login code to the phone of the account on every attempt
        if (!File.Exists(_options.SessionPath)) {
            await ReportLoginRequiredAsync($"No session file in {_options.SessionPath}", stoppingToken);
            return;
        }

        Task refreshLoop = RefreshActiveChannelsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested) {
            TimeSpan retryDelay = TimeSpan.FromSeconds(_options.ReconnectDelaySeconds);

            try {
                await _activeChannelCache.RefreshAsync(stoppingToken);

                User user = await _readerSession.ConnectAsync(_updateHandler.HandleAsync, _activeChannelCache.Peers, stoppingToken);

                _readerStatusService.ReportConnected(DisplayName(user));
                _logger.LogInformation("Reader account connected as {Account}", DisplayName(user));

                await StayConnectedAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                break;
            }
            catch (Exception ex) when (IsLoginRequired(ex)) {
                // The session was revoked: each new attempt would ask Telegram for another code, so the reader stops
                // until someone logs in again and restarts the app
                await _readerSession.DisconnectAsync();
                await ReportLoginRequiredAsync(ex.Message, stoppingToken);
                break;
            }
            catch (RpcException ex) when (ex.Code == 420) {
                _logger.LogWarning("FLOOD_WAIT of {Seconds}s while connecting the reader account", ex.X);
                _readerStatusService.ReportFloodWait(ex.X);
                _readerStatusService.ReportDisconnected($"FLOOD_WAIT de {ex.X}s ao conectar");

                retryDelay = TimeSpan.FromSeconds(Math.Max(ex.X, _options.ReconnectDelaySeconds));
            }
            catch (Exception ex) {
                _logger.LogError(ex, "The reader account failed");
                _readerStatusService.ReportDisconnected(ex.Message);
            }

            await _readerSession.DisconnectAsync();

            try {
                await Task.Delay(retryDelay, stoppingToken);
            }
            catch (OperationCanceledException) {
                break;
            }
        }

        await _readerSession.DisconnectAsync();
        await refreshLoop.ContinueWith(_ => { }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
    }

    private async Task ReportLoginRequiredAsync(string reason, CancellationToken stoppingToken) {
        _logger.LogError("The reader account needs an interactive login, so it stays off until the app restarts: {Reason}", reason);
        _readerStatusService.ReportDisconnected(reason, loginRequired: true);

        await _botNotifier.NotifyAdminAsync(
            "🔐 <b>A conta leitora precisa de login.</b>\nA sessão não existe ou foi encerrada, e o leitor ficou desligado. " +
            "Na VM, rode <code>docker compose run --rm app --login</code> e depois <code>docker compose restart app</code>.",
            stoppingToken);
    }

    private static bool IsLoginRequired(Exception exception) {
        return exception switch {
            ReaderLoginRequiredException => true,
            AggregateException aggregate => aggregate.InnerExceptions.Any(IsLoginRequired),
            _ => exception.InnerException != null && IsLoginRequired(exception.InnerException)
        };
    }

    // Saves the update state from time to time while connected, until the connection is lost
    private async Task StayConnectedAsync(CancellationToken stoppingToken) {
        Task disconnected = _readerSession.WaitForDisconnectionAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested) {
            Task tick = Task.Delay(TimeSpan.FromSeconds(_options.StateSaveIntervalSeconds), stoppingToken);
            Task finished = await Task.WhenAny(disconnected, tick);

            _readerSession.SaveState();

            if (finished == disconnected && !stoppingToken.IsCancellationRequested) {
                _readerStatusService.ReportDisconnected("Conexão perdida");
                return;
            }
        }
    }

    private async Task RefreshActiveChannelsAsync(CancellationToken stoppingToken) {
        while (!stoppingToken.IsCancellationRequested) {
            try {
                await Task.Delay(TimeSpan.FromSeconds(_options.ActiveChannelsRefreshSeconds), stoppingToken);
                await _activeChannelCache.RefreshAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                return;
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "The monitored channels could not be refreshed");
            }
        }
    }

    private static string DisplayName(User user) {
        string name = $"{user.first_name} {user.last_name}".Trim();

        return string.IsNullOrEmpty(user.MainUsername) ? name : $"{name} (@{user.MainUsername})";
    }
}