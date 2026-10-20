/*
 * File: ChannelStatus.cs
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

namespace PromoFilterBot.Domain.Models;

/// <summary>
/// Life cycle of a channel: registered by the admin, joined by the reader account, or blocked.
/// </summary>
public enum ChannelStatus {
    /// <summary>
    /// Registered, waiting for the reader account to join it.
    /// </summary>
    Approved = 1,

    Active = 2,

    /// <summary>
    /// Removed by the admin, or impossible to read (see the reason).
    /// </summary>
    Blocked = 3
}