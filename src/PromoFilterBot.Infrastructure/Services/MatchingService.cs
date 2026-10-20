/*
 * File: MatchingService.cs
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

public class MatchingService : IMatchingService {
    private readonly IChannelRepository _channelRepository;
    private readonly IChannelMessageRepository _channelMessageRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAlertIndexProvider _alertIndexProvider;
    private readonly IShortLinkResolver _shortLinkResolver;
    private readonly INotificationQueue _notificationQueue;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly MatchingOptions _matchingOptions;
    private readonly TimeZoneInfo _timeZone;
    private readonly ILogger<MatchingService> _logger;

    public MatchingService(
        IChannelRepository channelRepository,
        IChannelMessageRepository channelMessageRepository,
        INotificationRepository notificationRepository,
        IUserRepository userRepository,
        IAlertIndexProvider alertIndexProvider,
        IShortLinkResolver shortLinkResolver,
        INotificationQueue notificationQueue,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IOptions<MatchingOptions> matchingOptions,
        IOptions<GeneralOptions> generalOptions,
        ILogger<MatchingService> logger) {
        _channelRepository = channelRepository;
        _channelMessageRepository = channelMessageRepository;
        _notificationRepository = notificationRepository;
        _userRepository = userRepository;
        _alertIndexProvider = alertIndexProvider;
        _shortLinkResolver = shortLinkResolver;
        _notificationQueue = notificationQueue;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _matchingOptions = matchingOptions.Value;
        _timeZone = TimeZoneResolver.Resolve(generalOptions.Value.TimeZone);
        _logger = logger;
    }

    public async Task<ServiceResult<MatchingOutcomeDto>> ProcessOfferAsync(ReceivedOfferDto offer, CancellationToken cancellationToken = default) {
        if (offer == null) {
            _logger.LogWarning("Offer without content received by the matcher");
            return new ServiceResult<MatchingOutcomeDto>(ResultCode.EMPTY_REQUEST, null);
        }

        Channel? channel = await _channelRepository.GetChannelByTelegramIdAsync(offer.TelegramChannelId);

        if (channel == null || channel.ChannelStatus != ChannelStatus.Active) {
            _logger.LogDebug("Post {MessageId} of channel {TelegramId}, which is not monitored", offer.TelegramMessageId, offer.TelegramChannelId);
            return new ServiceResult<MatchingOutcomeDto>(ResultCode.CHANNEL_NOT_MONITORED, null);
        }

        if (await _channelMessageRepository.ChannelMessageExistsAsync(channel.ChannelId, offer.TelegramMessageId)) {
            _logger.LogDebug("Post {MessageId} of @{Username} was already processed", offer.TelegramMessageId, channel.ChannelUsername);
            return new ServiceResult<MatchingOutcomeDto>(ResultCode.MESSAGE_ALREADY_PROCESSED, null);
        }

        OfferText offerText = OfferText.FromRawText(offer.Text);

        if (offerText.Tokens.Count == 0) {
            _logger.LogDebug("Post {MessageId} of @{Username} has no text", offer.TelegramMessageId, channel.ChannelUsername);
            return new ServiceResult<MatchingOutcomeDto>(ResultCode.EMPTY_OFFER, null);
        }

        DateTime now = _timeProvider.GetUtcNow().UtcDateTime;
        List<string> links = CollectLinks(offer);
        OfferIdentity identity = await IdentifyOfferAsync(links, offerText, cancellationToken);

        OfferKey offerKey = new OfferKey(
            identity.ProductKey,
            identity.Store,
            PriceExtractor.Extract(offer.Text),
            CouponExtractor.ToKey(CouponExtractor.Extract(offer.Text)),
            DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(now, _timeZone)));

        AlertIndex index = await _alertIndexProvider.GetIndexAsync(cancellationToken);
        IReadOnlyList<AlertMatch> matches = AlertMatcher.FindMatches(index, offerText, offerKey.Price, _matchingOptions.NotifyWhenPriceUnknown);
        List<Notification> notifications = await BuildNotificationsAsync(matches, offerKey, now);

        ChannelMessage channelMessage = new ChannelMessage {
            CreationUser = SystemUsers.MATCHER,
            UpdateUser = SystemUsers.MATCHER,
            ChannelId = channel.ChannelId,
            ChannelMessageTelegramId = offer.TelegramMessageId,
            ChannelMessageText = offer.Text,
            ChannelMessageHtml = offer.Html,
            ChannelMessageLink = identity.OfferLink,
            ChannelMessageDetectedPrice = offerKey.Price,
            ChannelMessageProductKey = offerKey.ProductKey,
            ChannelMessageStore = offerKey.Store,
            ChannelMessageCoupon = offerKey.Coupon,
            ChannelMessageReceivedAt = offer.ReceivedAt
        };

        // The message and its notifications are saved together, and only then queued to the notifier
        await _unitOfWork.ExecuteInTransactionAsync(async () => {
            await _channelMessageRepository.CreateChannelMessageAsync(channelMessage);

            if (notifications.Count > 0) {
                foreach (Notification notification in notifications) notification.ChannelMessageId = channelMessage.ChannelMessageId;

                await _notificationRepository.CreateNotificationsAsync(notifications);
            }

            _unitOfWork.AfterCompletion(committed => {
                if (committed) {
                    foreach (Notification notification in notifications) _notificationQueue.Enqueue(notification.NotificationId);
                }

                return Task.CompletedTask;
            });

            return true;
        }, saved => saved);

        _logger.LogInformation(
            "Post {MessageId} of @{Username} ({ProductKey} at {Store}, price {Price}, coupon {Coupon}) matched {Matches} alerts: {Notifications} notifications",
            offer.TelegramMessageId, channel.ChannelUsername, offerKey.ProductKey, offerKey.Store, offerKey.Price, offerKey.Coupon,
            matches.Count, notifications.Count);

        MatchingOutcomeDto outcome = new MatchingOutcomeDto {
            ChannelMessageId = channelMessage.ChannelMessageId,
            ProductKey = offerKey.ProductKey,
            Store = offerKey.Store,
            DetectedPrice = offerKey.Price,
            Coupon = offerKey.Coupon,
            MatchedAlerts = matches.Count,
            NotificationIds = notifications.Select(n => n.NotificationId).ToList()
        };

        return new ServiceResult<MatchingOutcomeDto>(ResultCode.NO_ERROR, outcome);
    }

    private static List<string> CollectLinks(ReceivedOfferDto offer) {
        return offer.Links
            .Select(LinkExtractor.Normalize)
            .OfType<string>()
            .Concat(LinkExtractor.Extract(offer.Text))
            .Where(link => !LinkExtractor.IsTelegramLink(link))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    // The link kept is the one posted, with the affiliate code of the channel, even when a short link had to be followed
    private async Task<OfferIdentity> IdentifyOfferAsync(List<string> links, OfferText offerText, CancellationToken cancellationToken) {
        foreach (string link in links) {
            string? key = ProductKeyGenerator.FromLink(link);

            if (key != null) return new OfferIdentity(key, StoreDetector.FromLink(link) ?? StoreDetector.Unknown, link);
        }

        List<string> resolvedLinks = [];

        if (_matchingOptions.ResolveShortLinks) {
            foreach (string link in links.Where(_shortLinkResolver.IsShortLink)) {
                string? resolved = await _shortLinkResolver.ResolveAsync(link, cancellationToken);
                string? key = ProductKeyGenerator.FromLink(resolved);

                if (key != null) return new OfferIdentity(key, StoreDetector.FromLink(resolved) ?? StoreDetector.Unknown, link);
                if (resolved != null) resolvedLinks.Add(resolved);
            }
        }

        string store = StoreDetector.Detect(links.Concat(resolvedLinks), offerText.NormalizedText);

        return new OfferIdentity(ProductKeyGenerator.FromText(offerText.NormalizedText), store, links.FirstOrDefault());
    }

    private async Task<List<Notification>> BuildNotificationsAsync(IReadOnlyList<AlertMatch> matches, OfferKey offer, DateTime now) {
        if (matches.Count == 0) return [];

        // One notification per user, even when several of their alerts match
        List<AlertMatch> matchesByUser = matches
            .GroupBy(m => m.Alert.UserId)
            .Select(g => g.OrderBy(m => m.PriceUnknown).ThenBy(m => m.Alert.AlertId).First())
            .ToList();

        List<int> userIds = matchesByUser.Select(m => m.Alert.UserId).ToList();
        List<User> users = await _userRepository.GetUsersByIdsAsync(userIds);
        HashSet<int> reachableUserIds = users
            .Where(u => u.UserIsActive && !u.IsDeleted && !(u.UserPausedUntil > now))
            .Select(u => u.UserId)
            .ToHashSet();

        if (reachableUserIds.Count == 0) return [];

        // The same offer posted by several channels reaches each user once a day; another price, coupon or store is
        // another offer, sent right away
        List<int> alreadyNotified = await _notificationRepository.GetUserIdsWithOfferAsync(
            reachableUserIds, offer.ProductKey, offer.Store, offer.Price, offer.Coupon, offer.Date);

        reachableUserIds.ExceptWith(alreadyNotified);

        return matchesByUser
            .Where(m => reachableUserIds.Contains(m.Alert.UserId))
            .Select(m => new Notification {
                CreationUser = SystemUsers.MATCHER,
                UpdateUser = SystemUsers.MATCHER,
                UserId = m.Alert.UserId,
                AlertId = m.Alert.AlertId,
                NotificationProductKey = offer.ProductKey,
                NotificationStore = offer.Store,
                NotificationPrice = offer.Price,
                NotificationCoupon = offer.Coupon,
                NotificationOfferDate = offer.Date,
                NotificationStatus = NotificationStatus.Pending
            })
            .ToList();
    }

    private sealed record OfferIdentity(string ProductKey, string Store, string? OfferLink);

    // What tells two offers apart: product x store x price x coupon, on a day
    private sealed record OfferKey(string ProductKey, string Store, decimal? Price, string Coupon, DateOnly Date);
}