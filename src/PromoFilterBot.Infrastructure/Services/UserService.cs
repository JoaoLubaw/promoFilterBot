/*
 * File: UserService.cs
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

public class UserService : IUserService {
    private const int MaxNameLength = 64;
    private const string DefaultName = "Usuário";

    private readonly IUserRepository _userRepository;
    private readonly IAlertIndexProvider _alertIndexProvider;
    private readonly TimeProvider _timeProvider;
    private readonly GeneralOptions _generalOptions;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        IAlertIndexProvider alertIndexProvider,
        TimeProvider timeProvider,
        IOptions<GeneralOptions> generalOptions,
        ILogger<UserService> logger) {
        _userRepository = userRepository;
        _alertIndexProvider = alertIndexProvider;
        _timeProvider = timeProvider;
        _generalOptions = generalOptions.Value;
        _logger = logger;
    }

    public async Task<ServiceResult<UserDto>> RegisterUserAsync(long chatId, string? name) {
        string userName = NormalizeName(name);
        User? user = await _userRepository.GetUserByChatIdAsync(chatId);

        if (user == null) {
            user = await _userRepository.CreateUserAsync(chatId, userName, SystemUsers.FromChat(chatId));
            _logger.LogInformation("Chat {ChatId} registered as user {UserId}", chatId, user.UserId);

            return new ServiceResult<UserDto>(ResultCode.NO_ERROR, user.ToDto(isNew: true));
        }

        if (user.UserName != userName || !user.UserIsActive) {
            bool reactivated = !user.UserIsActive;
            user = await _userRepository.EditUserAsync(user, userName, isActive: true, SystemUsers.FromChat(chatId));

            // The alerts of an inactive user are out of the index
            if (reactivated) _alertIndexProvider.Invalidate();
        }

        return new ServiceResult<UserDto>(ResultCode.NO_ERROR, user.ToDto());
    }

    public async Task<ServiceResult<UserDto>> GetUserAsync(long chatId) {
        User? user = await _userRepository.GetUserByChatIdAsync(chatId);

        if (user == null) {
            _logger.LogWarning(LogMessages.UNREGISTERED_USER, chatId);
            return new ServiceResult<UserDto>(ResultCode.UNREGISTERED_USER, null);
        }

        return new ServiceResult<UserDto>(ResultCode.NO_ERROR, user.ToDto());
    }

    public async Task<ServiceResult<UserDto>> PauseUserAsync(long chatId, string? duration) {
        User? user = await _userRepository.GetUserByChatIdAsync(chatId);

        if (user == null) {
            _logger.LogWarning(LogMessages.UNREGISTERED_USER, chatId);
            return new ServiceResult<UserDto>(ResultCode.UNREGISTERED_USER, null);
        }

        PauseRequest? pauseRequest = PauseDurationParser.Parse(duration);

        if (pauseRequest == null) {
            _logger.LogWarning("Pause duration '{Duration}' from chat {ChatId} could not be read", duration, chatId);
            return new ServiceResult<UserDto>(ResultCode.INVALID_PAUSE_DURATION, null);
        }

        DateTime now = _timeProvider.GetUtcNow().UtcDateTime;
        DateTime? pausedUntil = pauseRequest.ResolveUntil(now, TimeZoneResolver.Resolve(_generalOptions.TimeZone));

        user = await _userRepository.SetUserPausedUntilAsync(user, pausedUntil, SystemUsers.FromChat(chatId));

        return new ServiceResult<UserDto>(ResultCode.NO_ERROR, user.ToDto());
    }

    public async Task<ServiceResult<EmptyDto>> DeleteAccountAsync(long chatId) {
        User? user = await _userRepository.GetUserByChatIdAsync(chatId);

        if (user == null) {
            _logger.LogWarning(LogMessages.UNREGISTERED_USER, chatId);
            return new ServiceResult<EmptyDto>(ResultCode.UNREGISTERED_USER, null);
        }

        await _userRepository.DeleteUserAsync(user.UserId);
        _alertIndexProvider.Invalidate();

        _logger.LogInformation("User {UserId} deleted their account", user.UserId);

        return new ServiceResult<EmptyDto>(ResultCode.NO_ERROR, EmptyDto.Instance);
    }

    public async Task DeactivateUserAsync(int userId) {
        await _userRepository.DeactivateUserAsync(userId, SystemUsers.NOTIFIER);
        _alertIndexProvider.Invalidate();

        _logger.LogInformation("User {UserId} blocked the bot and was deactivated", userId);
    }

    public bool IsAdmin(long chatId) => _generalOptions.IsAdmin(chatId);

    private static string NormalizeName(string? name) {
        string trimmed = name?.Trim() ?? string.Empty;

        if (trimmed.Length == 0) return DefaultName;

        return trimmed.Length > MaxNameLength ? trimmed[..MaxNameLength] : trimmed;
    }
}