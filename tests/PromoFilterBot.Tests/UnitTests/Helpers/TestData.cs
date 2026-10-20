/*
 * File: TestData.cs
 * Project: Helpers
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

using PromoFilterBot.Domain.Common;
using PromoFilterBot.Domain.Dto;
using PromoFilterBot.Domain.Models;
using PromoFilterBot.Infrastructure.Dto;

namespace PromoFilterBot.Tests.Helpers;

public static class TestData {
    public const long ChatId = 1001;
    public const long AdminChatId = 9999;

    public static readonly DateTime Now = new DateTime(2026, 10, 20, 15, 0, 0, DateTimeKind.Utc);

    public static User User(int userId = 1, long chatId = ChatId, bool isActive = true, DateTime? pausedUntil = null) {
        return new User {
            UserId = userId,
            UserTelegramChatId = chatId,
            UserName = "João",
            UserIsActive = isActive,
            UserPausedUntil = pausedUntil
        };
    }

    public static UserDto UserDto(long chatId = ChatId, bool isNew = false, DateTime? pausedUntil = null) {
        return new UserDto { UserId = 1, UserChatId = chatId, UserName = "João", UserIsActive = true, UserIsNew = isNew, UserPausedUntil = pausedUntil };
    }

    public static Alert Alert(int alertId, int userId, string text, decimal? maxPrice = null, bool isActive = true) {
        AlertQuery query = AlertQueryParser.Parse(text);

        return new Alert {
            AlertId = alertId,
            UserId = userId,
            AlertOriginalText = text,
            AlertNormalizedTerms = query.StoredRequiredTerms,
            AlertExcludedTerms = query.StoredExcludedTerms,
            AlertMaxPrice = maxPrice,
            AlertIsActive = isActive
        };
    }

    public static Channel Channel(int channelId, string username, ChannelStatus status = ChannelStatus.Active, long? telegramId = null) {
        return new Channel {
            ChannelId = channelId,
            ChannelUsername = username,
            ChannelStatus = status,
            ChannelTelegramId = telegramId
        };
    }

    public static IndexedAlert IndexedAlert(int alertId, int userId, string text, decimal? maxPrice = null) {
        return new IndexedAlert { AlertId = alertId, UserId = userId, Query = AlertQueryParser.Parse(text), MaxPrice = maxPrice };
    }
}