/*
 * File: INotificationQueue.cs
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

namespace PromoFilterBot.Infrastructure.Interfaces;

/// <summary>
/// Ids of the notifications saved by the matcher, waiting for the notifier. The notifications themselves are in the
/// database, so the ones pending when the app stops are queued again when it starts.
/// </summary>
public interface INotificationQueue {
    void Enqueue(long notificationId);

    IAsyncEnumerable<long> ReadAllAsync(CancellationToken cancellationToken = default);
}