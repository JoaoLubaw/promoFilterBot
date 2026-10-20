/*
 * File: MonitoringWorker.cs
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

using Microsoft.Extensions.Options;

using PromoFilterBot.Bot.Common;
using PromoFilterBot.Bot.Interfaces;
using PromoFilterBot.Host.Options;
using PromoFilterBot.Infrastructure.Dto;
using PromoFilterBot.Infrastructure.Interfaces;
using PromoFilterBot.Reader.Interfaces;

namespace PromoFilterBot.Host.BackgroundServices;

/// <summary>
/// Tells the admin when the reader account stays disconnected, and when no post arrives from the channels for a long
/// time (heartbeat). Each problem is reported once, and again when it is solved.
/// </summary>
[ExcludeFromCodeCoverage]
public class MonitoringWorker : BackgroundService {
    private readonly IReaderStatusService _readerStatusService;
    private readonly IActiveChannelCache _activeChannelCache;
    private readonly IBotNotifier _botNotifier;
    private readonly ILocalClock _localClock;
    private readonly MonitoringOptions _options;
    private readonly ILogger<MonitoringWorker> _logger;

    private DateTime _startedAt;
    private bool _disconnectionReported;
    private bool _silenceReported;

    public MonitoringWorker(
        IReaderStatusService readerStatusService,
        IActiveChannelCache activeChannelCache,
        IBotNotifier botNotifier,
        ILocalClock localClock,
        IOptions<MonitoringOptions> options,
        ILogger<MonitoringWorker> logger) {
        _readerStatusService = readerStatusService;
        _activeChannelCache = activeChannelCache;
        _botNotifier = botNotifier;
        _localClock = localClock;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        _startedAt = _localClock.UtcNow;

        using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromMinutes(Math.Max(1, _options.CheckIntervalMinutes)));

        try {
            while (await timer.WaitForNextTickAsync(stoppingToken)) {
                try {
                    await CheckAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException) {
                    _logger.LogError(ex, "The monitoring check failed");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
            // Stopping
        }
    }

    private async Task CheckAsync(CancellationToken stoppingToken) {
        ReaderStatusDto status = _readerStatusService.GetStatus();
        DateTime now = _localClock.UtcNow;

        if (!status.IsEnabled) return;

        // The reader itself tells the admin when it needs a login
        bool disconnectedTooLong = !status.IsConnected
            && !status.LoginRequired
            && status.DisconnectedSince.HasValue
            && now - status.DisconnectedSince.Value >= TimeSpan.FromMinutes(_options.DisconnectedAlertMinutes);

        if (disconnectedTooLong && !_disconnectionReported) {
            _disconnectionReported = true;
            string error = string.IsNullOrWhiteSpace(status.LastError) ? string.Empty : $"\nÚltimo erro: {Html.Escape(Html.Truncate(status.LastError, 200))}";

            await _botNotifier.NotifyAdminAsync(
                $"🔴 A conta leitora está desconectada desde {_localClock.Format(status.DisconnectedSince!.Value)}.{error}", stoppingToken);
        }
        else if (status.IsConnected && _disconnectionReported) {
            _disconnectionReported = false;
            await _botNotifier.NotifyAdminAsync("🟢 A conta leitora conectou de novo.", stoppingToken);
        }

        if (!status.IsConnected || _activeChannelCache.Count == 0) return;

        DateTime lastActivity = status.LastMessageAt ?? status.ConnectedSince ?? _startedAt;
        bool silent = now - lastActivity >= TimeSpan.FromHours(_options.SilenceAlertHours);

        if (silent && !_silenceReported) {
            _silenceReported = true;
            string last = status.LastMessageAt.HasValue ? $"a última foi em {_localClock.Format(status.LastMessageAt.Value)}" : "nenhuma desde que o app subiu";

            await _botNotifier.NotifyAdminAsync(
                $"🔕 Nenhuma mensagem dos {_activeChannelCache.Count} canais há mais de {_options.SilenceAlertHours}h ({last}). " +
                "Confira a conta leitora.", stoppingToken);
        }
        else if (!silent && _silenceReported) {
            _silenceReported = false;
            await _botNotifier.NotifyAdminAsync("🔔 As mensagens dos canais voltaram a chegar.", stoppingToken);
        }
    }
}