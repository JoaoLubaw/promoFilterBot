/*
 * File: ILocalClock.cs
 * Project: Interfaces
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

namespace PromoFilterBot.Bot.Interfaces;

/// <summary>
/// The current time, and the dates shown in the time zone of the users.
/// </summary>
public interface ILocalClock {
    DateTime UtcNow { get; }

    // "20/10 às 18:30"
    string Format(DateTime utc);
}