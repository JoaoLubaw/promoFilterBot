/*
 * File: ReaderStatusService.cs
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

using PromoFilterBot.Infrastructure.Dto;
using PromoFilterBot.Infrastructure.Interfaces;

namespace PromoFilterBot.Infrastructure.Services;

/// <summary>
/// Singleton with the state of the reader account. It lives only in memory: a restart begins disconnected.
/// </summary>
public sealed class ReaderStatusService : IReaderStatusService {
    private readonly TimeProvider _timeProvider;
    private readonly object _lock = new object();
    private readonly ReaderStatusDto _status;

    public ReaderStatusService(TimeProvider timeProvider) {
        _timeProvider = timeProvider;
        _status = new ReaderStatusDto { DisconnectedSince = Now() };
    }

    public void ReportDisabled() {
        lock (_lock) {
            _status.IsEnabled = false;
            _status.IsConnected = false;
        }
    }

    public void ReportConnected(string accountName) {
        lock (_lock) {
            _status.IsEnabled = true;
            _status.IsConnected = true;
            _status.LoginRequired = false;
            _status.AccountName = accountName;
            _status.ConnectedSince = Now();
            _status.DisconnectedSince = null;
            _status.LastError = null;
        }
    }

    public void ReportDisconnected(string? error, bool loginRequired = false) {
        lock (_lock) {
            if (_status.IsConnected || _status.DisconnectedSince == null) _status.DisconnectedSince = Now();

            _status.IsConnected = false;
            _status.ConnectedSince = null;
            _status.LoginRequired = loginRequired;
            _status.LastError = error;
        }
    }

    public void ReportMessageReceived(DateTime receivedAt) {
        lock (_lock) {
            _status.LastMessageAt = receivedAt;
        }
    }

    public void ReportFloodWait(int seconds) {
        lock (_lock) {
            _status.LastFloodWaitSeconds = seconds;
            _status.LastFloodWaitAt = Now();
        }
    }

    public ReaderStatusDto GetStatus() {
        lock (_lock) {
            return new ReaderStatusDto {
                IsEnabled = _status.IsEnabled,
                IsConnected = _status.IsConnected,
                LoginRequired = _status.LoginRequired,
                AccountName = _status.AccountName,
                ConnectedSince = _status.ConnectedSince,
                DisconnectedSince = _status.DisconnectedSince,
                LastError = _status.LastError,
                LastMessageAt = _status.LastMessageAt,
                LastFloodWaitSeconds = _status.LastFloodWaitSeconds,
                LastFloodWaitAt = _status.LastFloodWaitAt
            };
        }
    }

    private DateTime Now() => _timeProvider.GetUtcNow().UtcDateTime;
}