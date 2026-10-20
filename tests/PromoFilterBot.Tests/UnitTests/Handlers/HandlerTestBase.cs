/*
 * File: HandlerTestBase.cs
 * Project: Handlers
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

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using PromoFilterBot.Bot.Common;
using PromoFilterBot.Bot.Handlers;
using PromoFilterBot.Bot.Interfaces;
using PromoFilterBot.Infrastructure.Common;
using PromoFilterBot.Infrastructure.Dto;
using PromoFilterBot.Infrastructure.Interfaces;
using PromoFilterBot.Tests.Helpers;

using Telegram.Bot.Types.ReplyMarkups;

namespace PromoFilterBot.Tests.Handlers;

/// <summary>
/// The handlers with mocked services, and what they sent.
/// </summary>
public abstract class HandlerTestBase {
    protected const string CallbackQueryId = "cb1";
    protected const int MessageId = 77;

    protected readonly Mock<IBotMessenger> _mockMessenger = new();
    protected readonly Mock<IAlertService> _mockAlertService = new();
    protected readonly Mock<IChannelService> _mockChannelService = new();
    protected readonly Mock<IConversationService> _mockConversationService = new();
    protected readonly Mock<IUserService> _mockUserService = new();
    protected readonly Mock<INotificationService> _mockNotificationService = new();
    protected readonly Mock<IAdminService> _mockAdminService = new();
    protected readonly Mock<ILocalClock> _mockLocalClock = new();
    protected readonly List<string> _sentMessages = [];
    protected readonly List<string> _editedMessages = [];

    protected HandlerTestBase() {
        _mockMessenger
            .Setup(m => m.SendTextAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<InlineKeyboardMarkup?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback((long _, string html, InlineKeyboardMarkup? _, string? _, CancellationToken _) => _sentMessages.Add(html))
            .ReturnsAsync(1);
        _mockMessenger
            .Setup(m => m.EditTextAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<InlineKeyboardMarkup?>(), It.IsAny<CancellationToken>()))
            .Callback((long _, int _, string html, InlineKeyboardMarkup? _, CancellationToken _) => _editedMessages.Add(html))
            .Returns(Task.CompletedTask);

        _mockLocalClock.Setup(c => c.UtcNow).Returns(TestData.Now);
        _mockLocalClock.Setup(c => c.Format(It.IsAny<DateTime>())).Returns("21/10 às 08:00");
    }

    protected static BotContext MessageContext(UserDto? user = null, bool isAdmin = false) {
        return new BotContext { ChatId = TestData.ChatId, User = user ?? TestData.UserDto(), IsAdmin = isAdmin };
    }

    protected static BotContext CallbackContext() {
        return new BotContext { ChatId = TestData.ChatId, User = TestData.UserDto(), CallbackQueryId = CallbackQueryId, MessageId = MessageId };
    }

    protected AlertWizardHandler CreateWizardHandler() {
        return new AlertWizardHandler(
            _mockConversationService.Object, _mockAlertService.Object, _mockMessenger.Object, NullLogger<AlertWizardHandler>.Instance);
    }

    protected AdminHandler CreateAdminHandler() {
        return new AdminHandler(
            _mockAdminService.Object, _mockChannelService.Object, _mockConversationService.Object, _mockLocalClock.Object,
            _mockMessenger.Object, NullLogger<AdminHandler>.Instance);
    }

    protected AlertHandler CreateAlertHandler() {
        return new AlertHandler(
            _mockAlertService.Object, CreateWizardHandler(), _mockLocalClock.Object,
            _mockMessenger.Object, NullLogger<AlertHandler>.Instance);
    }

    protected UpdateRouter CreateRouter() {
        return new UpdateRouter(
            _mockUserService.Object,
            _mockConversationService.Object,
            _mockMessenger.Object,
            new StartHandler(_mockMessenger.Object, NullLogger<StartHandler>.Instance),
            CreateAlertHandler(),
            CreateWizardHandler(),
            new PauseHandler(_mockUserService.Object, _mockLocalClock.Object, _mockMessenger.Object, NullLogger<PauseHandler>.Instance),
            new AccountHandler(_mockUserService.Object, _mockMessenger.Object, NullLogger<AccountHandler>.Instance),
            CreateAdminHandler(),
            new NotificationHandler(_mockAlertService.Object, _mockNotificationService.Object, _mockMessenger.Object, NullLogger<NotificationHandler>.Instance),
            NullLogger<UpdateRouter>.Instance);
    }

    protected static AlertDto AlertDto(int alertId = 10, int position = 1, string text = "rtx 4060", bool isActive = true, decimal? maxPrice = 2000) {
        return new AlertDto {
            AlertId = alertId,
            AlertPosition = position,
            AlertOriginalText = text,
            AlertDescription = text.Replace(" ", " · "),
            AlertIsActive = isActive,
            AlertMaxPrice = maxPrice
        };
    }

    protected static ServiceResult<T> Success<T>(T data) => new ServiceResult<T>(ResultCode.NO_ERROR, data);

    protected static ServiceResult<T> Failure<T>(ResultCode code) => new ServiceResult<T>(code, default);
}