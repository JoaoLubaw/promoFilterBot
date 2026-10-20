/*
 * File: Notification.cs
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
/// An offer matched by an alert, sent (or to be sent) to its owner.
/// </summary>
[Table("notification"), DataContract]
public class Notification : BaseEntity {
    /// <summary>
    /// Table primary key.
    /// </summary>
    [Column("notification_id"), DataMember]
    public long NotificationId { get; set; }

    [Column("user_id"), DataMember]
    public int UserId { get; set; }

    [ForeignKey("UserId"), IgnoreDataMember]
    public virtual User? User { get; set; }

    [Column("alert_id"), DataMember]
    public int AlertId { get; set; }

    [ForeignKey("AlertId"), IgnoreDataMember]
    public virtual Alert? Alert { get; set; }

    [Column("channel_message_id"), DataMember]
    public long ChannelMessageId { get; set; }

    [ForeignKey("ChannelMessageId"), IgnoreDataMember]
    public virtual ChannelMessage? ChannelMessage { get; set; }

    /// <summary>
    /// The offer is product x store x price x coupon: the user gets each one once a day, and gets it again when any of
    /// them changes (another price, another coupon, another store).
    /// </summary>
    [Column("notification_product_key"), MaxLength(128), DataMember]
    public required string NotificationProductKey { get; set; }

    [Column("notification_store"), MaxLength(32), DataMember]
    public string NotificationStore { get; set; } = string.Empty;

    [Column("notification_price", TypeName = "numeric(12,2)"), DataMember]
    public decimal? NotificationPrice { get; set; }

    [Column("notification_coupon"), MaxLength(128), DataMember]
    public string NotificationCoupon { get; set; } = string.Empty;

    /// <summary>
    /// Day of the offer in the time zone of the bot (General:TimeZone).
    /// </summary>
    [Column("notification_offer_date"), DataMember]
    public DateOnly NotificationOfferDate { get; set; }

    [Column("notification_status"), DataMember]
    public NotificationStatus NotificationStatus { get; set; } = NotificationStatus.Pending;

    [Column("notification_sent_at"), DataMember]
    public DateTime? NotificationSentAt { get; set; }

    [Column("notification_feedback"), DataMember]
    public NotificationFeedback? NotificationFeedback { get; set; }
}