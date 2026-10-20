/*
 * File: UserRepository.cs
 * Project: Repositories
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

using System.Diagnostics.CodeAnalysis;

using Microsoft.EntityFrameworkCore;

using PromoFilterBot.Domain.Models;
using PromoFilterBot.Infrastructure.Interfaces;
using PromoFilterBot.Infrastructure.Models;

namespace PromoFilterBot.Infrastructure.Repositories;

[ExcludeFromCodeCoverage]
public class UserRepository : BaseRepository, IUserRepository {
    private new readonly PromoFilterBotDbContext _dbContext;

    public UserRepository(PromoFilterBotDbContext dbContext) : base(dbContext) {
        _dbContext = dbContext;
    }

    public async Task<User?> GetUserByChatIdAsync(long chatId) {
        return await _dbContext.Users.FirstOrDefaultAsync(u => u.UserTelegramChatId == chatId && !u.IsDeleted);
    }

    public async Task<List<User>> GetUsersByIdsAsync(IReadOnlyCollection<int> userIds) {
        return await _dbContext.Users.Where(u => userIds.Contains(u.UserId) && !u.IsDeleted).ToListAsync();
    }

    public async Task<User> CreateUserAsync(long chatId, string name, string creationUser) {
        User user = new User {
            CreationUser = creationUser,
            UpdateUser = creationUser,
            UserTelegramChatId = chatId,
            UserName = name,
            UserIsActive = true
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        return user;
    }

    public async Task<User> EditUserAsync(User user, string name, bool isActive, string currentUser) {
        user.UserName = name;
        user.UserIsActive = isActive;
        user.UpdateUser = currentUser;

        await _dbContext.SaveChangesAsync();

        return user;
    }

    public async Task<User> SetUserPausedUntilAsync(User user, DateTime? pausedUntil, string currentUser) {
        user.UserPausedUntil = pausedUntil;
        user.UpdateUser = currentUser;

        await _dbContext.SaveChangesAsync();

        return user;
    }

    public async Task DeactivateUserAsync(int userId, string currentUser) {
        DateTime now = DateTime.UtcNow;

        await _dbContext.Users
            .Where(u => u.UserId == userId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.UserIsActive, false)
                .SetProperty(u => u.UpdateUser, currentUser)
                .SetProperty(u => u.UpdateTime, now));
    }

    public async Task DeleteUserAsync(int userId) {
        await _dbContext.Users.Where(u => u.UserId == userId).ExecuteDeleteAsync();
    }

    public async Task<int> CountUsersAsync() {
        return await _dbContext.Users.CountAsync(u => !u.IsDeleted);
    }

    public async Task<int> CountActiveUsersAsync() {
        return await _dbContext.Users.CountAsync(u => u.UserIsActive && !u.IsDeleted);
    }

    public async Task<int> CountPausedUsersAsync(DateTime nowUtc) {
        return await _dbContext.Users.CountAsync(u => u.UserPausedUntil > nowUtc && !u.IsDeleted);
    }
}