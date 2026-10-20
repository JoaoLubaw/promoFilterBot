/*
 * File: IMatchingService.cs
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
using PromoFilterBot.Infrastructure.Common;
using PromoFilterBot.Infrastructure.Dto;

namespace PromoFilterBot.Infrastructure.Interfaces;

public interface IMatchingService {
    // Saves the offer, finds the alerts it matches and queues a notification for each of their owners.
    Task<ServiceResult<MatchingOutcomeDto>> ProcessOfferAsync(ReceivedOfferDto offer, CancellationToken cancellationToken = default);
}