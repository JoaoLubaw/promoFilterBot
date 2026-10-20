/*
 * File: IReaderStatusService.cs
 * Project: Interfaces
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

namespace PromoFilterBot.Infrastructure.Interfaces;

/// <summary>
/// State of the reader account, reported by the reader and read by /admin and the monitoring.
/// </summary>
public interface IReaderStatusService {
    void ReportDisabled();

    void ReportConnected(string accountName);

    void ReportDisconnected(string? error, bool loginRequired = false);

    void ReportMessageReceived(DateTime receivedAt);

    void ReportFloodWait(int seconds);

    ReaderStatusDto GetStatus();
}