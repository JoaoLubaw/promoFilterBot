/*
 * File: BaseEntity.cs
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
/// Common data between all tables.
/// </summary>
public class BaseEntity {
    /// <summary>
    /// Row creation user.
    /// </summary>
    [Column("row_creation_user"), DataMember]
    public string CreationUser { get; set; } = "system";

    /// <summary>
    /// Row creation time.
    /// </summary>
    [Column("row_creation_time"), DataMember]
    public DateTime CreationTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Row update user.
    /// </summary>
    [Column("row_update_user"), DataMember]
    public string UpdateUser { get; set; } = "system";

    /// <summary>
    /// Row update time.
    /// </summary>
    [Column("row_update_time"), DataMember]
    public DateTime UpdateTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// If row is deleted.
    /// </summary>
    [Column("row_is_deleted"), DataMember]
    public bool IsDeleted { get; set; } = false;
}