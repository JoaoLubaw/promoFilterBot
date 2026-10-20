/*
 * File: AlertHandler.cs
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

using System.Globalization;

using Microsoft.Extensions.Logging;

using PromoFilterBot.Bot.Common;
using PromoFilterBot.Bot.Interfaces;
using PromoFilterBot.Domain.Common;
using PromoFilterBot.Domain.Dto;
using PromoFilterBot.Infrastructure.Common;
using PromoFilterBot.Infrastructure.Dto;
using PromoFilterBot.Infrastructure.Interfaces;

namespace PromoFilterBot.Bot.Handlers;

/// <summary>
/// /adicionar typed in one line, /alertas and /remover, and the buttons of the alert list.
/// </summary>
public class AlertHandler : BotHandlerBase {
    private readonly IAlertService _alertService;
    private readonly AlertWizardHandler _alertWizardHandler;
    private readonly ILocalClock _localClock;

    public AlertHandler(
        IAlertService alertService,
        AlertWizardHandler alertWizardHandler,
        ILocalClock localClock,
        IBotMessenger messenger,
        ILogger<AlertHandler> logger)
        : base(logger, messenger) {
        _alertService = alertService;
        _alertWizardHandler = alertWizardHandler;
        _localClock = localClock;
    }

    public async Task HandleAddAsync(BotContext context, string arguments, CancellationToken cancellationToken) {
        await ExecuteAsync(context, "AddAlert", arguments, async () => {
            AddAlertCommand command = AddAlertCommandParser.Parse(arguments);

            if (command.HasInvalidMaxPrice) {
                await ReplyErrorAsync(context, ResultCode.INVALID_MAX_PRICE, cancellationToken);
                return;
            }

            CreateAlertRequestDto requestDto = new CreateAlertRequestDto {
                AlertText = command.Text,
                AlertMaxPrice = command.MaxPrice
            };

            ServiceResult<AlertDto> result = await _alertService.CreateAlertAsync(context.ChatId, requestDto);

            if (!result.IsSuccess) {
                await ReplyErrorAsync(context, result.Code, cancellationToken);
                return;
            }

            await SendAsync(context, BotTexts.AlertSaved(result.Data!, edited: false), Keyboards.AfterAlertSaved(), cancellationToken);
        }, cancellationToken);
    }

    public async Task HandleListAsync(BotContext context, CancellationToken cancellationToken) {
        await ExecuteAsync(context, "ListAlerts", null, async () => {
            await ShowListAsync(context, cancellationToken);
        }, cancellationToken);
    }

    public async Task HandleRemoveAsync(BotContext context, string arguments, CancellationToken cancellationToken) {
        await ExecuteAsync(context, "RemoveAlert", arguments, async () => {
            if (string.IsNullOrWhiteSpace(arguments)) {
                ServiceResult<List<AlertDto>> alerts = await _alertService.GetAlertsAsync(context.ChatId);

                if (!alerts.IsSuccess) {
                    await ReplyErrorAsync(context, alerts.Code, cancellationToken);
                    return;
                }

                if (alerts.Data!.Count == 0) {
                    await ReplyAsync(context, BotTexts.NoAlerts, Keyboards.NewAlert(), cancellationToken);
                    return;
                }

                await ReplyAsync(context, BotTexts.AlertRemovalList(alerts.Data), Keyboards.AlertRemoval(alerts.Data), cancellationToken);
                return;
            }

            if (!int.TryParse(arguments.Trim().TrimStart('#'), NumberStyles.None, CultureInfo.InvariantCulture, out int position)) {
                await ReplyErrorAsync(context, ResultCode.INVALID_ALERT_POSITION, cancellationToken);
                return;
            }

            ServiceResult<AlertDto> result = await _alertService.DeleteAlertByPositionAsync(context.ChatId, position);

            if (!result.IsSuccess) {
                await ReplyErrorAsync(context, result.Code, cancellationToken);
                return;
            }

            await SendAsync(context, BotTexts.AlertDeleted(result.Data!), Keyboards.AfterAlertSaved(), cancellationToken);
        }, cancellationToken);
    }

    public async Task HandleCallbackAsync(BotContext context, CallbackData data, CancellationToken cancellationToken) {
        await ExecuteAsync(context, $"AlertButton:{data.Action}", data.Argument, async () => {
            int? alertId = data.IntArgument;

            if (alertId == null) {
                await AnswerAsync(context, null, false, cancellationToken);
                return;
            }

            switch (data.Action) {
                case Callbacks.AlertEdit:
                    await _alertWizardHandler.StartAsync(context, alertId, cancellationToken);
                    return;
                case Callbacks.AlertToggle: {
                        ServiceResult<AlertDto> result = await _alertService.ToggleAlertAsync(context.ChatId, alertId.Value);

                        if (!result.IsSuccess) {
                            await ReplyErrorAsync(context, result.Code, cancellationToken);
                            return;
                        }

                        await AnswerAsync(context, result.Data!.AlertIsActive ? "▶️ Alerta retomado" : "⏸ Alerta pausado", false, cancellationToken);
                        break;
                    }
                case Callbacks.AlertDelete: {
                        ServiceResult<AlertDto> result = await _alertService.DeleteAlertAsync(context.ChatId, alertId.Value);

                        if (!result.IsSuccess) {
                            await ReplyErrorAsync(context, result.Code, cancellationToken);
                            return;
                        }

                        await AnswerAsync(context, "🗑 Alerta excluído", false, cancellationToken);
                        break;
                    }
                default:
                    await AnswerAsync(context, null, false, cancellationToken);
                    return;
            }

            await ShowListAsync(context, cancellationToken);
        }, cancellationToken);
    }

    private async Task ShowListAsync(BotContext context, CancellationToken cancellationToken) {
        ServiceResult<List<AlertDto>> result = await _alertService.GetAlertsAsync(context.ChatId);

        if (!result.IsSuccess) {
            await ReplyErrorAsync(context, result.Code, cancellationToken);
            return;
        }

        if (result.Data!.Count == 0) {
            await ReplyAsync(context, BotTexts.NoAlerts, Keyboards.NewAlert(), cancellationToken);
            return;
        }

        string? pausedUntil = context.User.IsPaused(_localClock.UtcNow) ? _localClock.Format(context.User.UserPausedUntil!.Value) : null;

        await ReplyAsync(context, BotTexts.AlertList(result.Data, pausedUntil), Keyboards.AlertList(result.Data), cancellationToken);
    }
}