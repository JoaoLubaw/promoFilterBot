/*
 * File: Alert.cs
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
/// What a user wants to buy, and up to which price.
/// </summary>
[Table("alert"), DataContract]
public class Alert : BaseEntity {
    /// <summary>
    /// Table primary key.
    /// </summary>
    [Column("alert_id"), DataMember]
    public int AlertId { get; set; }

    /// <summary>
    /// Owner of the alert, the only one notified by it.
    /// </summary>
    [Column("user_id"), DataMember]
    public int UserId { get; set; }

    [ForeignKey("UserId"), IgnoreDataMember]
    public virtual User? User { get; set; }

    /// <summary>
    /// Search as the user typed it.
    /// </summary>
    [Column("alert_original_text"), DataMember]
    public required string AlertOriginalText { get; set; }

    /// <summary>
    /// Normalized terms the offer must contain, separated by '|' (a term with spaces is a phrase).
    /// </summary>
    [Column("alert_normalized_terms"), DataMember]
    public required string AlertNormalizedTerms { get; set; }

    /// <summary>
    /// Normalized terms the offer must not contain, separated by '|'.
    /// </summary>
    [Column("alert_excluded_terms"), DataMember]
    public string AlertExcludedTerms { get; set; } = string.Empty;

    /// <summary>
    /// Highest price accepted. Null means no limit.
    /// </summary>
    [Column("alert_max_price", TypeName = "numeric(12,2)"), DataMember]
    public decimal? AlertMaxPrice { get; set; }

    /// <summary>
    /// False while the alert is paused.
    /// </summary>
    [Column("alert_is_active"), DataMember]
    public bool AlertIsActive { get; set; } = true;
}