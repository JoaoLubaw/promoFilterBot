/*
 * File: AccountHandler.cs
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
using PromoFilterBot.Infrastructure.Interfaces;

namespace PromoFilterBot.Bot.Handlers;

/// <summary>
/// /apagarconta (LGPD): deletes everything about the user, after a confirmation.
/// </summary>
public class AccountHandler : BotHandlerBase {
    private readonly IUserService _userService;

    public AccountHandler(IUserService userService, IBotMessenger messenger, ILogger<AccountHandler> logger) : base(logger, messenger) {
        _userService = userService;
    }

    public async Task HandleDeleteAccountAsync(BotContext context, CancellationToken cancellationToken) {
        await ExecuteAsync(context, "DeleteAccount", null, async () => {
            await ReplyAsync(context, BotTexts.ConfirmDeleteAccount, Keyboards.DeleteAccountConfirm(), cancellationToken);
        }, cancellationToken);
    }

    public async Task HandleCallbackAsync(BotContext context, CallbackData data, CancellationToken cancellationToken) {
        await ExecuteAsync(context, $"AccountButton:{data.Action}", null, async () => {
            if (data.Action != Callbacks.AccountDeleteConfirm) {
                await ReplyAsync(context, BotTexts.AccountDeleteCancelled, Keyboards.MainMenu(), cancellationToken);
                return;
            }

            ServiceResult<EmptyDto> result = await _userService.DeleteAccountAsync(context.ChatId);

            if (!result.IsSuccess) {
                await ReplyErrorAsync(context, result.Code, cancellationToken);
                return;
            }

            await ReplyAsync(context, BotTexts.AccountDeleted, null, cancellationToken);
        }, cancellationToken);
    }
}