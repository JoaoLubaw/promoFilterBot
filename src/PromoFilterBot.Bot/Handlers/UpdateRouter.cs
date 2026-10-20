/*
 * File: UpdateRouter.cs
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
using PromoFilterBot.Domain.Models;
using PromoFilterBot.Infrastructure.Common;
using PromoFilterBot.Infrastructure.Dto;
using PromoFilterBot.Infrastructure.Interfaces;

using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

using TelegramUser = Telegram.Bot.Types.User;

namespace PromoFilterBot.Bot.Handlers;

/// <summary>
/// Sends each update to its handler: a command, a button, or the answer to a step of a guided flow.
/// </summary>
public class UpdateRouter {
    private readonly IUserService _userService;
    private readonly IConversationService _conversationService;
    private readonly IBotMessenger _messenger;
    private readonly StartHandler _startHandler;
    private readonly AlertHandler _alertHandler;
    private readonly AlertWizardHandler _alertWizardHandler;
    private readonly PauseHandler _pauseHandler;
    private readonly AccountHandler _accountHandler;
    private readonly AdminHandler _adminHandler;
    private readonly NotificationHandler _notificationHandler;
    private readonly ILogger<UpdateRouter> _logger;

    public UpdateRouter(
        IUserService userService,
        IConversationService conversationService,
        IBotMessenger messenger,
        StartHandler startHandler,
        AlertHandler alertHandler,
        AlertWizardHandler alertWizardHandler,
        PauseHandler pauseHandler,
        AccountHandler accountHandler,
        AdminHandler adminHandler,
        NotificationHandler notificationHandler,
        ILogger<UpdateRouter> logger) {
        _userService = userService;
        _conversationService = conversationService;
        _messenger = messenger;
        _startHandler = startHandler;
        _alertHandler = alertHandler;
        _alertWizardHandler = alertWizardHandler;
        _pauseHandler = pauseHandler;
        _accountHandler = accountHandler;
        _adminHandler = adminHandler;
        _notificationHandler = notificationHandler;
        _logger = logger;
    }

    public async Task RouteAsync(Update update, CancellationToken cancellationToken) {
        if (update.Message is { } message) {
            await RouteMessageAsync(message, cancellationToken);
        }
        else if (update.CallbackQuery is { } callbackQuery) {
            await RouteCallbackAsync(callbackQuery, cancellationToken);
        }
    }

    private async Task RouteMessageAsync(Message message, CancellationToken cancellationToken) {
        // The bot only talks in private: in a group, an offer would reach people who did not ask for it
        if (message.Chat.Type != ChatType.Private || message.From == null) return;

        BotContext? context = await CreateContextAsync(message.Chat.Id, message.From, null, null);

        if (context == null) return;

        string? text = message.Text?.Trim();

        if (string.IsNullOrEmpty(text)) {
            await _startHandler.HandleNonTextAsync(context, cancellationToken);
            return;
        }

        IncomingCommand? command = IncomingCommand.Parse(text);

        if (command != null) {
            await RouteCommandAsync(context, command, cancellationToken);
            return;
        }

        ConversationStateDto? state = await _conversationService.GetStateAsync(context.ChatId);

        if (state == null) {
            await _startHandler.HandleUnknownTextAsync(context, text, cancellationToken);
        }
        else if (state.Step == ConversationStep.AwaitingChannelsToAdd) {
            await _adminHandler.HandleChannelsTextAsync(context, text, cancellationToken);
        }
        else {
            await _alertWizardHandler.HandleTextAsync(context, state, text, cancellationToken);
        }
    }

    private async Task RouteCommandAsync(BotContext context, IncomingCommand command, CancellationToken cancellationToken) {
        _logger.LogDebug("Command /{Command} from chat {ChatId}", command.Name, context.ChatId);

        // A command leaves the guided flow the user was in
        await _conversationService.ClearStateAsync(context.ChatId);

        switch (command.Name) {
            case "start":
                await _startHandler.HandleStartAsync(context, cancellationToken);
                break;
            case "ajuda" or "help":
                await _startHandler.HandleHelpAsync(context, cancellationToken);
                break;
            case "adicionar" or "add" or "novo":
                if (command.Arguments.Length == 0) {
                    await _alertWizardHandler.StartAsync(context, null, cancellationToken);
                }
                else {
                    await _alertHandler.HandleAddAsync(context, command.Arguments, cancellationToken);
                }

                break;
            case "alertas":
                await _alertHandler.HandleListAsync(context, cancellationToken);
                break;
            case "remover":
                await _alertHandler.HandleRemoveAsync(context, command.Arguments, cancellationToken);
                break;
            case "pausar":
                await _pauseHandler.HandlePauseAsync(context, command.Arguments, cancellationToken);
                break;
            case "apagarconta":
                await _accountHandler.HandleDeleteAccountAsync(context, cancellationToken);
                break;
            case "admin":
                await _adminHandler.HandleAdminAsync(context, command.Arguments, cancellationToken);
                break;
            case "cancelar":
                await _startHandler.HandleCancelAsync(context, cancellationToken);
                break;
            default:
                await _startHandler.HandleUnknownCommandAsync(context, cancellationToken);
                break;
        }
    }

    private async Task RouteCallbackAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken) {
        Message? message = callbackQuery.Message;

        if (message != null && message.Chat.Type != ChatType.Private) {
            await _messenger.AnswerCallbackAsync(callbackQuery.Id, cancellationToken: cancellationToken);
            return;
        }

        long chatId = message?.Chat.Id ?? callbackQuery.From.Id;
        BotContext? context = await CreateContextAsync(chatId, callbackQuery.From, callbackQuery.Id, message?.Id);

        if (context == null) {
            await _messenger.AnswerCallbackAsync(callbackQuery.Id, cancellationToken: cancellationToken);
            return;
        }

        try {
            CallbackData? data = CallbackData.Parse(callbackQuery.Data);

            if (data != null) await RouteButtonAsync(context, data, cancellationToken);
        }
        finally {
            // Telegram shows the button loading until the callback is answered
            if (!context.CallbackAnswered) await _messenger.AnswerCallbackAsync(callbackQuery.Id, cancellationToken: cancellationToken);
        }
    }

    private async Task RouteButtonAsync(BotContext context, CallbackData data, CancellationToken cancellationToken) {
        switch (data.Prefix) {
            case Callbacks.Menu:
                await RouteMenuAsync(context, data, cancellationToken);
                break;
            case Callbacks.Alert:
                await _alertHandler.HandleCallbackAsync(context, data, cancellationToken);
                break;
            case Callbacks.Wizard:
                await _alertWizardHandler.HandleCallbackAsync(context, data, cancellationToken);
                break;
            case Callbacks.Pause:
                await _pauseHandler.HandleCallbackAsync(context, data, cancellationToken);
                break;
            case Callbacks.Account:
                await _accountHandler.HandleCallbackAsync(context, data, cancellationToken);
                break;
            case Callbacks.Admin:
                await _adminHandler.HandleCallbackAsync(context, data, cancellationToken);
                break;
            case Callbacks.Notification:
                await _notificationHandler.HandleCallbackAsync(context, data, cancellationToken);
                break;
        }
    }

    private async Task RouteMenuAsync(BotContext context, CallbackData data, CancellationToken cancellationToken) {
        await _conversationService.ClearStateAsync(context.ChatId);

        switch (data.Action) {
            case Callbacks.MenuNewAlert:
                await _alertWizardHandler.StartAsync(context, null, cancellationToken);
                break;
            case Callbacks.MenuAlerts:
                await _alertHandler.HandleListAsync(context, cancellationToken);
                break;
            case Callbacks.MenuPause:
                await _pauseHandler.HandlePauseAsync(context, string.Empty, cancellationToken);
                break;
            case Callbacks.MenuHelp:
                await _startHandler.HandleHelpAsync(context, cancellationToken);
                break;
            default:
                await _startHandler.HandleStartAsync(context, cancellationToken);
                break;
        }
    }

    // Registers the user on their first update, so every handler can count on them
    private async Task<BotContext?> CreateContextAsync(long chatId, TelegramUser from, string? callbackQueryId, int? messageId) {
        string name = string.Join(' ', new[] { from.FirstName, from.LastName }.Where(n => !string.IsNullOrWhiteSpace(n)));
        ServiceResult<UserDto> result = await _userService.RegisterUserAsync(chatId, name);

        if (!result.IsSuccess || result.Data == null) {
            _logger.LogWarning("Chat {ChatId} could not be registered ({Code})", chatId, result.Code);
            return null;
        }

        return new BotContext {
            ChatId = chatId,
            User = result.Data,
            IsAdmin = _userService.IsAdmin(chatId),
            CallbackQueryId = callbackQueryId,
            MessageId = messageId
        };
    }
}