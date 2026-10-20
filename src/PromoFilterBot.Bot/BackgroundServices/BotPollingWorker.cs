/*
 * File: BotPollingWorker.cs
 * Project: BackgroundServices
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

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using PromoFilterBot.Bot.Common;
using PromoFilterBot.Bot.Options;
using PromoFilterBot.Bot.Services;

using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PromoFilterBot.Bot.BackgroundServices;

/// <summary>
/// Long polling: the bot asks Telegram for the updates, so the VM needs no open port, domain or HTTPS certificate.
/// </summary>
[ExcludeFromCodeCoverage]
public class BotPollingWorker : BackgroundService {
    private readonly ITelegramBotClient _botClient;
    private readonly BotUpdateHandler _updateHandler;
    private readonly BotOptions _options;
    private readonly ILogger<BotPollingWorker> _logger;

    public BotPollingWorker(ITelegramBotClient botClient, BotUpdateHandler updateHandler, IOptions<BotOptions> options, ILogger<BotPollingWorker> logger) {
        _botClient = botClient;
        _updateHandler = updateHandler;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        ReceiverOptions receiverOptions = new ReceiverOptions {
            AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery],
            DropPendingUpdates = _options.DropPendingUpdates
        };

        while (!stoppingToken.IsCancellationRequested) {
            try {
                User bot = await _botClient.GetMe(stoppingToken);
                await _botClient.SetMyCommands(BotTexts.Commands, cancellationToken: stoppingToken);

                _logger.LogInformation("Bot @{Username} receiving updates", bot.Username);

                await _botClient.ReceiveAsync(_updateHandler, receiverOptions, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                break;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "The bot stopped receiving updates, starting again");

                try {
                    await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
                }
                catch (OperationCanceledException) {
                    break;
                }
            }
        }
    }
}