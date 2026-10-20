/*
 * File: IAlertRepository.cs
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

using PromoFilterBot.Domain.Dto;
using PromoFilterBot.Domain.Models;

namespace PromoFilterBot.Infrastructure.Interfaces;

public interface IAlertRepository {
    // Alerts of the user that were not removed, in the order of /alertas.
    Task<List<Alert>> GetUserAlertsAsync(int userId);

    Task<Alert?> GetAlertByIdAsync(int alertId);

    Task<int> CountActiveAlertsAsync(int userId);

    Task<int> CountAlertsAsync(int userId);

    Task<Alert> CreateAlertAsync(int userId, string originalText, AlertQuery query, decimal? maxPrice, string creationUser);

    Task<Alert> EditAlertAsync(Alert alert, string originalText, AlertQuery query, decimal? maxPrice, string currentUser);

    Task<Alert> SetAlertActiveAsync(Alert alert, bool isActive, string currentUser);

    // Soft delete: the alert stays for the history of its notifications.
    Task DeleteAlertAsync(Alert alert, string currentUser);

    // Active alerts of active users, for the alert index.
    Task<List<Alert>> GetIndexableAlertsAsync();

    Task<int> CountAllActiveAlertsAsync();
}