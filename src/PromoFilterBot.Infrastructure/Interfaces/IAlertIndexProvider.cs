/*
 * File: IAlertIndexProvider.cs
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

using PromoFilterBot.Domain.Common;

namespace PromoFilterBot.Infrastructure.Interfaces;

/// <summary>
/// Keeps the index of the active alerts in memory, rebuilt when an alert changes.
/// </summary>
public interface IAlertIndexProvider {
    Task<AlertIndex> GetIndexAsync(CancellationToken cancellationToken = default);

    // An alert changed, so the index is rebuilt before the next offer.
    void Invalidate();
}