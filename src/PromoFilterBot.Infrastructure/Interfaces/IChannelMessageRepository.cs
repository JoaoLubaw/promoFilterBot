/*
 * File: IChannelMessageRepository.cs
 * Project: Interfaces
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

namespace PromoFilterBot.Infrastructure.Interfaces;

public interface IChannelMessageRepository {
    Task<bool> ChannelMessageExistsAsync(int channelId, int telegramMessageId);

    Task<ChannelMessage> CreateChannelMessageAsync(ChannelMessage channelMessage);

    Task<int> CountChannelMessagesSinceAsync(DateTime since);

    Task<DateTime?> GetLastReceivedAtAsync();

    // Retention: deletes the messages received before the limit, and their notifications.
    Task<int> DeleteChannelMessagesOlderThanAsync(DateTime limit);
}