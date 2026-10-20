/*
 * File: IUserRepository.cs
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

using PromoFilterBot.Domain.Models;

namespace PromoFilterBot.Infrastructure.Interfaces;

public interface IUserRepository {
    Task<User?> GetUserByChatIdAsync(long chatId);

    Task<List<User>> GetUsersByIdsAsync(IReadOnlyCollection<int> userIds);

    Task<User> CreateUserAsync(long chatId, string name, string creationUser);

    Task<User> EditUserAsync(User user, string name, bool isActive, string currentUser);

    Task<User> SetUserPausedUntilAsync(User user, DateTime? pausedUntil, string currentUser);

    // Done when the user blocks the bot; they are active again on their next message.
    Task DeactivateUserAsync(int userId, string currentUser);

    // Deletes the user and, through the cascades of the database, everything that is theirs.
    Task DeleteUserAsync(int userId);

    Task<int> CountUsersAsync();

    Task<int> CountActiveUsersAsync();

    Task<int> CountPausedUsersAsync(DateTime nowUtc);
}