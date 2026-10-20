/*
 * File: BotBlockedException.cs
 * Project: Common
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

namespace PromoFilterBot.Bot.Common;

/// <summary>
/// The user blocked the bot (or deleted the chat), so nothing can be sent to them.
/// </summary>
public class BotBlockedException : Exception {
    public BotBlockedException(long chatId, Exception innerException)
        : base($"The bot was blocked in chat {chatId}", innerException) {
        ChatId = chatId;
    }

    public long ChatId { get; }
}