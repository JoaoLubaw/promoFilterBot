/*
 * File: INotificationRepository.cs
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

public interface INotificationRepository {
    Task CreateNotificationsAsync(IReadOnlyCollection<Notification> notifications);

    // With the user, the alert, and the message with its channel.
    Task<Notification?> GetNotificationForDispatchAsync(long notificationId);

    Task<Notification?> GetNotificationByIdAsync(long notificationId);

    // Users that already have the offer (product x store x price x coupon) on the day, whatever happened to it.
    Task<List<int>> GetUserIdsWithOfferAsync(
        IReadOnlyCollection<int> userIds, string productKey, string store, decimal? price, string coupon, DateOnly offerDate);

    Task SetNotificationStatusAsync(Notification notification, NotificationStatus status, DateTime? sentAt, string currentUser);

    Task SetNotificationsStatusAsync(IReadOnlyCollection<long> notificationIds, NotificationStatus status, DateTime? sentAt, string currentUser);

    Task<List<long>> GetPendingNotificationIdsAsync();

    Task SetNotificationFeedbackAsync(Notification notification, NotificationFeedback feedback, string currentUser);

    Task<int> CountNotificationsAsync(NotificationStatus status, DateTime? since = null);

    Task<int> CountFeedbackSinceAsync(NotificationFeedback feedback, DateTime since);
}