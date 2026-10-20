/*
 * File: AlertIndexProvider.cs
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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using PromoFilterBot.Domain.Common;
using PromoFilterBot.Domain.Models;
using PromoFilterBot.Infrastructure.Common;
using PromoFilterBot.Infrastructure.Interfaces;
using PromoFilterBot.Infrastructure.Options;

namespace PromoFilterBot.Infrastructure.Services;

/// <summary>
/// Singleton holding the alert index. Rebuilt on the next offer after an alert changes, and periodically, in case a
/// change was not reported (an alert edited straight in the database).
/// </summary>
public sealed class AlertIndexProvider : IAlertIndexProvider, IDisposable {
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly MatchingOptions _matchingOptions;
    private readonly ILogger<AlertIndexProvider> _logger;
    private readonly SemaphoreSlim _buildLock = new SemaphoreSlim(1, 1);

    private volatile AlertIndex? _index;
    private volatile bool _dirty = true;
    private DateTime _builtAt = DateTime.MinValue;

    public AlertIndexProvider(
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        IOptions<MatchingOptions> matchingOptions,
        ILogger<AlertIndexProvider> logger) {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _matchingOptions = matchingOptions.Value;
        _logger = logger;
    }

    public async Task<AlertIndex> GetIndexAsync(CancellationToken cancellationToken = default) {
        AlertIndex? index = _index;

        if (index != null && !IsStale()) return index;

        await _buildLock.WaitAsync(cancellationToken);

        try {
            if (_index != null && !IsStale()) return _index;

            // Cleared before reading, so a change made while the index is built marks it dirty again
            _dirty = false;

            using IServiceScope scope = _scopeFactory.CreateScope();
            IAlertRepository alertRepository = scope.ServiceProvider.GetRequiredService<IAlertRepository>();

            List<Alert> alerts = await alertRepository.GetIndexableAlertsAsync();

            _index = AlertIndex.Build(alerts.Select(a => a.ToIndexedAlert()));
            _builtAt = _timeProvider.GetUtcNow().UtcDateTime;

            _logger.LogInformation("Alert index built with {Count} alerts", _index.Count);

            return _index;
        }
        catch {
            _dirty = true;
            throw;
        }
        finally {
            _buildLock.Release();
        }
    }

    public void Invalidate() {
        _dirty = true;
    }

    public void Dispose() {
        _buildLock.Dispose();
    }

    private bool IsStale() {
        TimeSpan refresh = TimeSpan.FromMinutes(_matchingOptions.AlertIndexRefreshMinutes);

        return _dirty || _timeProvider.GetUtcNow().UtcDateTime - _builtAt > refresh;
    }
}