/*
 * File: AdminHandler.cs
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
using PromoFilterBot.Domain.Models;
using PromoFilterBot.Infrastructure.Common;
using PromoFilterBot.Infrastructure.Dto;
using PromoFilterBot.Infrastructure.Interfaces;

namespace PromoFilterBot.Bot.Handlers;

/// <summary>
/// /admin, only for the chat of General:AdminChatId: numbers, state of the reader account and the channels to read,
/// which only the admin registers.
/// </summary>
public class AdminHandler : BotHandlerBase {
    private readonly IAdminService _adminService;
    private readonly IChannelService _channelService;
    private readonly IConversationService _conversationService;
    private readonly ILocalClock _localClock;

    public AdminHandler(
        IAdminService adminService,
        IChannelService channelService,
        IConversationService conversationService,
        ILocalClock localClock,
        IBotMessenger messenger,
        ILogger<AdminHandler> logger)
        : base(logger, messenger) {
        _adminService = adminService;
        _channelService = channelService;
        _conversationService = conversationService;
        _localClock = localClock;
    }

    public async Task HandleAdminAsync(BotContext context, string arguments, CancellationToken cancellationToken) {
        await ExecuteAsync(context, "Admin", arguments, async () => {
            string[] parts = arguments.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            string subcommand = parts.Length > 0 ? TextNormalizer.Fold(parts[0]) : string.Empty;
            string references = parts.Length > 1 ? parts[1] : string.Empty;

            switch (subcommand) {
                case "":
                    await ShowStatsAsync(context, cancellationToken);
                    break;
                case "adicionar" or "add":
                    if (references.Length == 0) {
                        await AskChannelsAsync(context, cancellationToken);
                    }
                    else {
                        await AddChannelsAsync(context, references, cancellationToken);
                    }

                    break;
                case "remover" or "bloquear" or "remove":
                    await RemoveChannelAsync(context, references, cancellationToken);
                    break;
                case "canais":
                    await ShowChannelsAsync(context, cancellationToken);
                    break;
                default:
                    await SendAsync(context, BotTexts.Help(context.IsAdmin), null, cancellationToken);
                    break;
            }
        }, cancellationToken);
    }

    public async Task HandleCallbackAsync(BotContext context, CallbackData data, CancellationToken cancellationToken) {
        await ExecuteAsync(context, $"AdminButton:{data.Action}", data.Argument, async () => {
            switch (data.Action) {
                case Callbacks.AdminChannels:
                    await ShowChannelsAsync(context, cancellationToken);
                    break;
                case Callbacks.AdminAddChannels:
                    await AskChannelsAsync(context, cancellationToken);
                    break;
                default:
                    await ShowStatsAsync(context, cancellationToken);
                    break;
            }
        }, cancellationToken);
    }

    // The answer to "Adicionar canais"
    public async Task HandleChannelsTextAsync(BotContext context, string text, CancellationToken cancellationToken) {
        await ExecuteAsync(context, "AddChannelsText", text, async () => {
            await _conversationService.ClearStateAsync(context.ChatId);
            await AddChannelsAsync(context, text, cancellationToken);
        }, cancellationToken);
    }

    private async Task ShowStatsAsync(BotContext context, CancellationToken cancellationToken) {
        ServiceResult<AdminStatsDto> result = await _adminService.GetStatsAsync(context.ChatId);

        if (!result.IsSuccess) {
            await ReplyErrorAsync(context, result.Code, cancellationToken);
            return;
        }

        await ReplyAsync(context, BotTexts.AdminStats(result.Data!, _localClock.Format), Keyboards.AdminMenu(), cancellationToken);
    }

    private async Task ShowChannelsAsync(BotContext context, CancellationToken cancellationToken) {
        ServiceResult<List<ChannelDto>> result = await _channelService.GetChannelsAsync(context.ChatId, null);

        if (!result.IsSuccess) {
            await ReplyErrorAsync(context, result.Code, cancellationToken);
            return;
        }

        await ReplyAsync(context, BotTexts.AdminChannelList(result.Data!), Keyboards.AdminChannels(), cancellationToken);
    }

    private async Task AskChannelsAsync(BotContext context, CancellationToken cancellationToken) {
        if (!context.IsAdmin) {
            await ReplyErrorAsync(context, ResultCode.NOT_ADMIN, cancellationToken);
            return;
        }

        await _conversationService.SaveStateAsync(context.ChatId, ConversationStep.AwaitingChannelsToAdd);
        await ReplyAsync(context, BotTexts.AskChannelsToAdd, Keyboards.CancelOnly(), cancellationToken);
    }

    private async Task AddChannelsAsync(BotContext context, string references, CancellationToken cancellationToken) {
        ServiceResult<List<ChannelAdditionDto>> result = await _channelService.AddChannelsAsync(context.ChatId, references);

        if (!result.IsSuccess) {
            await ReplyErrorAsync(context, result.Code, cancellationToken);
            return;
        }

        await SendAsync(context, BotTexts.ChannelAdditions(result.Data!), Keyboards.AdminChannels(), cancellationToken);
    }

    private async Task RemoveChannelAsync(BotContext context, string reference, CancellationToken cancellationToken) {
        ServiceResult<ChannelDto> result = await _channelService.RemoveChannelAsync(context.ChatId, reference);

        if (!result.IsSuccess) {
            await ReplyErrorAsync(context, result.Code, cancellationToken);
            return;
        }

        await SendAsync(context, BotTexts.ChannelRemoved(result.Data!), null, cancellationToken);
    }
}