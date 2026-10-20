/*
 * File: AdminStatsDto.cs
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

using PromoFilterBot.Domain.Models;

namespace PromoFilterBot.Infrastructure.Dto;

/// <summary>
/// Numbers of /admin.
/// </summary>
public class AdminStatsDto {
    public int TotalUsers { get; set; }

    public int ActiveUsers { get; set; }

    public int PausedUsers { get; set; }

    public int ActiveAlerts { get; set; }

    public Dictionary<ChannelStatus, int> ChannelsByStatus { get; set; } = [];

    public int MessagesLast24Hours { get; set; }

    public int NotificationsSentLast24Hours { get; set; }

    public int PendingNotifications { get; set; }

    public int NotRelevantFeedbackLast7Days { get; set; }

    public DateTime? LastMessageAt { get; set; }

    public required ReaderStatusDto Reader { get; set; }
}