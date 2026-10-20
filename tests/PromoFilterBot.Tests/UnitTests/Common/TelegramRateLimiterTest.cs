/*
 * File: TelegramRateLimiterTest.cs
 * Project: Common
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

using PromoFilterBot.Bot.Options;
using PromoFilterBot.Bot.Services;
using PromoFilterBot.Tests.Helpers;

using MsOptions = Microsoft.Extensions.Options.Options;

namespace PromoFilterBot.Tests.Common;

public class TelegramRateLimiterTest {
    private readonly FixedTimeProvider _timeProvider = new(TestData.Now);
    private readonly TelegramRateLimiter _limiter;

    public TelegramRateLimiterTest() {
        BotOptions options = new BotOptions { GlobalMessagesPerSecond = 3, PerChatIntervalMilliseconds = 1000 };

        _limiter = new TelegramRateLimiter(_timeProvider, MsOptions.Create(options));
    }

    [Fact]
    public void TryAcquire_ShouldSpaceTheMessagesToTheSameChat() {
        Assert.Equal(TimeSpan.Zero, _limiter.TryAcquire(1));
        Assert.Equal(TimeSpan.FromSeconds(1), _limiter.TryAcquire(1));

        _timeProvider.Advance(TimeSpan.FromMilliseconds(400));
        Assert.Equal(TimeSpan.FromMilliseconds(600), _limiter.TryAcquire(1));

        _timeProvider.Advance(TimeSpan.FromMilliseconds(600));
        Assert.Equal(TimeSpan.Zero, _limiter.TryAcquire(1));
    }

    [Fact]
    public void TryAcquire_ShouldLimitTheMessagesPerSecond_AcrossChats() {
        Assert.Equal(TimeSpan.Zero, _limiter.TryAcquire(1));

        _timeProvider.Advance(TimeSpan.FromMilliseconds(400));
        Assert.Equal(TimeSpan.Zero, _limiter.TryAcquire(2));
        Assert.Equal(TimeSpan.Zero, _limiter.TryAcquire(3));
        Assert.Equal(TimeSpan.FromMilliseconds(600), _limiter.TryAcquire(4));

        _timeProvider.Advance(TimeSpan.FromMilliseconds(600));
        Assert.Equal(TimeSpan.Zero, _limiter.TryAcquire(4));
    }
}