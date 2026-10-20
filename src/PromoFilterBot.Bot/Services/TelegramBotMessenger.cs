/*
 * File: TelegramBotMessenger.cs
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

using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using PromoFilterBot.Bot.Common;
using PromoFilterBot.Bot.Interfaces;
using PromoFilterBot.Bot.Options;

using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace PromoFilterBot.Bot.Services;

[ExcludeFromCodeCoverage]
public sealed class TelegramBotMessenger : IBotMessenger {
    private const int TooManyRequests = 429;
    private const int Forbidden = 403;
    private const int BadRequest = 400;

    private readonly ITelegramBotClient _botClient;
    private readonly ITelegramRateLimiter _rateLimiter;
    private readonly BotOptions _options;
    private readonly ILogger<TelegramBotMessenger> _logger;

    public TelegramBotMessenger(
        ITelegramBotClient botClient,
        ITelegramRateLimiter rateLimiter,
        IOptions<BotOptions> options,
        ILogger<TelegramBotMessenger> logger) {
        _botClient = botClient;
        _rateLimiter = rateLimiter;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<int?> SendTextAsync(long chatId, string html, InlineKeyboardMarkup? keyboard = null, string? previewUrl = null, CancellationToken cancellationToken = default) {
        LinkPreviewOptions preview = previewUrl == null
            ? new LinkPreviewOptions { IsDisabled = true }
            : new LinkPreviewOptions { Url = previewUrl, PreferSmallMedia = true };

        Message message = await ExecuteAsync(chatId, () => _botClient.SendMessage(
            chatId, html, ParseMode.Html, replyMarkup: keyboard, linkPreviewOptions: preview, cancellationToken: cancellationToken), cancellationToken);

        return message.Id;
    }

    public async Task EditTextAsync(long chatId, int messageId, string html, InlineKeyboardMarkup? keyboard = null, CancellationToken cancellationToken = default) {
        try {
            await ExecuteAsync(chatId, () => _botClient.EditMessageText(
                chatId, messageId, html, ParseMode.Html, replyMarkup: keyboard,
                linkPreviewOptions: new LinkPreviewOptions { IsDisabled = true }, cancellationToken: cancellationToken), cancellationToken);
        }
        catch (ApiRequestException ex) when (ex.ErrorCode == BadRequest && ex.Message.Contains("message is not modified", StringComparison.OrdinalIgnoreCase)) {
            // The same button pressed twice: the message already shows it
        }
        catch (ApiRequestException ex) when (ex.ErrorCode == BadRequest) {
            // Too old, or deleted, to be edited
            _logger.LogDebug(ex, "Message {MessageId} of chat {ChatId} could not be edited, sending a new one", messageId, chatId);
            await SendTextAsync(chatId, html, keyboard, null, cancellationToken);
        }
    }

    public async Task AnswerCallbackAsync(string callbackQueryId, string? text = null, bool showAlert = false, CancellationToken cancellationToken = default) {
        try {
            await _botClient.AnswerCallbackQuery(callbackQueryId, text, showAlert, cancellationToken: cancellationToken);
        }
        catch (ApiRequestException ex) {
            // A button pressed long ago can no longer be answered, and nothing is lost
            _logger.LogDebug(ex, "Callback query {CallbackQueryId} could not be answered", callbackQueryId);
        }
    }

    private async Task<T> ExecuteAsync<T>(long chatId, Func<Task<T>> request, CancellationToken cancellationToken) {
        int attempts = Math.Max(1, _options.MaxSendAttempts);

        for (int attempt = 1; ; attempt++) {
            await _rateLimiter.WaitAsync(chatId, cancellationToken);

            try {
                return await request();
            }
            catch (ApiRequestException ex) when (ex.ErrorCode == Forbidden) {
                throw new BotBlockedException(chatId, ex);
            }
            catch (ApiRequestException ex) when (ex.ErrorCode == TooManyRequests && attempt < attempts) {
                int retryAfter = ex.Parameters?.RetryAfter ?? 1;
                _logger.LogWarning("Telegram asked the bot to wait {Seconds}s (chat {ChatId})", retryAfter, chatId);

                await Task.Delay(TimeSpan.FromSeconds(retryAfter), cancellationToken);
            }
            catch (RequestException ex) when (ex is not ApiRequestException && attempt < attempts) {
                _logger.LogWarning(ex, "Request to Telegram failed (attempt {Attempt} of {Attempts})", attempt, attempts);

                await Task.Delay(TimeSpan.FromSeconds(2 * attempt), cancellationToken);
            }
        }
    }
}