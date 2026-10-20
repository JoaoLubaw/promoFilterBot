/*
 * File: StartHandler.cs
 * Project: Handlers
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

using PromoFilterBot.Bot.Common;
using PromoFilterBot.Bot.Interfaces;

namespace PromoFilterBot.Bot.Handlers;

/// <summary>
/// /start, /ajuda and whatever the bot does not understand.
/// </summary>
public class StartHandler : BotHandlerBase {
    public StartHandler(IBotMessenger messenger, ILogger<StartHandler> logger) : base(logger, messenger) {
    }

    public async Task HandleStartAsync(BotContext context, CancellationToken cancellationToken) {
        await ExecuteAsync(context, "Start", null, async () => {
            await ReplyAsync(context, BotTexts.Welcome(context.User.UserName), Keyboards.MainMenu(), cancellationToken);
        }, cancellationToken);
    }

    public async Task HandleHelpAsync(BotContext context, CancellationToken cancellationToken) {
        await ExecuteAsync(context, "Help", null, async () => {
            await ReplyAsync(context, BotTexts.Help(context.IsAdmin), Keyboards.MainMenu(), cancellationToken);
        }, cancellationToken);
    }

    public async Task HandleCancelAsync(BotContext context, CancellationToken cancellationToken) {
        await ExecuteAsync(context, "Cancel", null, async () => {
            await ReplyAsync(context, BotTexts.CancelCommand, Keyboards.MainMenu(), cancellationToken);
        }, cancellationToken);
    }

    public async Task HandleUnknownTextAsync(BotContext context, string text, CancellationToken cancellationToken) {
        await ExecuteAsync(context, "UnknownText", text, async () => {
            // The first message of someone may be anything, so they get the welcome
            string html = context.User.UserIsNew ? BotTexts.Welcome(context.User.UserName) : BotTexts.UnknownText(text);

            await SendAsync(context, html, Keyboards.MainMenu(), cancellationToken);
        }, cancellationToken);
    }

    public async Task HandleUnknownCommandAsync(BotContext context, CancellationToken cancellationToken) {
        await ExecuteAsync(context, "UnknownCommand", null, async () => {
            await SendAsync(context, BotTexts.UnknownCommand, Keyboards.MainMenu(), cancellationToken);
        }, cancellationToken);
    }

    public async Task HandleNonTextAsync(BotContext context, CancellationToken cancellationToken) {
        await ExecuteAsync(context, "NonText", null, async () => {
            await SendAsync(context, BotTexts.NonTextMessage, Keyboards.MainMenu(), cancellationToken);
        }, cancellationToken);
    }
}