/*
 * File: ReaderUpdateHandler.cs
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

using PromoFilterBot.Domain.Dto;
using PromoFilterBot.Infrastructure.Interfaces;
using PromoFilterBot.Reader.Common;
using PromoFilterBot.Reader.Interfaces;

using TL;

namespace PromoFilterBot.Reader.Services;

public sealed class ReaderUpdateHandler : IReaderUpdateHandler {
    private readonly IOfferQueue _offerQueue;
    private readonly IActiveChannelCache _activeChannelCache;
    private readonly IReaderStatusService _readerStatusService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ReaderUpdateHandler> _logger;

    public ReaderUpdateHandler(
        IOfferQueue offerQueue,
        IActiveChannelCache activeChannelCache,
        IReaderStatusService readerStatusService,
        TimeProvider timeProvider,
        ILogger<ReaderUpdateHandler> logger) {
        _offerQueue = offerQueue;
        _activeChannelCache = activeChannelCache;
        _readerStatusService = readerStatusService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task HandleAsync(Update update) {
        if (update is not UpdateNewChannelMessage { message: Message message }) return;
        if (message.peer_id is not PeerChannel peer) return;
        if (!_activeChannelCache.IsMonitored(peer.channel_id)) return;

        DateTime now = _timeProvider.GetUtcNow().UtcDateTime;
        ReceivedOfferDto? offer = ReaderMessageMapper.Map(message, peer.channel_id, now);

        _readerStatusService.ReportMessageReceived(now);

        if (offer == null) return;

        _logger.LogDebug("Post {MessageId} received from channel {ChannelId}", message.id, peer.channel_id);

        await _offerQueue.EnqueueAsync(offer);
    }
}