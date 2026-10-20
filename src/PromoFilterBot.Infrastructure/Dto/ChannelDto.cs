/*
 * File: ChannelDto.cs
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
using PromoFilterBot.Infrastructure.Common;

namespace PromoFilterBot.Infrastructure.Dto;

public class ChannelDto {
    public int ChannelId { get; set; }

    public required string ChannelUsername { get; set; }

    public string? ChannelTitle { get; set; }

    public ChannelStatus ChannelStatus { get; set; }

    public string? ChannelStatusReason { get; set; }

    public DateTime? ChannelJoinedAt { get; set; }
}

/// <summary>
/// Outcome of one of the channels the admin sent to add.
/// </summary>
public class ChannelAdditionDto {
    /// <summary>
    /// What the admin typed for the channel.
    /// </summary>
    public required string Reference { get; set; }

    public ResultCode Code { get; set; }

    public ChannelDto? Channel { get; set; }

    public bool IsAdded => Code == ResultCode.NO_ERROR;
}

/// <summary>
/// What the reader account needs to reach a monitored channel without resolving its username again.
/// </summary>
public sealed record ChannelPeerDto(long TelegramId, long AccessHash);