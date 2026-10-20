/*
 * File: NotificationStatus.cs
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

public enum NotificationStatus {
    /// <summary>
    /// Waiting for the notifier.
    /// </summary>
    Pending = 0,

    Sent = 1,

    /// <summary>
    /// Not sent: the user or the alert was paused, or the user left, before its turn.
    /// </summary>
    Skipped = 2,

    Failed = 3
}