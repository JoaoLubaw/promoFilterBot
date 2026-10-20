/*
 * File: MaintenanceService.cs
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

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using PromoFilterBot.Infrastructure.Dto;
using PromoFilterBot.Infrastructure.Interfaces;
using PromoFilterBot.Infrastructure.Options;

namespace PromoFilterBot.Infrastructure.Services;

public class MaintenanceService : IMaintenanceService {
    private readonly IChannelMessageRepository _channelMessageRepository;
    private readonly IConversationStateRepository _conversationStateRepository;
    private readonly TimeProvider _timeProvider;
    private readonly LimitsOptions _limits;
    private readonly ILogger<MaintenanceService> _logger;

    public MaintenanceService(
        IChannelMessageRepository channelMessageRepository,
        IConversationStateRepository conversationStateRepository,
        TimeProvider timeProvider,
        IOptions<LimitsOptions> limits,
        ILogger<MaintenanceService> logger) {
        _channelMessageRepository = channelMessageRepository;
        _conversationStateRepository = conversationStateRepository;
        _timeProvider = timeProvider;
        _limits = limits.Value;
        _logger = logger;
    }

    public async Task<MaintenanceResultDto> CleanupAsync() {
        DateTime now = _timeProvider.GetUtcNow().UtcDateTime;

        MaintenanceResultDto result = new MaintenanceResultDto {
            DeletedMessages = await _channelMessageRepository.DeleteChannelMessagesOlderThanAsync(now.AddDays(-_limits.MessageRetentionDays)),
            DeletedConversationStates = await _conversationStateRepository.DeleteConversationStatesOlderThanAsync(now.AddHours(-_limits.ConversationTimeoutHours))
        };

        _logger.LogInformation(
            "Cleanup deleted {Messages} messages older than {Days} days and {States} abandoned guided flows",
            result.DeletedMessages, _limits.MessageRetentionDays, result.DeletedConversationStates);

        return result;
    }
}