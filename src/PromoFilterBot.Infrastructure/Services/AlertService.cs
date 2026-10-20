/*
 * File: AlertService.cs
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

using PromoFilterBot.Domain.Common;
using PromoFilterBot.Domain.Dto;
using PromoFilterBot.Domain.Models;
using PromoFilterBot.Infrastructure.Common;
using PromoFilterBot.Infrastructure.Dto;
using PromoFilterBot.Infrastructure.Interfaces;
using PromoFilterBot.Infrastructure.Options;

namespace PromoFilterBot.Infrastructure.Services;

public class AlertService : IAlertService {
    private readonly IAlertRepository _alertRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAlertIndexProvider _alertIndexProvider;
    private readonly LimitsOptions _limits;
    private readonly ILogger<AlertService> _logger;

    public AlertService(
        IAlertRepository alertRepository,
        IUserRepository userRepository,
        IAlertIndexProvider alertIndexProvider,
        IOptions<LimitsOptions> limits,
        ILogger<AlertService> logger) {
        _alertRepository = alertRepository;
        _userRepository = userRepository;
        _alertIndexProvider = alertIndexProvider;
        _limits = limits.Value;
        _logger = logger;
    }

    public async Task<ServiceResult<AlertDto>> CreateAlertAsync(long chatId, CreateAlertRequestDto requestDto) {
        User? user = await _userRepository.GetUserByChatIdAsync(chatId);

        if (user == null) {
            _logger.LogWarning(LogMessages.UNREGISTERED_USER, chatId);
            return new ServiceResult<AlertDto>(ResultCode.UNREGISTERED_USER, null);
        }

        if (requestDto == null) {
            _logger.LogWarning(LogMessages.EMPTY_REQUEST, chatId);
            return new ServiceResult<AlertDto>(ResultCode.EMPTY_REQUEST, null);
        }

        if (!requestDto.IsValid()) {
            _logger.LogWarning(LogMessages.INVALID_ALERT_TEXT, chatId);
            return new ServiceResult<AlertDto>(ResultCode.INVALID_ALERT_TEXT, null);
        }

        ResultCode validation = ValidateAlert(chatId, requestDto.AlertText, requestDto.AlertMaxPrice, out AlertQuery query);
        if (validation != ResultCode.NO_ERROR) return new ServiceResult<AlertDto>(validation, null);

        if (await _alertRepository.CountAlertsAsync(user.UserId) >= _limits.MaxAlertsPerUser) {
            _logger.LogWarning("User {UserId} reached the limit of {Limit} alerts", user.UserId, _limits.MaxAlertsPerUser);
            return new ServiceResult<AlertDto>(ResultCode.ALERT_LIMIT_REACHED, null);
        }

        if (await _alertRepository.CountActiveAlertsAsync(user.UserId) >= _limits.MaxActiveAlertsPerUser) {
            _logger.LogWarning("User {UserId} reached the limit of {Limit} active alerts", user.UserId, _limits.MaxActiveAlertsPerUser);
            return new ServiceResult<AlertDto>(ResultCode.ACTIVE_ALERT_LIMIT_REACHED, null);
        }

        Alert alert = await _alertRepository.CreateAlertAsync(
            user.UserId, requestDto.AlertText.Trim(), query, requestDto.AlertMaxPrice, SystemUsers.FromChat(chatId));

        _alertIndexProvider.Invalidate();

        List<Alert> alerts = await _alertRepository.GetUserAlertsAsync(user.UserId);
        int position = alerts.FindIndex(a => a.AlertId == alert.AlertId) + 1;

        return new ServiceResult<AlertDto>(ResultCode.NO_ERROR, alert.ToDto(position));
    }

    public async Task<ServiceResult<List<AlertDto>>> GetAlertsAsync(long chatId) {
        User? user = await _userRepository.GetUserByChatIdAsync(chatId);

        if (user == null) {
            _logger.LogWarning(LogMessages.UNREGISTERED_USER, chatId);
            return new ServiceResult<List<AlertDto>>(ResultCode.UNREGISTERED_USER, null);
        }

        List<Alert> alerts = await _alertRepository.GetUserAlertsAsync(user.UserId);
        List<AlertDto> alertDtos = alerts.Select((alert, index) => alert.ToDto(index + 1)).ToList();

        return new ServiceResult<List<AlertDto>>(ResultCode.NO_ERROR, alertDtos);
    }

    public async Task<ServiceResult<AlertDto>> GetAlertAsync(long chatId, int alertId) {
        User? user = await _userRepository.GetUserByChatIdAsync(chatId);

        if (user == null) {
            _logger.LogWarning(LogMessages.UNREGISTERED_USER, chatId);
            return new ServiceResult<AlertDto>(ResultCode.UNREGISTERED_USER, null);
        }

        (Alert? alert, int position) = await FindUserAlertAsync(user, alertId);

        if (alert == null) {
            _logger.LogWarning(LogMessages.ALERT_NOT_FOUND, alertId, chatId);
            return new ServiceResult<AlertDto>(ResultCode.ALERT_NOT_FOUND, null);
        }

        return new ServiceResult<AlertDto>(ResultCode.NO_ERROR, alert.ToDto(position));
    }

    public async Task<ServiceResult<AlertDto>> EditAlertAsync(long chatId, int alertId, EditAlertRequestDto requestDto) {
        User? user = await _userRepository.GetUserByChatIdAsync(chatId);

        if (user == null) {
            _logger.LogWarning(LogMessages.UNREGISTERED_USER, chatId);
            return new ServiceResult<AlertDto>(ResultCode.UNREGISTERED_USER, null);
        }

        (Alert? alert, int position) = await FindUserAlertAsync(user, alertId);

        if (alert == null) {
            _logger.LogWarning(LogMessages.ALERT_NOT_FOUND, alertId, chatId);
            return new ServiceResult<AlertDto>(ResultCode.ALERT_NOT_FOUND, null);
        }

        if (requestDto == null) {
            _logger.LogWarning(LogMessages.EMPTY_REQUEST, chatId);
            return new ServiceResult<AlertDto>(ResultCode.EMPTY_REQUEST, null);
        }

        if (!requestDto.IsValid()) {
            _logger.LogWarning(LogMessages.INVALID_ALERT_TEXT, chatId);
            return new ServiceResult<AlertDto>(ResultCode.INVALID_ALERT_TEXT, null);
        }

        ResultCode validation = ValidateAlert(chatId, requestDto.AlertText, requestDto.AlertMaxPrice, out AlertQuery query);
        if (validation != ResultCode.NO_ERROR) return new ServiceResult<AlertDto>(validation, null);

        alert = await _alertRepository.EditAlertAsync(
            alert, requestDto.AlertText.Trim(), query, requestDto.AlertMaxPrice, SystemUsers.FromChat(chatId));

        _alertIndexProvider.Invalidate();

        return new ServiceResult<AlertDto>(ResultCode.NO_ERROR, alert.ToDto(position));
    }

    public async Task<ServiceResult<AlertDto>> ToggleAlertAsync(long chatId, int alertId) {
        User? user = await _userRepository.GetUserByChatIdAsync(chatId);

        if (user == null) {
            _logger.LogWarning(LogMessages.UNREGISTERED_USER, chatId);
            return new ServiceResult<AlertDto>(ResultCode.UNREGISTERED_USER, null);
        }

        (Alert? alert, int position) = await FindUserAlertAsync(user, alertId);

        if (alert == null) {
            _logger.LogWarning(LogMessages.ALERT_NOT_FOUND, alertId, chatId);
            return new ServiceResult<AlertDto>(ResultCode.ALERT_NOT_FOUND, null);
        }

        if (!alert.AlertIsActive && await _alertRepository.CountActiveAlertsAsync(user.UserId) >= _limits.MaxActiveAlertsPerUser) {
            _logger.LogWarning("User {UserId} reached the limit of {Limit} active alerts", user.UserId, _limits.MaxActiveAlertsPerUser);
            return new ServiceResult<AlertDto>(ResultCode.ACTIVE_ALERT_LIMIT_REACHED, null);
        }

        alert = await _alertRepository.SetAlertActiveAsync(alert, !alert.AlertIsActive, SystemUsers.FromChat(chatId));
        _alertIndexProvider.Invalidate();

        return new ServiceResult<AlertDto>(ResultCode.NO_ERROR, alert.ToDto(position));
    }

    public async Task<ServiceResult<AlertDto>> PauseAlertAsync(long chatId, int alertId) {
        User? user = await _userRepository.GetUserByChatIdAsync(chatId);

        if (user == null) {
            _logger.LogWarning(LogMessages.UNREGISTERED_USER, chatId);
            return new ServiceResult<AlertDto>(ResultCode.UNREGISTERED_USER, null);
        }

        (Alert? alert, int position) = await FindUserAlertAsync(user, alertId);

        if (alert == null) {
            _logger.LogWarning(LogMessages.ALERT_NOT_FOUND, alertId, chatId);
            return new ServiceResult<AlertDto>(ResultCode.ALERT_NOT_FOUND, null);
        }

        if (alert.AlertIsActive) {
            alert = await _alertRepository.SetAlertActiveAsync(alert, false, SystemUsers.FromChat(chatId));
            _alertIndexProvider.Invalidate();
        }

        return new ServiceResult<AlertDto>(ResultCode.NO_ERROR, alert.ToDto(position));
    }

    public async Task<ServiceResult<AlertDto>> DeleteAlertAsync(long chatId, int alertId) {
        User? user = await _userRepository.GetUserByChatIdAsync(chatId);

        if (user == null) {
            _logger.LogWarning(LogMessages.UNREGISTERED_USER, chatId);
            return new ServiceResult<AlertDto>(ResultCode.UNREGISTERED_USER, null);
        }

        (Alert? alert, int position) = await FindUserAlertAsync(user, alertId);

        if (alert == null) {
            _logger.LogWarning(LogMessages.ALERT_NOT_FOUND, alertId, chatId);
            return new ServiceResult<AlertDto>(ResultCode.ALERT_NOT_FOUND, null);
        }

        AlertDto deleted = alert.ToDto(position);

        await _alertRepository.DeleteAlertAsync(alert, SystemUsers.FromChat(chatId));
        _alertIndexProvider.Invalidate();

        return new ServiceResult<AlertDto>(ResultCode.NO_ERROR, deleted);
    }

    public async Task<ServiceResult<AlertDto>> DeleteAlertByPositionAsync(long chatId, int position) {
        User? user = await _userRepository.GetUserByChatIdAsync(chatId);

        if (user == null) {
            _logger.LogWarning(LogMessages.UNREGISTERED_USER, chatId);
            return new ServiceResult<AlertDto>(ResultCode.UNREGISTERED_USER, null);
        }

        List<Alert> alerts = await _alertRepository.GetUserAlertsAsync(user.UserId);

        if (position < 1 || position > alerts.Count) {
            _logger.LogWarning("Chat {ChatId} has no alert number {Position}", chatId, position);
            return new ServiceResult<AlertDto>(ResultCode.INVALID_ALERT_POSITION, null);
        }

        Alert alert = alerts[position - 1];
        AlertDto deleted = alert.ToDto(position);

        await _alertRepository.DeleteAlertAsync(alert, SystemUsers.FromChat(chatId));
        _alertIndexProvider.Invalidate();

        return new ServiceResult<AlertDto>(ResultCode.NO_ERROR, deleted);
    }

    private ResultCode ValidateAlert(long chatId, string text, decimal? maxPrice, out AlertQuery query) {
        query = AlertQueryParser.Parse(text);

        if (text.Trim().Length > _limits.MaxAlertTextLength) {
            _logger.LogWarning("Alert text of chat {ChatId} longer than {Limit} characters", chatId, _limits.MaxAlertTextLength);
            return ResultCode.ALERT_TEXT_TOO_LONG;
        }

        if (!query.IsValid) {
            _logger.LogWarning(LogMessages.INVALID_ALERT_TEXT, chatId);
            return ResultCode.INVALID_ALERT_TEXT;
        }

        if (maxPrice.HasValue && (maxPrice.Value <= 0 || maxPrice.Value > _limits.MaxPriceLimit)) {
            _logger.LogWarning("Max price {MaxPrice} of chat {ChatId} out of range", maxPrice, chatId);
            return ResultCode.INVALID_MAX_PRICE;
        }

        return ResultCode.NO_ERROR;
    }

    // Only the alerts of the user count, so the alert of someone else is "not found"
    private async Task<(Alert? Alert, int Position)> FindUserAlertAsync(User user, int alertId) {
        List<Alert> alerts = await _alertRepository.GetUserAlertsAsync(user.UserId);
        int index = alerts.FindIndex(a => a.AlertId == alertId);

        return index < 0 ? (null, 0) : (alerts[index], index + 1);
    }
}