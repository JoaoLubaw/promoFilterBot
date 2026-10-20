/*
 * File: TimeZoneResolver.cs
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

namespace PromoFilterBot.Infrastructure.Common;

public static class TimeZoneResolver {
    /// <summary>
    /// The configured time zone, or UTC-3 (Brasília, without daylight saving time since 2019) when the system does not
    /// know it, such as in an image without tzdata.
    /// </summary>
    public static TimeZoneInfo Resolve(string? timeZoneId) {
        if (!string.IsNullOrWhiteSpace(timeZoneId) && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out TimeZoneInfo? timeZone)) {
            return timeZone;
        }

        return TimeZoneInfo.CreateCustomTimeZone("UTC-03", TimeSpan.FromHours(-3), "UTC-03", "UTC-03");
    }
}