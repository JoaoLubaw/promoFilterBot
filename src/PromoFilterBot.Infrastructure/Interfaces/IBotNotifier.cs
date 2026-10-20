/*
 * File: IBotNotifier.cs
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

namespace PromoFilterBot.Infrastructure.Interfaces;

/// <summary>
/// Messages the bot sends on its own, to the admin or to a user. Implemented by the bot, the only one that talks to the
/// users; a failure to send is logged, never thrown.
/// </summary>
public interface IBotNotifier {
    Task NotifyAdminAsync(string html, CancellationToken cancellationToken = default);

    Task NotifyUserAsync(long chatId, string html, CancellationToken cancellationToken = default);
}