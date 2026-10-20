/*
 * File: MatchingServiceTest.cs
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

using PromoFilterBot.Domain.Common;
using PromoFilterBot.Domain.Dto;
using PromoFilterBot.Domain.Models;
using PromoFilterBot.Infrastructure.Common;
using PromoFilterBot.Infrastructure.Dto;
using PromoFilterBot.Infrastructure.Interfaces;
using PromoFilterBot.Infrastructure.Options;
using PromoFilterBot.Infrastructure.Services;
using PromoFilterBot.Tests.Helpers;

using MsOptions = Microsoft.Extensions.Options.Options;

namespace PromoFilterBot.Tests.Services;

public class MatchingServiceTest {
    private const long TelegramChannelId = 777;
    private const string AmazonLink = "https://www.amazon.com.br/dp/B0BX4KQ3Z5?tag=lobao-20";
    private const string AmazonKey = "amazon:B0BX4KQ3Z5";

    // TestData.Now (15h UTC) is noon in São Paulo, on the same day
    private static readonly DateOnly Today = new DateOnly(2026, 10, 20);

    private readonly Mock<IChannelRepository> _mockChannelRepository = new();
    private readonly Mock<IChannelMessageRepository> _mockChannelMessageRepository = new();
    private readonly Mock<INotificationRepository> _mockNotificationRepository = new();
    private readonly Mock<IUserRepository> _mockUserRepository = new();
    private readonly Mock<IAlertIndexProvider> _mockAlertIndexProvider = new();
    private readonly Mock<IShortLinkResolver> _mockShortLinkResolver = new();
    private readonly Mock<INotificationQueue> _mockNotificationQueue = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<ILogger<MatchingService>> _mockLogger = new();
    private readonly List<Func<bool, Task>> _afterCompletion = [];
    private readonly List<Notification> _savedNotifications = [];
    private readonly MatchingService _service;

    private ChannelMessage? _savedMessage;

    public MatchingServiceTest() {
        _service = new MatchingService(
            _mockChannelRepository.Object,
            _mockChannelMessageRepository.Object,
            _mockNotificationRepository.Object,
            _mockUserRepository.Object,
            _mockAlertIndexProvider.Object,
            _mockShortLinkResolver.Object,
            _mockNotificationQueue.Object,
            _mockUnitOfWork.Object,
            new FixedTimeProvider(TestData.Now),
            MsOptions.Create(new MatchingOptions { NotifyWhenPriceUnknown = true, ResolveShortLinks = true }),
            MsOptions.Create(new GeneralOptions { TimeZone = "America/Sao_Paulo" }),
            _mockLogger.Object);

        _mockChannelRepository.Setup(r => r.GetChannelByTelegramIdAsync(TelegramChannelId))
            .ReturnsAsync(TestData.Channel(3, "lobao", ChannelStatus.Active, TelegramChannelId));
        _mockAlertIndexProvider.Setup(p => p.GetIndexAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(AlertIndex.Build([TestData.IndexedAlert(10, 1, "rtx 4060", 2000), TestData.IndexedAlert(11, 1, "4060"), TestData.IndexedAlert(20, 2, "mouse g305")]));
        _mockUserRepository.Setup(r => r.GetUsersByIdsAsync(It.IsAny<IReadOnlyCollection<int>>()))
            .ReturnsAsync((IReadOnlyCollection<int> ids) => ids.Select(id => TestData.User(userId: id, chatId: 1000 + id)).ToList());
        _mockNotificationRepository
            .Setup(r => r.GetUserIdsWithOfferAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal?>(), It.IsAny<string>(), It.IsAny<DateOnly>()))
            .ReturnsAsync([]);

        _mockChannelMessageRepository.Setup(r => r.CreateChannelMessageAsync(It.IsAny<ChannelMessage>()))
            .ReturnsAsync((ChannelMessage message) => {
                message.ChannelMessageId = 500;
                _savedMessage = message;
                return message;
            });
        _mockNotificationRepository.Setup(r => r.CreateNotificationsAsync(It.IsAny<IReadOnlyCollection<Notification>>()))
            .Callback((IReadOnlyCollection<Notification> notifications) => {
                long id = 900;
                foreach (Notification notification in notifications) notification.NotificationId = id++;
                _savedNotifications.AddRange(notifications);
            })
            .Returns(Task.CompletedTask);

        // Runs the operation, and then the actions of the end of the transaction, as a commit
        _mockUnitOfWork.Setup(u => u.AfterCompletion(It.IsAny<Func<bool, Task>>())).Callback((Func<bool, Task> action) => _afterCompletion.Add(action));
        _mockUnitOfWork.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<bool>>>(), It.IsAny<Func<bool, bool>>()))
            .Returns(async (Func<Task<bool>> operation, Func<bool, bool> _) => {
                bool result = await operation();
                foreach (Func<bool, Task> action in _afterCompletion) await action(true);
                return result;
            });
    }

    private static ReceivedOfferDto Offer(string text, params string[] links) {
        return new ReceivedOfferDto { TelegramChannelId = TelegramChannelId, TelegramMessageId = 55, Text = text, Links = links, ReceivedAt = TestData.Now };
    }

    // The user already got the offer at this price and coupon today
    private void AlreadyNotified(decimal? price, string coupon = "", int userId = 1) {
        _mockNotificationRepository
            .Setup(r => r.GetUserIdsWithOfferAsync(It.IsAny<IReadOnlyCollection<int>>(), AmazonKey, "amazon", price, coupon, Today))
            .ReturnsAsync([userId]);
    }

    [Fact]
    public async Task ProcessOffer_ReturnsNotMonitored_ForAnUnknownChannel() {
        _mockChannelRepository.Setup(r => r.GetChannelByTelegramIdAsync(TelegramChannelId)).ReturnsAsync((Channel?)null);

        ServiceResult<MatchingOutcomeDto> result = await _service.ProcessOfferAsync(Offer("RTX 4060"));

        Assert.Equal(ResultCode.CHANNEL_NOT_MONITORED, result.Code);
    }

    [Fact]
    public async Task ProcessOffer_ReturnsNotMonitored_ForABlockedChannel() {
        _mockChannelRepository.Setup(r => r.GetChannelByTelegramIdAsync(TelegramChannelId))
            .ReturnsAsync(TestData.Channel(3, "lobao", ChannelStatus.Blocked, TelegramChannelId));

        ServiceResult<MatchingOutcomeDto> result = await _service.ProcessOfferAsync(Offer("RTX 4060"));

        Assert.Equal(ResultCode.CHANNEL_NOT_MONITORED, result.Code);
    }

    [Fact]
    public async Task ProcessOffer_ReturnsAlreadyProcessed_ForAPostSeenBefore() {
        _mockChannelMessageRepository.Setup(r => r.ChannelMessageExistsAsync(3, 55)).ReturnsAsync(true);

        ServiceResult<MatchingOutcomeDto> result = await _service.ProcessOfferAsync(Offer("RTX 4060"));

        Assert.Equal(ResultCode.MESSAGE_ALREADY_PROCESSED, result.Code);
    }

    [Fact]
    public async Task ProcessOffer_ReturnsEmptyOffer_WithoutText() {
        ServiceResult<MatchingOutcomeDto> result = await _service.ProcessOfferAsync(Offer("🔥🔥🔥"));

        Assert.Equal(ResultCode.EMPTY_OFFER, result.Code);
    }

    [Fact]
    public async Task ProcessOffer_SavesTheOfferAndQueuesOneNotificationPerUser() {
        ServiceResult<MatchingOutcomeDto> result = await _service.ProcessOfferAsync(Offer("RTX 4060 por R$ 1.899", AmazonLink));

        Assert.Equal(ResultCode.NO_ERROR, result.Code);
        Assert.Equal(AmazonKey, result.Data!.ProductKey);
        Assert.Equal("amazon", result.Data.Store);
        Assert.Equal(1899m, result.Data.DetectedPrice);
        Assert.Equal(2, result.Data.MatchedAlerts);

        Notification notification = Assert.Single(_savedNotifications);
        Assert.Equal(1, notification.UserId);
        Assert.Equal(10, notification.AlertId);
        Assert.Equal(500, notification.ChannelMessageId);
        Assert.Equal(1899m, notification.NotificationPrice);
        Assert.Equal(AmazonLink, _savedMessage!.ChannelMessageLink);
        _mockNotificationQueue.Verify(q => q.Enqueue(900), Times.Once);
    }

    [Fact]
    public async Task ProcessOffer_SavesTheOffer_EvenWithoutAMatch() {
        ServiceResult<MatchingOutcomeDto> result = await _service.ProcessOfferAsync(Offer("Teclado mecânico R$ 199"));

        Assert.Equal(0, result.Data!.MatchedAlerts);
        Assert.NotNull(_savedMessage);
        Assert.Empty(_savedNotifications);
    }

    [Fact]
    public async Task ProcessOffer_LeavesOutThePausedUsers() {
        _mockUserRepository.Setup(r => r.GetUsersByIdsAsync(It.IsAny<IReadOnlyCollection<int>>()))
            .ReturnsAsync([TestData.User(userId: 1, pausedUntil: TestData.Now.AddHours(1))]);

        await _service.ProcessOfferAsync(Offer("RTX 4060 por R$ 1.899"));

        Assert.Empty(_savedNotifications);
    }

    [Fact]
    public async Task ProcessOffer_SavesTheOfferAsProductStorePriceAndCoupon() {
        ReceivedOfferDto offer = new ReceivedOfferDto {
            TelegramChannelId = TelegramChannelId,
            TelegramMessageId = 55,
            Text = "RTX 4060 por R$ 1.899\nCupom: TECH10",
            Html = "<b>RTX 4060</b> por R$ 1.899\nCupom: <code>TECH10</code>",
            Links = [AmazonLink],
            ReceivedAt = TestData.Now
        };

        ServiceResult<MatchingOutcomeDto> result = await _service.ProcessOfferAsync(offer);

        Assert.Equal("TECH10", result.Data!.Coupon);
        Notification notification = Assert.Single(_savedNotifications);
        Assert.Equal(AmazonKey, notification.NotificationProductKey);
        Assert.Equal("amazon", notification.NotificationStore);
        Assert.Equal(1899m, notification.NotificationPrice);
        Assert.Equal("TECH10", notification.NotificationCoupon);
        Assert.Equal(Today, notification.NotificationOfferDate);
        Assert.Equal(offer.Html, _savedMessage!.ChannelMessageHtml);
        Assert.Equal("amazon", _savedMessage.ChannelMessageStore);
        Assert.Equal("TECH10", _savedMessage.ChannelMessageCoupon);
    }

    [Fact]
    public async Task ProcessOffer_DoesNotSendTheSameOfferTwice_OnTheSameDay() {
        AlreadyNotified(1899m);

        await _service.ProcessOfferAsync(Offer("RTX 4060 por R$ 1.899", AmazonLink));

        Assert.Empty(_savedNotifications);
        Assert.NotNull(_savedMessage);
    }

    [Theory]
    [InlineData("RTX 4060 por R$ 1.799")]
    [InlineData("RTX 4060 por R$ 1.999")]
    [InlineData("RTX 4060 por R$ 1.899\nCupom: TECH10")]
    public async Task ProcessOffer_SendsTheProductAgain_WithAnotherPriceOrCoupon(string text) {
        AlreadyNotified(1899m);

        await _service.ProcessOfferAsync(Offer(text, AmazonLink));

        Assert.Equal(1, Assert.Single(_savedNotifications).UserId);
    }

    [Fact]
    public async Task ProcessOffer_IdentifiesTheProductBehindAShortLink_KeepingTheLinkPosted() {
        _mockShortLinkResolver.Setup(r => r.IsShortLink("https://amzn.to/3abc")).Returns(true);
        _mockShortLinkResolver.Setup(r => r.ResolveAsync("https://amzn.to/3abc", It.IsAny<CancellationToken>())).ReturnsAsync(AmazonLink);

        ServiceResult<MatchingOutcomeDto> result = await _service.ProcessOfferAsync(Offer("Mouse G305 R$ 149", "https://amzn.to/3abc"));

        Assert.Equal(AmazonKey, result.Data!.ProductKey);
        Assert.Equal("amazon", result.Data.Store);
        Assert.Equal("https://amzn.to/3abc", _savedMessage!.ChannelMessageLink);
    }

    [Fact]
    public async Task ProcessOffer_HashesTheText_WithoutAKnownLink() {
        ServiceResult<MatchingOutcomeDto> result = await _service.ProcessOfferAsync(Offer("Mouse G305 R$ 149"));

        Assert.StartsWith(ProductKeyGenerator.TextKeyPrefix, result.Data!.ProductKey);
        Assert.Equal(StoreDetector.Unknown, result.Data.Store);
        Assert.Equal(20, Assert.Single(_savedNotifications).AlertId);
    }

    [Fact]
    public async Task ProcessOffer_TakesTheStoreFromTheText_WithoutAKnownLink() {
        ServiceResult<MatchingOutcomeDto> result = await _service.ProcessOfferAsync(Offer("Mouse G305 R$ 149 na KaBuM!", "https://bit.ly/x"));

        Assert.Equal("kabum", result.Data!.Store);
    }
}