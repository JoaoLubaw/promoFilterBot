/*
 * File: NotificationServiceTest.cs
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
using PromoFilterBot.Infrastructure.Services;
using PromoFilterBot.Tests.Helpers;

namespace PromoFilterBot.Tests.Services;

public class NotificationServiceTest {
    private readonly Mock<INotificationRepository> _mockNotificationRepository = new();
    private readonly Mock<IUserRepository> _mockUserRepository = new();
    private readonly Mock<ILogger<NotificationService>> _mockLogger = new();
    private readonly NotificationService _service;

    public NotificationServiceTest() {
        _service = new NotificationService(
            _mockNotificationRepository.Object,
            _mockUserRepository.Object,
            new FixedTimeProvider(TestData.Now),
            _mockLogger.Object);
    }

    private static Notification Notification(User? user = null, Alert? alert = null, decimal? price = 1899, NotificationStatus status = NotificationStatus.Pending) {
        user ??= TestData.User();
        alert ??= TestData.Alert(10, user.UserId, "rtx 4060", 2000);

        return new Notification {
            NotificationId = 900,
            UserId = user.UserId,
            User = user,
            AlertId = alert.AlertId,
            Alert = alert,
            ChannelMessageId = 500,
            ChannelMessage = new ChannelMessage {
                ChannelMessageId = 500,
                ChannelMessageTelegramId = 55,
                ChannelMessageText = "RTX 4060 por R$ 1.899",
                ChannelMessageHtml = "<b>RTX 4060</b> por R$ 1.899",
                ChannelMessageLink = "https://amzn.to/3abc",
                ChannelMessageProductKey = "amazon:B0BX4KQ3Z5",
                Channel = TestData.Channel(3, "lobao")
            },
            NotificationProductKey = "amazon:B0BX4KQ3Z5",
            NotificationPrice = price,
            NotificationStatus = status
        };
    }

    [Fact]
    public async Task PrepareDispatch_ReturnsNotFound_ForAnUnknownNotification() {
        _mockNotificationRepository.Setup(r => r.GetNotificationForDispatchAsync(900)).ReturnsAsync((Notification?)null);

        ServiceResult<NotificationDispatchDto> result = await _service.PrepareDispatchAsync(900);

        Assert.Equal(ResultCode.NOTIFICATION_NOT_FOUND, result.Code);
    }

    [Fact]
    public async Task PrepareDispatch_ReturnsAlreadyHandled_ForANotificationNotPending() {
        _mockNotificationRepository.Setup(r => r.GetNotificationForDispatchAsync(900)).ReturnsAsync(Notification(status: NotificationStatus.Sent));

        ServiceResult<NotificationDispatchDto> result = await _service.PrepareDispatchAsync(900);

        Assert.Equal(ResultCode.NOTIFICATION_ALREADY_HANDLED, result.Code);
    }

    [Fact]
    public async Task PrepareDispatch_Skips_WhenTheUserPausedMeanwhile() {
        Notification notification = Notification(user: TestData.User(pausedUntil: TestData.Now.AddHours(1)));
        _mockNotificationRepository.Setup(r => r.GetNotificationForDispatchAsync(900)).ReturnsAsync(notification);

        ServiceResult<NotificationDispatchDto> result = await _service.PrepareDispatchAsync(900);

        Assert.Equal(DispatchDecision.Skip, result.Data!.Decision);
        _mockNotificationRepository.Verify(r => r.SetNotificationStatusAsync(notification, NotificationStatus.Skipped, null, SystemUsers.NOTIFIER), Times.Once);
    }

    [Fact]
    public async Task PrepareDispatch_Skips_WhenTheAlertWasPaused() {
        Notification notification = Notification(alert: TestData.Alert(10, 1, "rtx 4060", isActive: false));
        _mockNotificationRepository.Setup(r => r.GetNotificationForDispatchAsync(900)).ReturnsAsync(notification);

        ServiceResult<NotificationDispatchDto> result = await _service.PrepareDispatchAsync(900);

        Assert.Equal(DispatchDecision.Skip, result.Data!.Decision);
    }

    [Fact]
    public async Task PrepareDispatch_SendsWithWhatTheMessageNeeds() {
        _mockNotificationRepository.Setup(r => r.GetNotificationForDispatchAsync(900)).ReturnsAsync(Notification(price: null));

        ServiceResult<NotificationDispatchDto> result = await _service.PrepareDispatchAsync(900);

        NotificationDispatchDto dispatch = result.Data!;
        Assert.Equal(DispatchDecision.Send, dispatch.Decision);
        Assert.Equal(TestData.ChatId, dispatch.ChatId);
        Assert.Equal("https://t.me/lobao/55", dispatch.PostLink);
        Assert.Equal("https://amzn.to/3abc", dispatch.OfferLink);
        Assert.Equal("<b>RTX 4060</b> por R$ 1.899", dispatch.OfferHtml);
        Assert.True(dispatch.PriceUnknown);
        Assert.Equal("rtx 4060", dispatch.AlertText);
    }

    [Fact]
    public async Task PrepareDispatch_SendsEveryOffer_WithoutAnHourlyLimit() {
        _mockNotificationRepository.Setup(r => r.GetNotificationForDispatchAsync(900)).ReturnsAsync(Notification());

        ServiceResult<NotificationDispatchDto> result = await _service.PrepareDispatchAsync(900);

        Assert.Equal(DispatchDecision.Send, result.Data!.Decision);
        _mockNotificationRepository.Verify(r => r.SetNotificationStatusAsync(It.IsAny<Notification>(), It.IsAny<NotificationStatus>(), It.IsAny<DateTime?>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task MarkSent_SavesWhenItWasSent() {
        await _service.MarkSentAsync(900);

        _mockNotificationRepository.Verify(r => r.SetNotificationsStatusAsync(
            It.Is<IReadOnlyCollection<long>>(ids => ids.SequenceEqual(new[] { 900L })), NotificationStatus.Sent, TestData.Now, SystemUsers.NOTIFIER), Times.Once);
    }

    [Fact]
    public async Task RegisterFeedback_ReturnsNotFound_ForTheNotificationOfSomeoneElse() {
        _mockUserRepository.Setup(r => r.GetUserByChatIdAsync(TestData.ChatId)).ReturnsAsync(TestData.User(userId: 1));
        _mockNotificationRepository.Setup(r => r.GetNotificationByIdAsync(900)).ReturnsAsync(Notification(user: TestData.User(userId: 2)));

        ServiceResult<NotificationFeedbackDto> result = await _service.RegisterFeedbackAsync(TestData.ChatId, 900);

        Assert.Equal(ResultCode.NOTIFICATION_NOT_FOUND, result.Code);
    }

    [Fact]
    public async Task RegisterFeedback_MarksTheOfferAsNotRelevant() {
        Notification notification = Notification();
        _mockUserRepository.Setup(r => r.GetUserByChatIdAsync(TestData.ChatId)).ReturnsAsync(TestData.User(userId: 1));
        _mockNotificationRepository.Setup(r => r.GetNotificationByIdAsync(900)).ReturnsAsync(notification);

        ServiceResult<NotificationFeedbackDto> result = await _service.RegisterFeedbackAsync(TestData.ChatId, 900);

        Assert.Equal(10, result.Data!.AlertId);
        _mockNotificationRepository.Verify(r => r.SetNotificationFeedbackAsync(notification, NotificationFeedback.NotRelevant, "chat:1001"), Times.Once);
    }
}