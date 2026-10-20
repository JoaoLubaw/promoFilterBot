/*
 * File: IOfferQueue.cs
 * Project: Interfaces
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

using PromoFilterBot.Domain.Dto;

namespace PromoFilterBot.Infrastructure.Interfaces;

/// <summary>
/// Posts read by the reader, waiting for the matcher. The reader only publishes here, so a second reader account
/// with other channels only needs to publish here too.
/// </summary>
public interface IOfferQueue {
    ValueTask EnqueueAsync(ReceivedOfferDto offer, CancellationToken cancellationToken = default);

    IAsyncEnumerable<ReceivedOfferDto> ReadAllAsync(CancellationToken cancellationToken = default);
}