/*
 * File: AlertWizardHandler.cs
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
using PromoFilterBot.Domain.Common;
using PromoFilterBot.Domain.Dto;
using PromoFilterBot.Domain.Models;
using PromoFilterBot.Infrastructure.Common;
using PromoFilterBot.Infrastructure.Dto;
using PromoFilterBot.Infrastructure.Interfaces;

namespace PromoFilterBot.Bot.Handlers;

/// <summary>
/// The guided /adicionar (and the editing of an alert): what to look for → highest price → summary.
/// </summary>
public class AlertWizardHandler : BotHandlerBase {
    private readonly IConversationService _conversationService;
    private readonly IAlertService _alertService;

    public AlertWizardHandler(
        IConversationService conversationService,
        IAlertService alertService,
        IBotMessenger messenger,
        ILogger<AlertWizardHandler> logger)
        : base(logger, messenger) {
        _conversationService = conversationService;
        _alertService = alertService;
    }

    /// <summary>
    /// Starts a new alert, or the editing of <paramref name="editingAlertId"/>, with its answers filled in.
    /// </summary>
    public async Task StartAsync(BotContext context, int? editingAlertId, CancellationToken cancellationToken) {
        await ExecuteAsync(context, "StartWizard", editingAlertId, async () => {
            AlertDraftDto draft = new AlertDraftDto();

            if (editingAlertId.HasValue) {
                ServiceResult<AlertDto> alert = await _alertService.GetAlertAsync(context.ChatId, editingAlertId.Value);

                if (!alert.IsSuccess) {
                    await ReplyErrorAsync(context, alert.Code, cancellationToken);
                    return;
                }

                draft = new AlertDraftDto {
                    EditingAlertId = alert.Data!.AlertId,
                    AlertText = alert.Data.AlertOriginalText,
                    PriceChosen = true,
                    AlertMaxPrice = alert.Data.AlertMaxPrice
                };
            }

            await AskTextAsync(context, draft, cancellationToken);
        }, cancellationToken);
    }

    public async Task HandleTextAsync(BotContext context, ConversationStateDto state, string text, CancellationToken cancellationToken) {
        await ExecuteAsync(context, $"WizardText:{state.Step}", text, async () => {
            AlertDraftDto draft = state.Draft;

            switch (state.Step) {
                case ConversationStep.AwaitingAlertText: {
                        // "rtx 4060 max 2000" answers the price too
                        AddAlertCommand command = AddAlertCommandParser.Parse(text);

                        if (command.HasInvalidMaxPrice) {
                            await ReplyErrorAsync(context, ResultCode.INVALID_MAX_PRICE, cancellationToken);
                            return;
                        }

                        if (!AlertQueryParser.Parse(command.Text).IsValid) {
                            await ReplyErrorAsync(context, ResultCode.INVALID_ALERT_TEXT, cancellationToken);
                            return;
                        }

                        draft.AlertText = command.Text;

                        if (command.MaxPrice.HasValue) {
                            draft.AlertMaxPrice = command.MaxPrice;
                            draft.PriceChosen = true;
                            await ConfirmAsync(context, draft, cancellationToken);
                            return;
                        }

                        await AskPriceAsync(context, draft, cancellationToken);
                        return;
                    }
                case ConversationStep.AwaitingCustomPrice:
                    if (!MoneyParser.TryParse(text, out decimal maxPrice) || maxPrice <= 0) {
                        await ReplyErrorAsync(context, ResultCode.INVALID_MAX_PRICE, cancellationToken);
                        return;
                    }

                    draft.AlertMaxPrice = maxPrice;
                    draft.PriceChosen = true;
                    await ConfirmAsync(context, draft, cancellationToken);
                    return;
                default:
                    await SendAsync(context, BotTexts.UseButtons, null, cancellationToken);
                    return;
            }
        }, cancellationToken);
    }

    public async Task HandleCallbackAsync(BotContext context, CallbackData data, CancellationToken cancellationToken) {
        await ExecuteAsync(context, $"WizardButton:{data.Action}", data.Argument, async () => {
            if (data.Action == Callbacks.WizardCancel) {
                await _conversationService.ClearStateAsync(context.ChatId);
                await ReplyAsync(context, BotTexts.WizardCancelled, Keyboards.MainMenu(), cancellationToken);
                return;
            }

            ConversationStateDto? state = await _conversationService.GetStateAsync(context.ChatId);

            if (state == null || state.Step == ConversationStep.AwaitingChannelsToAdd) {
                await AnswerAsync(context, BotTexts.StepExpired, showAlert: true, cancellationToken);
                return;
            }

            AlertDraftDto draft = state.Draft;

            switch (data.Action) {
                case Callbacks.WizardKeepText when !string.IsNullOrWhiteSpace(draft.AlertText):
                    await AskPriceAsync(context, draft, cancellationToken);
                    break;
                case Callbacks.WizardPrice:
                    await HandlePriceAsync(context, draft, data.Argument, cancellationToken);
                    break;
                case Callbacks.WizardSave:
                    await SaveAsync(context, draft, cancellationToken);
                    break;
                case Callbacks.WizardFix:
                    await AskTextAsync(context, draft, cancellationToken);
                    break;
                default:
                    await AnswerAsync(context, BotTexts.StepExpired, showAlert: true, cancellationToken);
                    break;
            }
        }, cancellationToken);
    }

    private async Task HandlePriceAsync(BotContext context, AlertDraftDto draft, string? argument, CancellationToken cancellationToken) {
        switch (argument) {
            case Callbacks.PriceCustom:
                await _conversationService.SaveStateAsync(context.ChatId, ConversationStep.AwaitingCustomPrice, draft);
                await ReplyAsync(context, BotTexts.AskCustomPrice, Keyboards.CancelOnly(), cancellationToken);
                return;
            case Callbacks.PriceKeep:
                break;
            case Callbacks.PriceNone:
                draft.AlertMaxPrice = null;
                break;
            default:
                if (!MoneyParser.TryParse(argument, out decimal maxPrice)) {
                    await ReplyErrorAsync(context, ResultCode.INVALID_MAX_PRICE, cancellationToken);
                    return;
                }

                draft.AlertMaxPrice = maxPrice;
                break;
        }

        draft.PriceChosen = true;
        await ConfirmAsync(context, draft, cancellationToken);
    }

    // When fixing a new alert, or editing one, the text typed before can be kept
    private async Task AskTextAsync(BotContext context, AlertDraftDto draft, CancellationToken cancellationToken) {
        await _conversationService.SaveStateAsync(context.ChatId, ConversationStep.AwaitingAlertText, draft);

        bool hasText = !string.IsNullOrWhiteSpace(draft.AlertText);
        string html = hasText ? BotTexts.EditAlertText(draft.AlertText!) : BotTexts.AskAlertText;

        await ReplyAsync(context, html, Keyboards.WizardText(canKeepText: hasText), cancellationToken);
    }

    private async Task AskPriceAsync(BotContext context, AlertDraftDto draft, CancellationToken cancellationToken) {
        await _conversationService.SaveStateAsync(context.ChatId, ConversationStep.AwaitingMaxPrice, draft);
        await ReplyAsync(context, BotTexts.AskMaxPrice(draft), Keyboards.WizardPrice(draft), cancellationToken);
    }

    private async Task ConfirmAsync(BotContext context, AlertDraftDto draft, CancellationToken cancellationToken) {
        await _conversationService.SaveStateAsync(context.ChatId, ConversationStep.ConfirmingAlert, draft);
        await ReplyAsync(context, BotTexts.AlertSummary(draft), Keyboards.WizardConfirm(), cancellationToken);
    }

    private async Task SaveAsync(BotContext context, AlertDraftDto draft, CancellationToken cancellationToken) {
        if (string.IsNullOrWhiteSpace(draft.AlertText)) {
            await AskTextAsync(context, draft, cancellationToken);
            return;
        }

        ServiceResult<AlertDto> result;

        if (draft.EditingAlertId.HasValue) {
            EditAlertRequestDto requestDto = new EditAlertRequestDto {
                AlertText = draft.AlertText,
                AlertMaxPrice = draft.AlertMaxPrice
            };

            result = await _alertService.EditAlertAsync(context.ChatId, draft.EditingAlertId.Value, requestDto);
        }
        else {
            CreateAlertRequestDto requestDto = new CreateAlertRequestDto {
                AlertText = draft.AlertText,
                AlertMaxPrice = draft.AlertMaxPrice
            };

            result = await _alertService.CreateAlertAsync(context.ChatId, requestDto);
        }

        // The flow stays where it is, so the user can fix it
        if (!result.IsSuccess) {
            await ReplyErrorAsync(context, result.Code, cancellationToken);
            return;
        }

        await _conversationService.ClearStateAsync(context.ChatId);
        await ReplyAsync(context, BotTexts.AlertSaved(result.Data!, draft.EditingAlertId.HasValue), Keyboards.AfterAlertSaved(), cancellationToken);
    }
}