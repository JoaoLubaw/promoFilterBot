/*
 * File: IActiveChannelCache.cs
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

using PromoFilterBot.Infrastructure.Dto;

namespace PromoFilterBot.Reader.Interfaces;

/// <summary>
/// Channels being monitored, to drop at once the updates of any other chat of the account.
/// </summary>
public interface IActiveChannelCache {
    int Count { get; }

    // What the reader needs to reach each monitored channel.
    IReadOnlyCollection<ChannelPeerDto> Peers { get; }

    bool IsMonitored(long telegramChannelId);

    Task RefreshAsync(CancellationToken cancellationToken = default);
}