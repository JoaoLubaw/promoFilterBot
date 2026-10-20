/*
 * File: UserServiceTest.cs
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

using Moq;

using PromoFilterBot.Domain.Models;
using PromoFilterBot.Infrastructure.Common;
using PromoFilterBot.Infrastructure.Dto;
using PromoFilterBot.Infrastructure.Interfaces;
using PromoFilterBot.Infrastructure.Options;
using PromoFilterBot.Infrastructure.Services;
using PromoFilterBot.Tests.Helpers;

using MsOptions = Microsoft.Extensions.Options.Options;

namespace PromoFilterBot.Tests.Services;

public class UserServiceTest {
    private readonly Mock<IUserRepository> _mockUserRepository = new();
    private readonly Mock<IAlertIndexProvider> _mockAlertIndexProvider = new();
    private readonly Mock<ILogger<UserService>> _mockLogger = new();
    private readonly FixedTimeProvider _timeProvider = new(TestData.Now);
    private readonly UserService _service;

    public UserServiceTest() {
        GeneralOptions generalOptions = new GeneralOptions { AdminChatId = TestData.AdminChatId, TimeZone = "UTC" };

        _service = new UserService(
            _mockUserRepository.Object,
            _mockAlertIndexProvider.Object,
            _timeProvider,
            MsOptions.Create(generalOptions),
            _mockLogger.Object);

        _mockUserRepository
            .Setup(r => r.SetUserPausedUntilAsync(It.IsAny<User>(), It.IsAny<DateTime?>(), It.IsAny<string>()))
            .ReturnsAsync((User user, DateTime? until, string _) => {
                user.UserPausedUntil = until;
                return user;
            });
    }

    [Fact]
    public async Task RegisterUser_CreatesTheUser_OnTheFirstMessage() {
        _mockUserRepository.Setup(r => r.GetUserByChatIdAsync(TestData.ChatId)).ReturnsAsync((User?)null);
        _mockUserRepository.Setup(r => r.CreateUserAsync(TestData.ChatId, "João", "chat:1001")).ReturnsAsync(TestData.User());

        ServiceResult<UserDto> result = await _service.RegisterUserAsync(TestData.ChatId, " João ");

        Assert.Equal(ResultCode.NO_ERROR, result.Code);
        Assert.True(result.Data!.UserIsNew);
    }

    [Fact]
    public async Task RegisterUser_UsesADefaultName_WhenTelegramHasNone() {
        _mockUserRepository.Setup(r => r.GetUserByChatIdAsync(TestData.ChatId)).ReturnsAsync((User?)null);
        _mockUserRepository.Setup(r => r.CreateUserAsync(TestData.ChatId, "Usuário", It.IsAny<string>())).ReturnsAsync(TestData.User());

        await _service.RegisterUserAsync(TestData.ChatId, "  ");

        _mockUserRepository.Verify(r => r.CreateUserAsync(TestData.ChatId, "Usuário", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RegisterUser_ReactivatesAUserWhoHadBlockedTheBot() {
        User user = TestData.User(isActive: false);
        _mockUserRepository.Setup(r => r.GetUserByChatIdAsync(TestData.ChatId)).ReturnsAsync(user);
        _mockUserRepository.Setup(r => r.EditUserAsync(user, "João", true, It.IsAny<string>())).ReturnsAsync(user);

        ServiceResult<UserDto> result = await _service.RegisterUserAsync(TestData.ChatId, "João");

        Assert.False(result.Data!.UserIsNew);
        _mockUserRepository.Verify(r => r.EditUserAsync(user, "João", true, It.IsAny<string>()), Times.Once);
        _mockAlertIndexProvider.Verify(p => p.Invalidate(), Times.Once);
    }

    [Fact]
    public async Task RegisterUser_ChangesNothing_ForAKnownUser() {
        _mockUserRepository.Setup(r => r.GetUserByChatIdAsync(TestData.ChatId)).ReturnsAsync(TestData.User());

        await _service.RegisterUserAsync(TestData.ChatId, "João");

        _mockUserRepository.Verify(r => r.EditUserAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task PauseUser_ReturnsUnregistered_WhenUserNotFound() {
        _mockUserRepository.Setup(r => r.GetUserByChatIdAsync(TestData.ChatId)).ReturnsAsync((User?)null);

        ServiceResult<UserDto> result = await _service.PauseUserAsync(TestData.ChatId, "8h");

        Assert.Equal(ResultCode.UNREGISTERED_USER, result.Code);
    }

    [Fact]
    public async Task PauseUser_ReturnsInvalidDuration_WhenItCannotBeRead() {
        _mockUserRepository.Setup(r => r.GetUserByChatIdAsync(TestData.ChatId)).ReturnsAsync(TestData.User());

        ServiceResult<UserDto> result = await _service.PauseUserAsync(TestData.ChatId, "um tempinho");

        Assert.Equal(ResultCode.INVALID_PAUSE_DURATION, result.Code);
    }

    [Fact]
    public async Task PauseUser_PausesForTheDuration() {
        _mockUserRepository.Setup(r => r.GetUserByChatIdAsync(TestData.ChatId)).ReturnsAsync(TestData.User());

        ServiceResult<UserDto> result = await _service.PauseUserAsync(TestData.ChatId, "8h");

        Assert.Equal(TestData.Now.AddHours(8), result.Data!.UserPausedUntil);
    }

    [Fact]
    public async Task PauseUser_Resumes() {
        _mockUserRepository.Setup(r => r.GetUserByChatIdAsync(TestData.ChatId)).ReturnsAsync(TestData.User(pausedUntil: TestData.Now.AddHours(3)));

        ServiceResult<UserDto> result = await _service.PauseUserAsync(TestData.ChatId, "retomar");

        Assert.Null(result.Data!.UserPausedUntil);
    }

    [Fact]
    public async Task DeleteAccount_DeletesTheUserAndRebuildsTheIndex() {
        _mockUserRepository.Setup(r => r.GetUserByChatIdAsync(TestData.ChatId)).ReturnsAsync(TestData.User(userId: 7));

        ServiceResult<EmptyDto> result = await _service.DeleteAccountAsync(TestData.ChatId);

        Assert.True(result.IsSuccess);
        _mockUserRepository.Verify(r => r.DeleteUserAsync(7), Times.Once);
        _mockAlertIndexProvider.Verify(p => p.Invalidate(), Times.Once);
    }

    [Fact]
    public async Task DeleteAccount_ReturnsUnregistered_WhenUserNotFound() {
        _mockUserRepository.Setup(r => r.GetUserByChatIdAsync(TestData.ChatId)).ReturnsAsync((User?)null);

        ServiceResult<EmptyDto> result = await _service.DeleteAccountAsync(TestData.ChatId);

        Assert.Equal(ResultCode.UNREGISTERED_USER, result.Code);
        _mockUserRepository.Verify(r => r.DeleteUserAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void IsAdmin_IsOnlyTheConfiguredChat() {
        Assert.True(_service.IsAdmin(TestData.AdminChatId));
        Assert.False(_service.IsAdmin(TestData.ChatId));
    }
}