/*
 * File: MatcherWorker.cs
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

using System.Diagnostics.CodeAnalysis;

using PromoFilterBot.Domain.Dto;
using PromoFilterBot.Infrastructure.Interfaces;

namespace PromoFilterBot.Host.BackgroundServices;

/// <summary>
/// Takes the posts published by the reader, one at a time, and hands each to the matching service.
/// </summary>
[ExcludeFromCodeCoverage]
public class MatcherWorker : BackgroundService {
    private readonly IOfferQueue _offerQueue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MatcherWorker> _logger;

    public MatcherWorker(IOfferQueue offerQueue, IServiceScopeFactory scopeFactory, ILogger<MatcherWorker> logger) {
        _offerQueue = offerQueue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        try {
            await foreach (ReceivedOfferDto offer in _offerQueue.ReadAllAsync(stoppingToken)) {
                try {
                    using IServiceScope scope = _scopeFactory.CreateScope();
                    IMatchingService matchingService = scope.ServiceProvider.GetRequiredService<IMatchingService>();

                    await matchingService.ProcessOfferAsync(offer, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException) {
                    _logger.LogError(ex, "Post {MessageId} of channel {ChannelId} could not be matched", offer.TelegramMessageId, offer.TelegramChannelId);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
            // Stopping
        }
    }
}