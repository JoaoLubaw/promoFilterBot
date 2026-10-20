/*
 * File: OfferQueue.cs
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

using System.Threading.Channels;

using Microsoft.Extensions.Options;

using PromoFilterBot.Domain.Dto;
using PromoFilterBot.Infrastructure.Interfaces;
using PromoFilterBot.Infrastructure.Options;

namespace PromoFilterBot.Infrastructure.Services;

/// <summary>
/// Bounded, so a burst of posts makes the reader wait for the matcher instead of filling the memory.
/// </summary>
public sealed class OfferQueue : IOfferQueue {
    private readonly Channel<ReceivedOfferDto> _channel;

    public OfferQueue(IOptions<MatchingOptions> matchingOptions) {
        _channel = Channel.CreateBounded<ReceivedOfferDto>(new BoundedChannelOptions(matchingOptions.Value.OfferQueueCapacity) {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true
        });
    }

    public ValueTask EnqueueAsync(ReceivedOfferDto offer, CancellationToken cancellationToken = default) {
        return _channel.Writer.WriteAsync(offer, cancellationToken);
    }

    public IAsyncEnumerable<ReceivedOfferDto> ReadAllAsync(CancellationToken cancellationToken = default) {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}