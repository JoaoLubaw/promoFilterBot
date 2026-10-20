/*
 * File: PauseRequest.cs
 * Project: Dto
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

namespace PromoFilterBot.Domain.Dto;

public enum PauseKind {
    Resume = 0,
    Duration = 1,
    UntilTomorrow = 2
}

/// <summary>
/// For how long the user wants no notification.
/// </summary>
public sealed record PauseRequest(PauseKind Kind, TimeSpan Duration) {
    public static PauseRequest Resume { get; } = new PauseRequest(PauseKind.Resume, TimeSpan.Zero);

    public static PauseRequest UntilTomorrow { get; } = new PauseRequest(PauseKind.UntilTomorrow, TimeSpan.Zero);

    public static PauseRequest For(TimeSpan duration) => new PauseRequest(PauseKind.Duration, duration);

    /// <summary>
    /// Until when (in UTC) the notifications are paused, or null to resume them. "Até amanhã" is the next day at
    /// <paramref name="morningHour"/>, in the time zone of the users.
    /// </summary>
    public DateTime? ResolveUntil(DateTime nowUtc, TimeZoneInfo timeZone, int morningHour = 8) {
        switch (Kind) {
            case PauseKind.Resume:
                return null;
            case PauseKind.Duration:
                return nowUtc + Duration;
            default:
                DateTime localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc), timeZone);
                DateTime tomorrowMorning = DateTime.SpecifyKind(localNow.Date.AddDays(1).AddHours(morningHour), DateTimeKind.Unspecified);

                return TimeZoneInfo.ConvertTimeToUtc(tomorrowMorning, timeZone);
        }
    }
}