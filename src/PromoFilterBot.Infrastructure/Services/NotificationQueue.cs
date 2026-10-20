/*
 * File: NotificationQueue.cs
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

using System.Threading.Channels;

using PromoFilterBot.Infrastructure.Interfaces;

namespace PromoFilterBot.Infrastructure.Services;

/// <summary>
/// Unbounded: it only holds ids, and the matcher must never wait for the rate limits of the notifier.
/// </summary>
public sealed class NotificationQueue : INotificationQueue {
    private readonly Channel<long> _channel = Channel.CreateUnbounded<long>(new UnboundedChannelOptions { SingleReader = false });

    public void Enqueue(long notificationId) {
        _channel.Writer.TryWrite(notificationId);
    }

    public IAsyncEnumerable<long> ReadAllAsync(CancellationToken cancellationToken = default) {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}