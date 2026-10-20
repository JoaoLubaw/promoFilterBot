/*
 * File: IBotMessenger.cs
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

using Telegram.Bot.Types.ReplyMarkups;

namespace PromoFilterBot.Bot.Interfaces;

/// <summary>
/// Everything the bot sends goes through here, within the rate limits of Telegram. A user who blocked the bot raises
/// BotBlockedException.
/// </summary>
public interface IBotMessenger {
    // Sends an HTML message, with the preview of the link given (or without preview). Returns its id.
    Task<int?> SendTextAsync(long chatId, string html, InlineKeyboardMarkup? keyboard = null, string? previewUrl = null, CancellationToken cancellationToken = default);

    // Replaces a message of the bot; when it cannot be edited anymore, sends a new one.
    Task EditTextAsync(long chatId, int messageId, string html, InlineKeyboardMarkup? keyboard = null, CancellationToken cancellationToken = default);

    // Stops the loading of a button, optionally with a notice (or a popup, when showAlert).
    Task AnswerCallbackAsync(string callbackQueryId, string? text = null, bool showAlert = false, CancellationToken cancellationToken = default);
}