/*
 * File: TelegramRateLimiter.cs
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

using Microsoft.Extensions.Options;

using PromoFilterBot.Bot.Interfaces;
using PromoFilterBot.Bot.Options;

namespace PromoFilterBot.Bot.Services;

/// <summary>
/// Keeps the bot within the limits of Telegram: about 30 messages per second in total and one per second to the same
/// chat. Over them, Telegram answers 429 and makes the bot wait longer.
/// </summary>
public sealed class TelegramRateLimiter : ITelegramRateLimiter {
    private const int MaxTrackedChats = 10_000;
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(1);

    private readonly TimeProvider _timeProvider;
    private readonly int _globalMessagesPerSecond;
    private readonly TimeSpan _perChatInterval;
    private readonly Queue<DateTimeOffset> _recentSends = new Queue<DateTimeOffset>();
    private readonly Dictionary<long, DateTimeOffset> _nextSendByChat = new Dictionary<long, DateTimeOffset>();
    private readonly object _lock = new object();

    public TelegramRateLimiter(TimeProvider timeProvider, IOptions<BotOptions> options) {
        _timeProvider = timeProvider;
        _globalMessagesPerSecond = Math.Max(1, options.Value.GlobalMessagesPerSecond);
        _perChatInterval = TimeSpan.FromMilliseconds(Math.Max(0, options.Value.PerChatIntervalMilliseconds));
    }

    public async Task WaitAsync(long chatId, CancellationToken cancellationToken = default) {
        while (true) {
            TimeSpan wait = TryAcquire(chatId);

            if (wait <= TimeSpan.Zero) return;

            await Task.Delay(wait, _timeProvider, cancellationToken);
        }
    }

    /// <summary>
    /// Takes a slot for the chat and returns zero, or returns how long to wait before trying again.
    /// </summary>
    internal TimeSpan TryAcquire(long chatId) {
        lock (_lock) {
            DateTimeOffset now = _timeProvider.GetUtcNow();

            while (_recentSends.Count > 0 && now - _recentSends.Peek() >= Window) _recentSends.Dequeue();

            TimeSpan globalWait = _recentSends.Count < _globalMessagesPerSecond ? TimeSpan.Zero : _recentSends.Peek() + Window - now;
            TimeSpan chatWait = _nextSendByChat.TryGetValue(chatId, out DateTimeOffset next) && next > now ? next - now : TimeSpan.Zero;
            TimeSpan wait = globalWait > chatWait ? globalWait : chatWait;

            if (wait > TimeSpan.Zero) return wait;

            _recentSends.Enqueue(now);
            _nextSendByChat[chatId] = now + _perChatInterval;

            if (_nextSendByChat.Count > MaxTrackedChats) {
                foreach (long idle in _nextSendByChat.Where(c => c.Value <= now).Select(c => c.Key).ToList()) _nextSendByChat.Remove(idle);
            }

            return TimeSpan.Zero;
        }
    }
}