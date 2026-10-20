/*
 * File: ConversationState.cs
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
/// Step of the guided flow the user is in, with what was answered so far. The update time of the row is when the user
/// last answered.
/// </summary>
[Table("conversation_state"), DataContract]
public class ConversationState : BaseEntity {
    /// <summary>
    /// Table primary key: one guided flow per user.
    /// </summary>
    [Column("user_id"), DataMember]
    public int UserId { get; set; }

    [ForeignKey("UserId"), IgnoreDataMember]
    public virtual User? User { get; set; }

    [Column("conversation_state_step"), DataMember]
    public ConversationStep ConversationStateStep { get; set; }

    /// <summary>
    /// Answers given so far, as JSON.
    /// </summary>
    [Column("conversation_state_data_json", TypeName = "jsonb"), DataMember]
    public string? ConversationStateDataJson { get; set; }
}