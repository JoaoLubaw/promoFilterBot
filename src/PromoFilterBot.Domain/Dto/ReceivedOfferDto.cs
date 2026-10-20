/*
 * File: ReceivedOfferDto.cs
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
/// A post read by the reader account, on its way to the matcher.
/// </summary>
public sealed class ReceivedOfferDto {
    /// <summary>
    /// MTProto id of the channel.
    /// </summary>
    public required long TelegramChannelId { get; init; }

    public required int TelegramMessageId { get; init; }

    public required string Text { get; init; }

    /// <summary>
    /// The text with its formatting, as the HTML the bot sends (see TelegramHtml).
    /// </summary>
    public string? Html { get; init; }

    /// <summary>
    /// Links of the post, including the ones hidden behind words.
    /// </summary>
    public IReadOnlyList<string> Links { get; init; } = [];

    public string? ChannelUsername { get; init; }

    public DateTime ReceivedAt { get; init; } = DateTime.UtcNow;
}