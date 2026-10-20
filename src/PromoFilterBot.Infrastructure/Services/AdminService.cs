/*
 * File: AdminService.cs
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

using PromoFilterBot.Domain.Models;
using PromoFilterBot.Infrastructure.Common;
using PromoFilterBot.Infrastructure.Dto;
using PromoFilterBot.Infrastructure.Interfaces;
using PromoFilterBot.Infrastructure.Options;

namespace PromoFilterBot.Infrastructure.Services;

public class AdminService : IAdminService {
    private readonly IUserRepository _userRepository;
    private readonly IAlertRepository _alertRepository;
    private readonly IChannelRepository _channelRepository;
    private readonly IChannelMessageRepository _channelMessageRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IReaderStatusService _readerStatusService;
    private readonly TimeProvider _timeProvider;
    private readonly GeneralOptions _generalOptions;
    private readonly ILogger<AdminService> _logger;

    public AdminService(
        IUserRepository userRepository,
        IAlertRepository alertRepository,
        IChannelRepository channelRepository,
        IChannelMessageRepository channelMessageRepository,
        INotificationRepository notificationRepository,
        IReaderStatusService readerStatusService,
        TimeProvider timeProvider,
        IOptions<GeneralOptions> generalOptions,
        ILogger<AdminService> logger) {
        _userRepository = userRepository;
        _alertRepository = alertRepository;
        _channelRepository = channelRepository;
        _channelMessageRepository = channelMessageRepository;
        _notificationRepository = notificationRepository;
        _readerStatusService = readerStatusService;
        _timeProvider = timeProvider;
        _generalOptions = generalOptions.Value;
        _logger = logger;
    }

    public async Task<ServiceResult<AdminStatsDto>> GetStatsAsync(long chatId) {
        if (!_generalOptions.IsAdmin(chatId)) {
            _logger.LogWarning(LogMessages.NOT_ADMIN, chatId);
            return new ServiceResult<AdminStatsDto>(ResultCode.NOT_ADMIN, null);
        }

        DateTime now = _timeProvider.GetUtcNow().UtcDateTime;
        DateTime lastDay = now.AddHours(-24);

        AdminStatsDto stats = new AdminStatsDto {
            TotalUsers = await _userRepository.CountUsersAsync(),
            ActiveUsers = await _userRepository.CountActiveUsersAsync(),
            PausedUsers = await _userRepository.CountPausedUsersAsync(now),
            ActiveAlerts = await _alertRepository.CountAllActiveAlertsAsync(),
            ChannelsByStatus = await _channelRepository.CountChannelsByStatusAsync(),
            MessagesLast24Hours = await _channelMessageRepository.CountChannelMessagesSinceAsync(lastDay),
            NotificationsSentLast24Hours = await _notificationRepository.CountNotificationsAsync(NotificationStatus.Sent, lastDay),
            PendingNotifications = await _notificationRepository.CountNotificationsAsync(NotificationStatus.Pending),
            NotRelevantFeedbackLast7Days = await _notificationRepository.CountFeedbackSinceAsync(NotificationFeedback.NotRelevant, now.AddDays(-7)),
            LastMessageAt = await _channelMessageRepository.GetLastReceivedAtAsync(),
            Reader = _readerStatusService.GetStatus()
        };

        return new ServiceResult<AdminStatsDto>(ResultCode.NO_ERROR, stats);
    }
}