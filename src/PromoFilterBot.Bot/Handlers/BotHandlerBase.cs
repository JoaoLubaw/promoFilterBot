/*
 * File: BotHandlerBase.cs
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
using PromoFilterBot.Infrastructure.Common;

using Telegram.Bot.Types.ReplyMarkups;

namespace PromoFilterBot.Bot.Handlers;

/// <summary>
/// Base of the handlers. A handler only talks to Telegram: it opens the log scope, calls the service, turns its result
/// into a message, and turns an unexpected exception into a handled error.
/// </summary>
public abstract class BotHandlerBase {
    protected const string LogScopeTemplate = "{@Action}, {@Arguments}, {@ChatId}";

    protected readonly ILogger _logger;
    protected readonly IBotMessenger _messenger;

    protected BotHandlerBase(ILogger logger, IBotMessenger messenger) {
        _logger = logger;
        _messenger = messenger;
    }

    protected async Task ExecuteAsync(BotContext context, string action, object? arguments, Func<Task> handle, CancellationToken cancellationToken) {
        using IDisposable? scope = _logger.BeginScope(LogScopeTemplate, action, arguments, context.ChatId);

        try {
            await handle();
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not BotBlockedException) {
            await HandleExceptionAsync(context, ex, cancellationToken);
        }
    }

    /// <summary>
    /// Answers in place when the update is a button (the message of the button is replaced), or with a new message.
    /// </summary>
    protected async Task ReplyAsync(BotContext context, string html, InlineKeyboardMarkup? keyboard, CancellationToken cancellationToken) {
        if (context.IsCallback && context.MessageId.HasValue) {
            await _messenger.EditTextAsync(context.ChatId, context.MessageId.Value, html, keyboard, cancellationToken);
        }
        else {
            await _messenger.SendTextAsync(context.ChatId, html, keyboard, null, cancellationToken);
        }
    }

    protected async Task SendAsync(BotContext context, string html, InlineKeyboardMarkup? keyboard, CancellationToken cancellationToken) {
        await _messenger.SendTextAsync(context.ChatId, html, keyboard, null, cancellationToken);
    }

    /// <summary>
    /// Tells the user why the action failed: in a popup when it came from a button, or in a message.
    /// </summary>
    protected async Task ReplyErrorAsync(BotContext context, ResultCode code, CancellationToken cancellationToken) {
        string message = ResultMessages.Get(code);

        if (context.IsCallback && !context.CallbackAnswered) {
            await AnswerAsync(context, message, showAlert: true, cancellationToken);
            return;
        }

        await SendAsync(context, $"⚠️ {Html.Escape(message)}", null, cancellationToken);
    }

    /// <summary>
    /// Stops the loading of the button pressed. Telegram allows one answer per button.
    /// </summary>
    protected async Task AnswerAsync(BotContext context, string? text, bool showAlert, CancellationToken cancellationToken) {
        if (!context.IsCallback || context.CallbackAnswered) return;

        context.CallbackAnswered = true;
        await _messenger.AnswerCallbackAsync(context.CallbackQueryId!, text, showAlert, cancellationToken);
    }

    private async Task HandleExceptionAsync(BotContext context, Exception exception, CancellationToken cancellationToken) {
        _logger.LogError(exception, "Update of chat {ChatId} failed", context.ChatId);

        try {
            await ReplyErrorAsync(context, ResultCode.UNHANDLED_ERROR, cancellationToken);
        }
        catch (Exception replyException) when (replyException is not OperationCanceledException) {
            _logger.LogWarning(replyException, "The error could not be reported to chat {ChatId}", context.ChatId);
        }
    }
}