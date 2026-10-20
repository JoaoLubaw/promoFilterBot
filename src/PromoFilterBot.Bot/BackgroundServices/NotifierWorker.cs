/*
 * File: NotifierWorker.cs
 * Project: BackgroundServices
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

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using PromoFilterBot.Bot.Common;
using PromoFilterBot.Bot.Interfaces;
using PromoFilterBot.Bot.Options;
using PromoFilterBot.Infrastructure.Common;
using PromoFilterBot.Infrastructure.Dto;
using PromoFilterBot.Infrastructure.Interfaces;

using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.ReplyMarkups;

namespace PromoFilterBot.Bot.BackgroundServices;

/// <summary>
/// Sends the notifications queued by the matcher as soon as they arrive, a few users at a time, within the limits of
/// Telegram. The notifications still pending from before a restart are queued again when it starts.
/// </summary>
[ExcludeFromCodeCoverage]
public class NotifierWorker : BackgroundService {
    private const int BadRequest = 400;

    private readonly INotificationQueue _notificationQueue;
    private readonly IBotMessenger _messenger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly BotOptions _options;
    private readonly ILogger<NotifierWorker> _logger;
    private readonly ConcurrentDictionary<long, byte> _inFlight = new ConcurrentDictionary<long, byte>();

    public NotifierWorker(
        INotificationQueue notificationQueue,
        IBotMessenger messenger,
        IServiceScopeFactory scopeFactory,
        IOptions<BotOptions> options,
        ILogger<NotifierWorker> logger) {
        _notificationQueue = notificationQueue;
        _messenger = messenger;
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        await RequeuePendingAsync();

        int consumers = Math.Max(1, _options.NotifierConcurrency);

        await Task.WhenAll(Enumerable.Range(0, consumers).Select(_ => ConsumeAsync(stoppingToken)));
    }

    private async Task RequeuePendingAsync() {
        try {
            using IServiceScope scope = _scopeFactory.CreateScope();
            INotificationService notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            List<long> pending = await notificationService.GetPendingNotificationIdsAsync();

            foreach (long notificationId in pending) _notificationQueue.Enqueue(notificationId);

            if (pending.Count > 0) _logger.LogInformation("{Count} pending notifications queued again", pending.Count);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "The pending notifications could not be queued again");
        }
    }

    private async Task ConsumeAsync(CancellationToken stoppingToken) {
        try {
            await foreach (long notificationId in _notificationQueue.ReadAllAsync(stoppingToken)) {
                // The same id may be queued twice around a restart
                if (!_inFlight.TryAdd(notificationId, 0)) continue;

                try {
                    await DispatchAsync(notificationId, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException) {
                    _logger.LogError(ex, "Notification {NotificationId} could not be dispatched", notificationId);
                }
                finally {
                    _inFlight.TryRemove(notificationId, out _);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
            // Stopping
        }
    }

    private async Task DispatchAsync(long notificationId, CancellationToken stoppingToken) {
        using IServiceScope scope = _scopeFactory.CreateScope();
        INotificationService notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        ServiceResult<NotificationDispatchDto> result = await notificationService.PrepareDispatchAsync(notificationId);

        if (!result.IsSuccess || result.Data?.Decision != DispatchDecision.Send) return;

        NotificationDispatchDto notification = result.Data;

        try {
            await SendAsync(notification, stoppingToken);
            await notificationService.MarkSentAsync(notificationId);
        }
        catch (BotBlockedException) {
            IUserService userService = scope.ServiceProvider.GetRequiredService<IUserService>();

            await userService.DeactivateUserAsync(notification.UserId);
            await notificationService.MarkFailedAsync(notificationId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            _logger.LogError(ex, "Notification {NotificationId} could not be sent to chat {ChatId}", notificationId, notification.ChatId);
            await notificationService.MarkFailedAsync(notificationId);
        }
    }

    // The preview shows the product (or the post, without a link), so the photo of the offer comes along
    private async Task SendAsync(NotificationDispatchDto notification, CancellationToken stoppingToken) {
        InlineKeyboardMarkup keyboard = Keyboards.Notification(notification);
        string? previewUrl = notification.OfferLink ?? notification.PostLink;

        try {
            await _messenger.SendTextAsync(notification.ChatId, NotificationFormatter.Format(notification), keyboard, previewUrl, stoppingToken);
        }
        catch (ApiRequestException ex) when (ex.ErrorCode == BadRequest && ex.Message.Contains("can't parse entities", StringComparison.OrdinalIgnoreCase)) {
            // The formatting of the post did not survive the copy, so the plain text goes instead
            _logger.LogWarning("The formatting of notification {NotificationId} was refused ({Error}), sending it as plain text", notification.NotificationId, ex.Message);

            await _messenger.SendTextAsync(notification.ChatId, NotificationFormatter.FormatPlain(notification), keyboard, previewUrl, stoppingToken);
        }
    }
}