/*
 * File: LimitsOptions.cs
 * Project: Options
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

namespace PromoFilterBot.Infrastructure.Options;

/// <summary>
/// Business limits, adjustable in appsettings without a new build.
/// </summary>
public class LimitsOptions {
    public const string SectionName = "Limits";

    public int MaxActiveAlertsPerUser { get; set; } = 10;

    /// <summary>
    /// Active and paused alerts together, so /alertas stays readable.
    /// </summary>
    public int MaxAlertsPerUser { get; set; } = 30;

    public int MaxAlertTextLength { get; set; } = 200;

    public decimal MaxPriceLimit { get; set; } = 1_000_000m;

    public int MessageRetentionDays { get; set; } = 30;

    /// <summary>
    /// A guided flow left unanswered for longer starts over.
    /// </summary>
    public int ConversationTimeoutHours { get; set; } = 24;
}