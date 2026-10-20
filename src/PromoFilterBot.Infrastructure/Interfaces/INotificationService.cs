/*
 * File: INotificationService.cs
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

using PromoFilterBot.Infrastructure.Common;
using PromoFilterBot.Infrastructure.Dto;

namespace PromoFilterBot.Infrastructure.Interfaces;

public interface INotificationService {
    // Decides whether a pending notification is sent now or skipped (user or alert paused, user gone), and returns what
    // is needed to send it.
    Task<ServiceResult<NotificationDispatchDto>> PrepareDispatchAsync(long notificationId);

    Task MarkSentAsync(long notificationId);

    Task MarkFailedAsync(long notificationId);

    // Notifications not sent yet, queued again when the app starts.
    Task<List<long>> GetPendingNotificationIdsAsync();

    // "Não era isso".
    Task<ServiceResult<NotificationFeedbackDto>> RegisterFeedbackAsync(long chatId, long notificationId);
}