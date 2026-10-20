/*
 * File: IConversationService.cs
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
using PromoFilterBot.Infrastructure.Dto;

namespace PromoFilterBot.Infrastructure.Interfaces;

public interface IConversationService {
    // Step of the guided flow the user is in, or null when there is none (or it expired).
    Task<ConversationStateDto?> GetStateAsync(long chatId);

    Task SaveStateAsync(long chatId, ConversationStep step, AlertDraftDto? draft = null);

    Task ClearStateAsync(long chatId);
}