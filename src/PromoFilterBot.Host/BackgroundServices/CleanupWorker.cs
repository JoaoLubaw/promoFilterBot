/*
 * File: CleanupWorker.cs
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

using PromoFilterBot.Host.Options;
using PromoFilterBot.Infrastructure.Interfaces;

namespace PromoFilterBot.Host.BackgroundServices;

/// <summary>
/// Retention: once a day, deletes the posts older than Limits:MessageRetentionDays and the abandoned guided flows.
/// </summary>
[ExcludeFromCodeCoverage]
public class CleanupWorker : BackgroundService {
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MonitoringOptions _options;
    private readonly ILogger<CleanupWorker> _logger;

    public CleanupWorker(IServiceScopeFactory scopeFactory, IOptions<MonitoringOptions> options, ILogger<CleanupWorker> logger) {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        try {
            await Task.Delay(StartupDelay, stoppingToken);

            using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromHours(Math.Max(1, _options.CleanupIntervalHours)));

            do {
                try {
                    using IServiceScope scope = _scopeFactory.CreateScope();
                    IMaintenanceService maintenanceService = scope.ServiceProvider.GetRequiredService<IMaintenanceService>();

                    await maintenanceService.CleanupAsync();
                }
                catch (Exception ex) when (ex is not OperationCanceledException) {
                    _logger.LogError(ex, "The cleanup failed");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
            // Stopping
        }
    }
}