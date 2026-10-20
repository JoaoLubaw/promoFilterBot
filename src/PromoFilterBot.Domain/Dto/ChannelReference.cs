/*
 * File: ChannelReference.cs
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

namespace PromoFilterBot.Domain.Dto;

/// <summary>
/// Username of a public channel (lower case, without '@'), or null when it could not be read.
/// </summary>
public sealed record ChannelReference(string? Username, bool IsPrivateLink) {
    public static ChannelReference Invalid { get; } = new ChannelReference(null, false);
}