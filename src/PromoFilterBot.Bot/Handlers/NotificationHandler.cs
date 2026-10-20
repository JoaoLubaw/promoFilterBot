/*
 * File: NotificationHandler.cs
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
using PromoFilterBot.Infrastructure.Dto;
using PromoFilterBot.Infrastructure.Interfaces;

namespace PromoFilterBot.Bot.Handlers;

/// <summary>
/// The buttons of an offer: "Desligar alerta" and "Não era isso". The offer itself is never edited.
/// </summary>
public class NotificationHandler : BotHandlerBase {
    private readonly IAlertService _alertService;
    private readonly INotificationService _notificationService;

    public NotificationHandler(
        IAlertService alertService,
        INotificationService notificationService,
        IBotMessenger messenger,
        ILogger<NotificationHandler> logger)
        : base(logger, messenger) {
        _alertService = alertService;
        _notificationService = notificationService;
    }

    public async Task HandleCallbackAsync(BotContext context, CallbackData data, CancellationToken cancellationToken) {
        await ExecuteAsync(context, $"NotificationButton:{data.Action}", data.Argument, async () => {
            switch (data.Action) {
                case Callbacks.NotificationTurnOffAlert when data.IntArgument.HasValue: {
                        ServiceResult<AlertDto> result = await _alertService.PauseAlertAsync(context.ChatId, data.IntArgument.Value);

                        if (!result.IsSuccess) {
                            await ReplyErrorAsync(context, result.Code, cancellationToken);
                            return;
                        }

                        await AnswerAsync(context, BotTexts.AlertTurnedOffFromNotification, showAlert: true, cancellationToken);
                        break;
                    }
                case Callbacks.NotificationNotRelevant when data.LongArgument.HasValue: {
                        ServiceResult<NotificationFeedbackDto> result = await _notificationService.RegisterFeedbackAsync(context.ChatId, data.LongArgument.Value);

                        if (!result.IsSuccess) {
                            await ReplyErrorAsync(context, result.Code, cancellationToken);
                            return;
                        }

                        await AnswerAsync(context, BotTexts.FeedbackThanks, false, cancellationToken);
                        await SendAsync(context, BotTexts.FeedbackTip(result.Data!.AlertText), Keyboards.EditAlert(result.Data.AlertId), cancellationToken);
                        break;
                    }
                default:
                    await AnswerAsync(context, null, false, cancellationToken);
                    break;
            }
        }, cancellationToken);
    }
}