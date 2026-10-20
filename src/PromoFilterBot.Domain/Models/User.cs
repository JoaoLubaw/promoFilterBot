/*
 * File: User.cs
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

using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace PromoFilterBot.Domain.Models;

/// <summary>
/// Someone who talks to the bot. Only what the bot needs is kept: the chat, a display name and the alerts.
/// </summary>
[Table("user"), DataContract]
public class User : BaseEntity {
    /// <summary>
    /// Table primary key.
    /// </summary>
    [Column("user_id"), DataMember]
    public int UserId { get; set; }

    /// <summary>
    /// Private chat with the bot, where the notifications are sent.
    /// </summary>
    [Column("user_telegram_chat_id"), DataMember]
    public long UserTelegramChatId { get; set; }

    /// <summary>
    /// Display name, as shown by Telegram.
    /// </summary>
    [Column("user_name"), DataMember]
    public required string UserName { get; set; }

    /// <summary>
    /// While in the future, no notification is sent to the user (/pausar).
    /// </summary>
    [Column("user_paused_until"), DataMember]
    public DateTime? UserPausedUntil { get; set; }

    /// <summary>
    /// False when the user blocked the bot, so nothing is sent until they talk to it again.
    /// </summary>
    [Column("user_is_active"), DataMember]
    public bool UserIsActive { get; set; } = true;

    [IgnoreDataMember]
    public virtual ICollection<Alert> Alerts { get; set; } = [];
}