/*
 * File: DtoMappings.cs
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

using PromoFilterBot.Domain.Dto;
using PromoFilterBot.Domain.Models;
using PromoFilterBot.Infrastructure.Dto;

namespace PromoFilterBot.Infrastructure.Common;

/// <summary>
/// Model to Dto mappings shared by the services.
/// </summary>
public static class DtoMappings {
    public static UserDto ToDto(this User user, bool isNew = false) {
        return new UserDto {
            UserId = user.UserId,
            UserChatId = user.UserTelegramChatId,
            UserName = user.UserName,
            UserPausedUntil = user.UserPausedUntil,
            UserIsActive = user.UserIsActive,
            UserIsNew = isNew
        };
    }

    public static ChannelDto ToDto(this Channel channel) {
        return new ChannelDto {
            ChannelId = channel.ChannelId,
            ChannelUsername = channel.ChannelUsername,
            ChannelTitle = channel.ChannelTitle,
            ChannelStatus = channel.ChannelStatus,
            ChannelStatusReason = channel.ChannelStatusReason,
            ChannelJoinedAt = channel.ChannelJoinedAt
        };
    }

    public static AlertDto ToDto(this Alert alert, int position) {
        return new AlertDto {
            AlertId = alert.AlertId,
            AlertPosition = position,
            AlertOriginalText = alert.AlertOriginalText,
            AlertDescription = AlertQuery.FromStored(alert.AlertNormalizedTerms, alert.AlertExcludedTerms).Describe(),
            AlertMaxPrice = alert.AlertMaxPrice,
            AlertIsActive = alert.AlertIsActive
        };
    }

    public static IndexedAlert ToIndexedAlert(this Alert alert) {
        return new IndexedAlert {
            AlertId = alert.AlertId,
            UserId = alert.UserId,
            Query = AlertQuery.FromStored(alert.AlertNormalizedTerms, alert.AlertExcludedTerms),
            MaxPrice = alert.AlertMaxPrice
        };
    }

    /// <summary>
    /// Link to the post in its channel.
    /// </summary>
    public static string? ToPostLink(this ChannelMessage message) {
        string? username = message.Channel?.ChannelUsername;

        return string.IsNullOrEmpty(username) ? null : $"https://t.me/{username}/{message.ChannelMessageTelegramId}";
    }
}