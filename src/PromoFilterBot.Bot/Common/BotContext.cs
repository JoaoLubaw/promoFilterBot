/*
 * File: BotContext.cs
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

using PromoFilterBot.Infrastructure.Dto;

namespace PromoFilterBot.Bot.Common;

/// <summary>
/// Who sent the update being handled, and from where: a typed message or a button.
/// </summary>
public sealed class BotContext {
    public required long ChatId { get; init; }

    public required UserDto User { get; init; }

    public bool IsAdmin { get; init; }

    /// <summary>
    /// Id of the button pressed, when the update is one.
    /// </summary>
    public string? CallbackQueryId { get; init; }

    /// <summary>
    /// Message of the bot that holds the button pressed, to answer in place.
    /// </summary>
    public int? MessageId { get; init; }

    public bool CallbackAnswered { get; set; }

    public bool IsCallback => CallbackQueryId != null;
}