/*
 * File: ChannelMessage.cs
 * Project: Models
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

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace PromoFilterBot.Domain.Models;

/// <summary>
/// Offer read from a channel. Kept for the retention period, to deduplicate and to debug the matching.
/// </summary>
[Table("channel_message"), DataContract]
public class ChannelMessage : BaseEntity {
    /// <summary>
    /// Table primary key.
    /// </summary>
    [Column("channel_message_id"), DataMember]
    public long ChannelMessageId { get; set; }

    [Column("channel_id"), DataMember]
    public int ChannelId { get; set; }

    [ForeignKey("ChannelId"), IgnoreDataMember]
    public virtual Channel? Channel { get; set; }

    /// <summary>
    /// Message id inside the channel.
    /// </summary>
    [Column("channel_message_telegram_id"), DataMember]
    public int ChannelMessageTelegramId { get; set; }

    /// <summary>
    /// Original text of the post.
    /// </summary>
    [Column("channel_message_text"), DataMember]
    public required string ChannelMessageText { get; set; }

    /// <summary>
    /// The post with its formatting (bold, hidden links, coupons in monospace), as the HTML the bot sends.
    /// </summary>
    [Column("channel_message_html"), DataMember]
    public string? ChannelMessageHtml { get; set; }

    /// <summary>
    /// Link of the offer as posted, with the affiliate code of the channel.
    /// </summary>
    [Column("channel_message_link"), DataMember]
    public string? ChannelMessageLink { get; set; }

    /// <summary>
    /// Price found in the text, if any.
    /// </summary>
    [Column("channel_message_detected_price", TypeName = "numeric(12,2)"), DataMember]
    public decimal? ChannelMessageDetectedPrice { get; set; }

    /// <summary>
    /// Identifies the product across channels (store id from the link, or a hash of the text).
    /// </summary>
    [Column("channel_message_product_key"), MaxLength(128), DataMember]
    public required string ChannelMessageProductKey { get; set; }

    /// <summary>
    /// Store of the offer (amazon, mercadolivre...), empty when unknown.
    /// </summary>
    [Column("channel_message_store"), MaxLength(32), DataMember]
    public string ChannelMessageStore { get; set; } = string.Empty;

    /// <summary>
    /// Coupon codes of the offer, in alphabetical order and separated by ',', empty without coupon.
    /// </summary>
    [Column("channel_message_coupon"), MaxLength(128), DataMember]
    public string ChannelMessageCoupon { get; set; } = string.Empty;

    /// <summary>
    /// When the reader received the post.
    /// </summary>
    [Column("channel_message_received_at"), DataMember]
    public DateTime ChannelMessageReceivedAt { get; set; }
}