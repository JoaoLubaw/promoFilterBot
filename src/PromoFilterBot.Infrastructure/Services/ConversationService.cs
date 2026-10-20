/*
 * File: ConversationService.cs
 * Project: Services
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

using System.Text.Json;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using PromoFilterBot.Domain.Models;
using PromoFilterBot.Infrastructure.Common;
using PromoFilterBot.Infrastructure.Dto;
using PromoFilterBot.Infrastructure.Interfaces;
using PromoFilterBot.Infrastructure.Options;

namespace PromoFilterBot.Infrastructure.Services;

public class ConversationService : IConversationService {
    private readonly IConversationStateRepository _conversationStateRepository;
    private readonly IUserRepository _userRepository;
    private readonly TimeProvider _timeProvider;
    private readonly LimitsOptions _limits;
    private readonly ILogger<ConversationService> _logger;

    public ConversationService(
        IConversationStateRepository conversationStateRepository,
        IUserRepository userRepository,
        TimeProvider timeProvider,
        IOptions<LimitsOptions> limits,
        ILogger<ConversationService> logger) {
        _conversationStateRepository = conversationStateRepository;
        _userRepository = userRepository;
        _timeProvider = timeProvider;
        _limits = limits.Value;
        _logger = logger;
    }

    public async Task<ConversationStateDto?> GetStateAsync(long chatId) {
        User? user = await _userRepository.GetUserByChatIdAsync(chatId);

        if (user == null) return null;

        ConversationState? state = await _conversationStateRepository.GetConversationStateAsync(user.UserId);

        if (state == null || state.ConversationStateStep == ConversationStep.None) return null;

        DateTime limit = _timeProvider.GetUtcNow().UtcDateTime.AddHours(-_limits.ConversationTimeoutHours);

        if (state.UpdateTime < limit) {
            await _conversationStateRepository.DeleteConversationStateAsync(user.UserId);
            return null;
        }

        return new ConversationStateDto {
            Step = state.ConversationStateStep,
            Draft = ReadDraft(state.ConversationStateDataJson, user.UserId)
        };
    }

    public async Task SaveStateAsync(long chatId, ConversationStep step, AlertDraftDto? draft = null) {
        User? user = await _userRepository.GetUserByChatIdAsync(chatId);

        if (user == null) {
            _logger.LogWarning(LogMessages.UNREGISTERED_USER, chatId);
            return;
        }

        string dataJson = JsonSerializer.Serialize(draft ?? new AlertDraftDto());

        await _conversationStateRepository.SaveConversationStateAsync(user.UserId, step, dataJson, SystemUsers.FromChat(chatId));
    }

    public async Task ClearStateAsync(long chatId) {
        User? user = await _userRepository.GetUserByChatIdAsync(chatId);

        if (user == null) return;

        await _conversationStateRepository.DeleteConversationStateAsync(user.UserId);
    }

    private AlertDraftDto ReadDraft(string? dataJson, int userId) {
        if (string.IsNullOrWhiteSpace(dataJson)) return new AlertDraftDto();

        try {
            return JsonSerializer.Deserialize<AlertDraftDto>(dataJson) ?? new AlertDraftDto();
        }
        catch (JsonException ex) {
            _logger.LogWarning(ex, "Guided flow data of user {UserId} could not be read, starting over", userId);
            return new AlertDraftDto();
        }
    }
}