/*
 * File: LocalClock.cs
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

using PromoFilterBot.Bot.Common;
using PromoFilterBot.Bot.Interfaces;
using PromoFilterBot.Infrastructure.Common;
using PromoFilterBot.Infrastructure.Options;

namespace PromoFilterBot.Bot.Services;

public sealed class LocalClock : ILocalClock {
    private readonly TimeProvider _timeProvider;
    private readonly TimeZoneInfo _timeZone;

    public LocalClock(TimeProvider timeProvider, IOptions<GeneralOptions> generalOptions) {
        _timeProvider = timeProvider;
        _timeZone = TimeZoneResolver.Resolve(generalOptions.Value.TimeZone);
    }

    public DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

    public string Format(DateTime utc) => Formatters.LocalDateTime(utc, _timeZone);
}