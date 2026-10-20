/*
 * File: ConversationStateRepository.cs
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
public class ConversationStateRepository : BaseRepository, IConversationStateRepository {
    private new readonly PromoFilterBotDbContext _dbContext;

    public ConversationStateRepository(PromoFilterBotDbContext dbContext) : base(dbContext) {
        _dbContext = dbContext;
    }

    public async Task<ConversationState?> GetConversationStateAsync(int userId) {
        return await _dbContext.ConversationStates.FirstOrDefaultAsync(s => s.UserId == userId);
    }

    public async Task<ConversationState> SaveConversationStateAsync(int userId, ConversationStep step, string? dataJson, string currentUser) {
        ConversationState? state = await GetConversationStateAsync(userId);

        if (state == null) {
            state = new ConversationState {
                UserId = userId,
                CreationUser = currentUser,
                UpdateUser = currentUser
            };

            await _dbContext.ConversationStates.AddAsync(state);
        }

        state.ConversationStateStep = step;
        state.ConversationStateDataJson = dataJson;
        state.UpdateUser = currentUser;

        await _dbContext.SaveChangesAsync();

        return state;
    }

    public async Task DeleteConversationStateAsync(int userId) {
        await _dbContext.ConversationStates.Where(s => s.UserId == userId).ExecuteDeleteAsync();
    }

    public async Task<int> DeleteConversationStatesOlderThanAsync(DateTime limit) {
        return await _dbContext.ConversationStates.Where(s => s.UpdateTime < limit).ExecuteDeleteAsync();
    }
}