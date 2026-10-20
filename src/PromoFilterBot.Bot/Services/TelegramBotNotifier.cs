/*
 * File: TelegramBotNotifier.cs
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

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using PromoFilterBot.Bot.Interfaces;
using PromoFilterBot.Infrastructure.Interfaces;
using PromoFilterBot.Infrastructure.Options;

namespace PromoFilterBot.Bot.Services;

public sealed class TelegramBotNotifier : IBotNotifier {
    private readonly IBotMessenger _messenger;
    private readonly GeneralOptions _generalOptions;
    private readonly ILogger<TelegramBotNotifier> _logger;

    public TelegramBotNotifier(IBotMessenger messenger, IOptions<GeneralOptions> generalOptions, ILogger<TelegramBotNotifier> logger) {
        _messenger = messenger;
        _generalOptions = generalOptions.Value;
        _logger = logger;
    }

    public async Task NotifyAdminAsync(string html, CancellationToken cancellationToken = default) {
        if (_generalOptions.AdminChatId == 0) {
            _logger.LogWarning("No admin chat configured (General:AdminChatId) to receive: {Message}", html);
            return;
        }

        await NotifyUserAsync(_generalOptions.AdminChatId, html, cancellationToken);
    }

    public async Task NotifyUserAsync(long chatId, string html, CancellationToken cancellationToken = default) {
        try {
            await _messenger.SendTextAsync(chatId, html, cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            _logger.LogWarning(ex, "Message to chat {ChatId} could not be sent", chatId);
        }
    }
}