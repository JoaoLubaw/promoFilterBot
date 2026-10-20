/*
 * File: UserDto.cs
 * Project: Dto
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

namespace PromoFilterBot.Infrastructure.Dto;

public class UserDto {
    public int UserId { get; set; }

    public long UserChatId { get; set; }

    public required string UserName { get; set; }

    public DateTime? UserPausedUntil { get; set; }

    public bool UserIsActive { get; set; }

    /// <summary>
    /// Registered by this call, so it is the first contact with the bot.
    /// </summary>
    public bool UserIsNew { get; set; }

    public bool IsPaused(DateTime nowUtc) => UserPausedUntil.HasValue && UserPausedUntil.Value > nowUtc;
}