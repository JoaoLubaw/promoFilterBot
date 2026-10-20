/*
 * File: Channel.cs
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
/// Public promotion channel read by the reader account. Only the admin registers channels.
/// </summary>
[Table("channel"), DataContract]
public class Channel : BaseEntity {
    /// <summary>
    /// Table primary key.
    /// </summary>
    [Column("channel_id"), DataMember]
    public int ChannelId { get; set; }

    /// <summary>
    /// MTProto id of the channel, known once the reader resolves it.
    /// </summary>
    [Column("channel_telegram_id"), DataMember]
    public long? ChannelTelegramId { get; set; }

    /// <summary>
    /// MTProto access hash, needed to reach the channel without resolving its username again.
    /// </summary>
    [Column("channel_access_hash"), DataMember]
    public long? ChannelAccessHash { get; set; }

    /// <summary>
    /// Public username, in lower case and without the '@'.
    /// </summary>
    [Column("channel_username"), MaxLength(32), DataMember]
    public required string ChannelUsername { get; set; }

    /// <summary>
    /// Channel title, as shown by Telegram.
    /// </summary>
    [Column("channel_title"), DataMember]
    public string? ChannelTitle { get; set; }

    [Column("channel_status"), DataMember]
    public ChannelStatus ChannelStatus { get; set; } = ChannelStatus.Approved;

    /// <summary>
    /// Why the channel was blocked (not a public channel, username not found, removed by the admin...).
    /// </summary>
    [Column("channel_status_reason"), DataMember]
    public string? ChannelStatusReason { get; set; }

    /// <summary>
    /// When the reader account joined the channel.
    /// </summary>
    [Column("channel_joined_at"), DataMember]
    public DateTime? ChannelJoinedAt { get; set; }
}