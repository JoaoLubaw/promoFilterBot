/*
 * File: IChannelRepository.cs
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
using PromoFilterBot.Infrastructure.Dto;

namespace PromoFilterBot.Infrastructure.Interfaces;

public interface IChannelRepository {
    // Channels in any of the statuses, ordered by username.
    Task<List<Channel>> GetChannelsAsync(params ChannelStatus[] statuses);

    Task<Channel?> GetChannelByIdAsync(int channelId);

    Task<Channel?> GetChannelByUsernameAsync(string username);

    Task<Channel?> GetChannelByTelegramIdAsync(long telegramId);

    // The approved channel waiting longer for the reader to join it.
    Task<Channel?> GetNextChannelToJoinAsync();

    Task<List<ChannelPeerDto>> GetActiveChannelPeersAsync();

    Task<Dictionary<ChannelStatus, int>> CountChannelsByStatusAsync();

    // A new channel, approved and waiting for the reader account to join it.
    Task<Channel> CreateChannelAsync(string username, string creationUser);

    Task<Channel> SetChannelStatusAsync(Channel channel, ChannelStatus status, string? reason, string currentUser);

    Task<Channel> SetChannelJoinedAsync(Channel channel, long telegramId, long accessHash, string? title, DateTime joinedAt, string currentUser);
}