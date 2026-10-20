/*
 * File: NotificationRepository.cs
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
public class NotificationRepository : BaseRepository, INotificationRepository {
    private new readonly PromoFilterBotDbContext _dbContext;

    public NotificationRepository(PromoFilterBotDbContext dbContext) : base(dbContext) {
        _dbContext = dbContext;
    }

    public async Task CreateNotificationsAsync(IReadOnlyCollection<Notification> notifications) {
        await _dbContext.Notifications.AddRangeAsync(notifications);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<Notification?> GetNotificationForDispatchAsync(long notificationId) {
        return await _dbContext.Notifications
            .Include(n => n.User)
            .Include(n => n.Alert)
            .Include(n => n.ChannelMessage)
            .ThenInclude(m => m!.Channel)
            .FirstOrDefaultAsync(n => n.NotificationId == notificationId);
    }

    public async Task<Notification?> GetNotificationByIdAsync(long notificationId) {
        return await _dbContext.Notifications
            .Include(n => n.Alert)
            .FirstOrDefaultAsync(n => n.NotificationId == notificationId);
    }

    // The same columns of the unique index, so the matcher never tries to save an offer twice
    public async Task<List<int>> GetUserIdsWithOfferAsync(
        IReadOnlyCollection<int> userIds, string productKey, string store, decimal? price, string coupon, DateOnly offerDate) {
        return await _dbContext.Notifications
            .Where(n => userIds.Contains(n.UserId)
                && n.NotificationProductKey == productKey
                && n.NotificationStore == store
                && n.NotificationPrice == price
                && n.NotificationCoupon == coupon
                && n.NotificationOfferDate == offerDate)
            .Select(n => n.UserId)
            .Distinct()
            .ToListAsync();
    }

    public async Task SetNotificationStatusAsync(Notification notification, NotificationStatus status, DateTime? sentAt, string currentUser) {
        notification.NotificationStatus = status;
        notification.NotificationSentAt = sentAt ?? notification.NotificationSentAt;
        notification.UpdateUser = currentUser;

        await _dbContext.SaveChangesAsync();
    }

    public async Task SetNotificationsStatusAsync(IReadOnlyCollection<long> notificationIds, NotificationStatus status, DateTime? sentAt, string currentUser) {
        DateTime now = DateTime.UtcNow;

        await _dbContext.Notifications
            .Where(n => notificationIds.Contains(n.NotificationId))
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.NotificationStatus, status)
                .SetProperty(n => n.NotificationSentAt, sentAt)
                .SetProperty(n => n.UpdateUser, currentUser)
                .SetProperty(n => n.UpdateTime, now));
    }

    public async Task<List<long>> GetPendingNotificationIdsAsync() {
        return await _dbContext.Notifications
            .Where(n => n.NotificationStatus == NotificationStatus.Pending)
            .OrderBy(n => n.NotificationId)
            .Select(n => n.NotificationId)
            .ToListAsync();
    }

    public async Task SetNotificationFeedbackAsync(Notification notification, NotificationFeedback feedback, string currentUser) {
        notification.NotificationFeedback = feedback;
        notification.UpdateUser = currentUser;

        await _dbContext.SaveChangesAsync();
    }

    public async Task<int> CountNotificationsAsync(NotificationStatus status, DateTime? since = null) {
        IQueryable<Notification> query = _dbContext.Notifications.Where(n => n.NotificationStatus == status);

        if (since.HasValue) query = query.Where(n => n.UpdateTime >= since.Value);

        return await query.CountAsync();
    }

    public async Task<int> CountFeedbackSinceAsync(NotificationFeedback feedback, DateTime since) {
        return await _dbContext.Notifications.CountAsync(n => n.NotificationFeedback == feedback && n.UpdateTime >= since);
    }
}