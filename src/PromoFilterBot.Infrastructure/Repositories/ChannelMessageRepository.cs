/*
 * File: ChannelMessageRepository.cs
 * Project: Repositories
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

using System.Diagnostics.CodeAnalysis;

using Microsoft.EntityFrameworkCore;

using PromoFilterBot.Domain.Models;
using PromoFilterBot.Infrastructure.Interfaces;
using PromoFilterBot.Infrastructure.Models;

namespace PromoFilterBot.Infrastructure.Repositories;

[ExcludeFromCodeCoverage]
public class ChannelMessageRepository : BaseRepository, IChannelMessageRepository {
    private new readonly PromoFilterBotDbContext _dbContext;

    public ChannelMessageRepository(PromoFilterBotDbContext dbContext) : base(dbContext) {
        _dbContext = dbContext;
    }

    public async Task<bool> ChannelMessageExistsAsync(int channelId, int telegramMessageId) {
        return await _dbContext.ChannelMessages.AnyAsync(m => m.ChannelId == channelId && m.ChannelMessageTelegramId == telegramMessageId);
    }

    public async Task<ChannelMessage> CreateChannelMessageAsync(ChannelMessage channelMessage) {
        await _dbContext.ChannelMessages.AddAsync(channelMessage);
        await _dbContext.SaveChangesAsync();

        return channelMessage;
    }

    public async Task<int> CountChannelMessagesSinceAsync(DateTime since) {
        return await _dbContext.ChannelMessages.CountAsync(m => m.ChannelMessageReceivedAt >= since);
    }

    public async Task<DateTime?> GetLastReceivedAtAsync() {
        return await _dbContext.ChannelMessages.MaxAsync(m => (DateTime?)m.ChannelMessageReceivedAt);
    }

    public async Task<int> DeleteChannelMessagesOlderThanAsync(DateTime limit) {
        return await _dbContext.ChannelMessages.Where(m => m.ChannelMessageReceivedAt < limit).ExecuteDeleteAsync();
    }
}