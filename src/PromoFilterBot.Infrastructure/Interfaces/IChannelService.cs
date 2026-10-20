/*
 * File: IChannelService.cs
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
using PromoFilterBot.Infrastructure.Common;
using PromoFilterBot.Infrastructure.Dto;

namespace PromoFilterBot.Infrastructure.Interfaces;

// Only the admin registers channels; the users only have their alerts.
public interface IChannelService {
    // The admin adds one or more public channels (@canal, t.me/canal), separated by spaces, commas or lines. A removed
    // channel comes back. Each one has its own outcome.
    Task<ServiceResult<List<ChannelAdditionDto>>> AddChannelsAsync(long chatId, string? references);

    // The admin stops the reading of a channel.
    Task<ServiceResult<ChannelDto>> RemoveChannelAsync(long chatId, string? reference);

    // Channels for the admin, all of them or the ones in a status.
    Task<ServiceResult<List<ChannelDto>>> GetChannelsAsync(long chatId, ChannelStatus? status);

    /* Reader */
    Task<ChannelDto?> GetNextChannelToJoinAsync();

    Task<ServiceResult<ChannelDto>> MarkChannelJoinedAsync(int channelId, long telegramId, long accessHash, string? title);

    // The reader cannot read the channel (not a public channel, username not found...), so it is blocked with the reason.
    Task<ServiceResult<ChannelDto>> MarkChannelUnavailableAsync(int channelId, string reason);

    Task<List<ChannelPeerDto>> GetActiveChannelPeersAsync();
}