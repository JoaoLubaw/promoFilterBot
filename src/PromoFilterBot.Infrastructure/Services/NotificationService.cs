/*
 * File: NotificationService.cs
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

using PromoFilterBot.Domain.Models;
using PromoFilterBot.Infrastructure.Common;
using PromoFilterBot.Infrastructure.Dto;
using PromoFilterBot.Infrastructure.Interfaces;

namespace PromoFilterBot.Infrastructure.Services;

public class NotificationService : INotificationService {
    private readonly INotificationRepository _notificationRepository;
    private readonly IUserRepository _userRepository;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository notificationRepository,
        IUserRepository userRepository,
        TimeProvider timeProvider,
        ILogger<NotificationService> logger) {
        _notificationRepository = notificationRepository;
        _userRepository = userRepository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<ServiceResult<NotificationDispatchDto>> PrepareDispatchAsync(long notificationId) {
        Notification? notification = await _notificationRepository.GetNotificationForDispatchAsync(notificationId);

        if (notification == null) {
            _logger.LogWarning("Notification {NotificationId} not found", notificationId);
            return new ServiceResult<NotificationDispatchDto>(ResultCode.NOTIFICATION_NOT_FOUND, null);
        }

        if (notification.NotificationStatus != NotificationStatus.Pending) {
            _logger.LogDebug("Notification {NotificationId} was already handled ({Status})", notificationId, notification.NotificationStatus);
            return new ServiceResult<NotificationDispatchDto>(ResultCode.NOTIFICATION_ALREADY_HANDLED, null);
        }

        User user = notification.User!;
        Alert alert = notification.Alert!;
        DateTime now = _timeProvider.GetUtcNow().UtcDateTime;
        NotificationDispatchDto dispatch = BuildDispatch(notification);

        // Paused, or left, after the offer was matched
        if (!user.UserIsActive || user.IsDeleted || user.UserPausedUntil > now || !alert.AlertIsActive || alert.IsDeleted) {
            await _notificationRepository.SetNotificationStatusAsync(notification, NotificationStatus.Skipped, null, SystemUsers.NOTIFIER);

            dispatch.Decision = DispatchDecision.Skip;
            return new ServiceResult<NotificationDispatchDto>(ResultCode.NO_ERROR, dispatch);
        }

        dispatch.Decision = DispatchDecision.Send;
        return new ServiceResult<NotificationDispatchDto>(ResultCode.NO_ERROR, dispatch);
    }

    public async Task MarkSentAsync(long notificationId) {
        DateTime now = _timeProvider.GetUtcNow().UtcDateTime;

        await _notificationRepository.SetNotificationsStatusAsync([notificationId], NotificationStatus.Sent, now, SystemUsers.NOTIFIER);
    }

    public async Task MarkFailedAsync(long notificationId) {
        await _notificationRepository.SetNotificationsStatusAsync([notificationId], NotificationStatus.Failed, null, SystemUsers.NOTIFIER);
    }

    public async Task<List<long>> GetPendingNotificationIdsAsync() {
        return await _notificationRepository.GetPendingNotificationIdsAsync();
    }

    public async Task<ServiceResult<NotificationFeedbackDto>> RegisterFeedbackAsync(long chatId, long notificationId) {
        User? user = await _userRepository.GetUserByChatIdAsync(chatId);

        if (user == null) {
            _logger.LogWarning(LogMessages.UNREGISTERED_USER, chatId);
            return new ServiceResult<NotificationFeedbackDto>(ResultCode.UNREGISTERED_USER, null);
        }

        Notification? notification = await _notificationRepository.GetNotificationByIdAsync(notificationId);

        if (notification == null || notification.UserId != user.UserId) {
            _logger.LogWarning("Notification {NotificationId} not found for chat {ChatId}", notificationId, chatId);
            return new ServiceResult<NotificationFeedbackDto>(ResultCode.NOTIFICATION_NOT_FOUND, null);
        }

        await _notificationRepository.SetNotificationFeedbackAsync(notification, NotificationFeedback.NotRelevant, SystemUsers.FromChat(chatId));

        _logger.LogInformation(
            "Notification {NotificationId} of alert {AlertId} marked as not relevant", notificationId, notification.AlertId);

        NotificationFeedbackDto feedback = new NotificationFeedbackDto {
            NotificationId = notification.NotificationId,
            AlertId = notification.AlertId,
            AlertText = notification.Alert?.AlertOriginalText ?? string.Empty
        };

        return new ServiceResult<NotificationFeedbackDto>(ResultCode.NO_ERROR, feedback);
    }

    private static NotificationDispatchDto BuildDispatch(Notification notification) {
        ChannelMessage message = notification.ChannelMessage!;
        Alert alert = notification.Alert!;

        return new NotificationDispatchDto {
            NotificationId = notification.NotificationId,
            UserId = notification.UserId,
            ChatId = notification.User!.UserTelegramChatId,
            AlertId = alert.AlertId,
            AlertText = alert.AlertOriginalText,
            AlertMaxPrice = alert.AlertMaxPrice,
            OfferText = message.ChannelMessageText,
            OfferHtml = message.ChannelMessageHtml,
            OfferLink = message.ChannelMessageLink,
            PostLink = message.ToPostLink(),
            ChannelUsername = message.Channel?.ChannelUsername ?? string.Empty,
            ChannelTitle = message.Channel?.ChannelTitle,
            Price = notification.NotificationPrice,
            PriceUnknown = !notification.NotificationPrice.HasValue && alert.AlertMaxPrice.HasValue
        };
    }
}