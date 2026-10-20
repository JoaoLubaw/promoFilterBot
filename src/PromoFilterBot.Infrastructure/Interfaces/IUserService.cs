/*
 * File: IUserService.cs
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

using PromoFilterBot.Infrastructure.Common;
using PromoFilterBot.Infrastructure.Dto;

namespace PromoFilterBot.Infrastructure.Interfaces;

public interface IUserService {
    // Registers the chat on its first message, and keeps its name up to date. A user who had blocked the bot is active
    // again.
    Task<ServiceResult<UserDto>> RegisterUserAsync(long chatId, string? name);

    Task<ServiceResult<UserDto>> GetUserAsync(long chatId);

    // Pauses the notifications for a duration (30m, 8h, 2 dias), until tomorrow morning, or resumes them (retomar).
    Task<ServiceResult<UserDto>> PauseUserAsync(long chatId, string? duration);

    // Deletes everything about the user (/apagarconta).
    Task<ServiceResult<EmptyDto>> DeleteAccountAsync(long chatId);

    // The user blocked the bot, so nothing is sent to them until they write again.
    Task DeactivateUserAsync(int userId);

    bool IsAdmin(long chatId);
}