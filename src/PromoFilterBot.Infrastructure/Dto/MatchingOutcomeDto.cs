/*
 * File: MatchingOutcomeDto.cs
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

namespace PromoFilterBot.Infrastructure.Dto;

/// <summary>
/// What the matcher did with an offer.
/// </summary>
public class MatchingOutcomeDto {
    public long ChannelMessageId { get; set; }

    public required string ProductKey { get; set; }

    public string Store { get; set; } = string.Empty;

    public decimal? DetectedPrice { get; set; }

    public string Coupon { get; set; } = string.Empty;

    /// <summary>
    /// Alerts the offer matched, before leaving out paused users and the users who got the same offer today.
    /// </summary>
    public int MatchedAlerts { get; set; }

    public List<long> NotificationIds { get; set; } = [];
}