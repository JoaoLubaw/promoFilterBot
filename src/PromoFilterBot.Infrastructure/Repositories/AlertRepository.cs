/*
 * File: AlertRepository.cs
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

using PromoFilterBot.Domain.Dto;
using PromoFilterBot.Domain.Models;
using PromoFilterBot.Infrastructure.Interfaces;
using PromoFilterBot.Infrastructure.Models;

namespace PromoFilterBot.Infrastructure.Repositories;

[ExcludeFromCodeCoverage]
public class AlertRepository : BaseRepository, IAlertRepository {
    private new readonly PromoFilterBotDbContext _dbContext;

    public AlertRepository(PromoFilterBotDbContext dbContext) : base(dbContext) {
        _dbContext = dbContext;
    }

    public async Task<List<Alert>> GetUserAlertsAsync(int userId) {
        return await _dbContext.Alerts
            .Where(a => a.UserId == userId && !a.IsDeleted)
            .OrderBy(a => a.AlertId)
            .ToListAsync();
    }

    public async Task<Alert?> GetAlertByIdAsync(int alertId) {
        return await _dbContext.Alerts.FirstOrDefaultAsync(a => a.AlertId == alertId && !a.IsDeleted);
    }

    public async Task<int> CountActiveAlertsAsync(int userId) {
        return await _dbContext.Alerts.CountAsync(a => a.UserId == userId && a.AlertIsActive && !a.IsDeleted);
    }

    public async Task<int> CountAlertsAsync(int userId) {
        return await _dbContext.Alerts.CountAsync(a => a.UserId == userId && !a.IsDeleted);
    }

    public async Task<Alert> CreateAlertAsync(int userId, string originalText, AlertQuery query, decimal? maxPrice, string creationUser) {
        Alert alert = new Alert {
            CreationUser = creationUser,
            UpdateUser = creationUser,
            UserId = userId,
            AlertOriginalText = originalText,
            AlertNormalizedTerms = query.StoredRequiredTerms,
            AlertExcludedTerms = query.StoredExcludedTerms,
            AlertMaxPrice = maxPrice,
            AlertIsActive = true
        };

        await _dbContext.Alerts.AddAsync(alert);
        await _dbContext.SaveChangesAsync();

        return alert;
    }

    public async Task<Alert> EditAlertAsync(Alert alert, string originalText, AlertQuery query, decimal? maxPrice, string currentUser) {
        alert.AlertOriginalText = originalText;
        alert.AlertNormalizedTerms = query.StoredRequiredTerms;
        alert.AlertExcludedTerms = query.StoredExcludedTerms;
        alert.AlertMaxPrice = maxPrice;
        alert.UpdateUser = currentUser;

        await _dbContext.SaveChangesAsync();

        return alert;
    }

    public async Task<Alert> SetAlertActiveAsync(Alert alert, bool isActive, string currentUser) {
        alert.AlertIsActive = isActive;
        alert.UpdateUser = currentUser;

        await _dbContext.SaveChangesAsync();

        return alert;
    }

    public async Task DeleteAlertAsync(Alert alert, string currentUser) {
        alert.IsDeleted = true;
        alert.AlertIsActive = false;
        alert.UpdateUser = currentUser;

        await _dbContext.SaveChangesAsync();
    }

    public async Task<List<Alert>> GetIndexableAlertsAsync() {
        return await _dbContext.Alerts
            .AsNoTracking()
            .Where(a => a.AlertIsActive && !a.IsDeleted && a.User!.UserIsActive && !a.User.IsDeleted)
            .ToListAsync();
    }

    public async Task<int> CountAllActiveAlertsAsync() {
        return await _dbContext.Alerts.CountAsync(a => a.AlertIsActive && !a.IsDeleted);
    }
}