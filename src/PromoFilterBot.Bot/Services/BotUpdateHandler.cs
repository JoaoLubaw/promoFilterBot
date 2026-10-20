/*
 * File: BotUpdateHandler.cs
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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using PromoFilterBot.Bot.Common;
using PromoFilterBot.Bot.Handlers;

using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace PromoFilterBot.Bot.Services;

/// <summary>
/// Receives the updates of the long polling. Each one is handled in its own scope, as an HTTP request is in an API.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class BotUpdateHandler : IUpdateHandler {
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BotUpdateHandler> _logger;

    public BotUpdateHandler(IServiceScopeFactory scopeFactory, ILogger<BotUpdateHandler> logger) {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken) {
        using IServiceScope scope = _scopeFactory.CreateScope();
        UpdateRouter router = scope.ServiceProvider.GetRequiredService<UpdateRouter>();

        try {
            await router.RouteAsync(update, cancellationToken);
        }
        catch (BotBlockedException ex) {
            _logger.LogInformation("Chat {ChatId} blocked the bot while talking to it", ex.ChatId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            _logger.LogError(ex, "Update {UpdateId} could not be handled", update.Id);
        }
    }

    public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken) {
        _logger.LogWarning(exception, "Long polling error ({Source})", source);

        // Keeps a network failure from turning into a loop of requests
        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
    }
}