/*
 * File: ITelegramRateLimiter.cs
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

namespace PromoFilterBot.Bot.Interfaces;

public interface ITelegramRateLimiter {
    // Waits until a message can be sent to the chat within the global and per chat limits.
    Task WaitAsync(long chatId, CancellationToken cancellationToken = default);
}