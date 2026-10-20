/*
 * File: IConversationStateRepository.cs
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

public interface IConversationStateRepository {
    Task<ConversationState?> GetConversationStateAsync(int userId);

    // Creates the state of the user, or replaces it.
    Task<ConversationState> SaveConversationStateAsync(int userId, ConversationStep step, string? dataJson, string currentUser);

    Task DeleteConversationStateAsync(int userId);

    Task<int> DeleteConversationStatesOlderThanAsync(DateTime limit);
}