/*
 * File: NotificationDispatchDto.cs
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

namespace PromoFilterBot.Infrastructure.Dto;

public enum DispatchDecision {
    /// <summary>
    /// Send it now.
    /// </summary>
    Send = 0,

    /// <summary>
    /// Not sent: the user or the alert is paused, or the user left.
    /// </summary>
    Skip = 1
}

/// <summary>
/// Everything the notifier needs to send a notification: a copy of the post, with the channel it came from.
/// </summary>
public class NotificationDispatchDto {
    public long NotificationId { get; set; }

    public DispatchDecision Decision { get; set; }

    public int UserId { get; set; }

    public long ChatId { get; set; }

    public int AlertId { get; set; }

    public string AlertText { get; set; } = string.Empty;

    public decimal? AlertMaxPrice { get; set; }

    /// <summary>
    /// Original text of the post.
    /// </summary>
    public string OfferText { get; set; } = string.Empty;

    /// <summary>
    /// The post with its formatting, as HTML. Null when the reader did not keep it.
    /// </summary>
    public string? OfferHtml { get; set; }

    /// <summary>
    /// Link of the offer as posted, with the affiliate code of the channel.
    /// </summary>
    public string? OfferLink { get; set; }

    /// <summary>
    /// Link to the post in the channel.
    /// </summary>
    public string? PostLink { get; set; }

    public string ChannelUsername { get; set; } = string.Empty;

    public string? ChannelTitle { get; set; }

    public decimal? Price { get; set; }

    /// <summary>
    /// The alert has a limit, and the price of the offer could not be read.
    /// </summary>
    public bool PriceUnknown { get; set; }
}