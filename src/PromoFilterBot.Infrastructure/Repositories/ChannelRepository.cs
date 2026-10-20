/*
 * File: ChannelRepository.cs
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
using PromoFilterBot.Infrastructure.Dto;
using PromoFilterBot.Infrastructure.Interfaces;
using PromoFilterBot.Infrastructure.Models;

namespace PromoFilterBot.Infrastructure.Repositories;

[ExcludeFromCodeCoverage]
public class ChannelRepository : BaseRepository, IChannelRepository {
    private new readonly PromoFilterBotDbContext _dbContext;

    public ChannelRepository(PromoFilterBotDbContext dbContext) : base(dbContext) {
        _dbContext = dbContext;
    }

    public async Task<List<Channel>> GetChannelsAsync(params ChannelStatus[] statuses) {
        IQueryable<Channel> query = _dbContext.Channels.Where(c => !c.IsDeleted);

        if (statuses.Length > 0) query = query.Where(c => statuses.Contains(c.ChannelStatus));

        return await query.OrderBy(c => c.ChannelUsername).ToListAsync();
    }

    public async Task<Channel?> GetChannelByIdAsync(int channelId) {
        return await _dbContext.Channels.FirstOrDefaultAsync(c => c.ChannelId == channelId && !c.IsDeleted);
    }

    public async Task<Channel?> GetChannelByUsernameAsync(string username) {
        return await _dbContext.Channels.FirstOrDefaultAsync(c => c.ChannelUsername == username && !c.IsDeleted);
    }

    public async Task<Channel?> GetChannelByTelegramIdAsync(long telegramId) {
        return await _dbContext.Channels.FirstOrDefaultAsync(c => c.ChannelTelegramId == telegramId && !c.IsDeleted);
    }

    public async Task<Channel?> GetNextChannelToJoinAsync() {
        return await _dbContext.Channels
            .Where(c => c.ChannelStatus == ChannelStatus.Approved && !c.IsDeleted)
            .OrderBy(c => c.UpdateTime)
            .FirstOrDefaultAsync();
    }

    public async Task<List<ChannelPeerDto>> GetActiveChannelPeersAsync() {
        return await _dbContext.Channels
            .Where(c => c.ChannelStatus == ChannelStatus.Active && c.ChannelTelegramId != null && c.ChannelAccessHash != null && !c.IsDeleted)
            .Select(c => new ChannelPeerDto(c.ChannelTelegramId!.Value, c.ChannelAccessHash!.Value))
            .ToListAsync();
    }

    public async Task<Dictionary<ChannelStatus, int>> CountChannelsByStatusAsync() {
        return await _dbContext.Channels
            .Where(c => !c.IsDeleted)
            .GroupBy(c => c.ChannelStatus)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Status, g => g.Count);
    }

    public async Task<Channel> CreateChannelAsync(string username, string creationUser) {
        Channel channel = new Channel {
            CreationUser = creationUser,
            UpdateUser = creationUser,
            ChannelUsername = username,
            ChannelStatus = ChannelStatus.Approved
        };

        await _dbContext.Channels.AddAsync(channel);
        await _dbContext.SaveChangesAsync();

        return channel;
    }

    public async Task<Channel> SetChannelStatusAsync(Channel channel, ChannelStatus status, string? reason, string currentUser) {
        channel.ChannelStatus = status;
        channel.ChannelStatusReason = reason;
        channel.UpdateUser = currentUser;

        await _dbContext.SaveChangesAsync();

        return channel;
    }

    public async Task<Channel> SetChannelJoinedAsync(Channel channel, long telegramId, long accessHash, string? title, DateTime joinedAt, string currentUser) {
        channel.ChannelTelegramId = telegramId;
        channel.ChannelAccessHash = accessHash;
        channel.ChannelTitle = title;
        channel.ChannelJoinedAt = joinedAt;
        channel.ChannelStatus = ChannelStatus.Active;
        channel.ChannelStatusReason = null;
        channel.UpdateUser = currentUser;

        await _dbContext.SaveChangesAsync();

        return channel;
    }
}