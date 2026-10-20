/*
 * File: PauseHandler.cs
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
/// /pausar: no notification until a time, for every alert of the user.
/// </summary>
public class PauseHandler : BotHandlerBase {
    private readonly IUserService _userService;
    private readonly ILocalClock _localClock;

    public PauseHandler(IUserService userService, ILocalClock localClock, IBotMessenger messenger, ILogger<PauseHandler> logger)
        : base(logger, messenger) {
        _userService = userService;
        _localClock = localClock;
    }

    public async Task HandlePauseAsync(BotContext context, string arguments, CancellationToken cancellationToken) {
        await ExecuteAsync(context, "Pause", arguments, async () => {
            if (string.IsNullOrWhiteSpace(arguments)) {
                bool isPaused = context.User.IsPaused(_localClock.UtcNow);
                string? pausedUntil = isPaused ? _localClock.Format(context.User.UserPausedUntil!.Value) : null;

                await ReplyAsync(context, BotTexts.PauseMenu(pausedUntil), Keyboards.PauseMenu(isPaused), cancellationToken);
                return;
            }

            await ApplyAsync(context, arguments, cancellationToken);
        }, cancellationToken);
    }

    public async Task HandleCallbackAsync(BotContext context, CallbackData data, CancellationToken cancellationToken) {
        await ExecuteAsync(context, $"PauseButton:{data.Action}", null, async () => {
            await ApplyAsync(context, data.Action, cancellationToken);
        }, cancellationToken);
    }

    private async Task ApplyAsync(BotContext context, string duration, CancellationToken cancellationToken) {
        ServiceResult<UserDto> result = await _userService.PauseUserAsync(context.ChatId, duration);

        if (!result.IsSuccess) {
            await ReplyErrorAsync(context, result.Code, cancellationToken);
            return;
        }

        string html = result.Data!.UserPausedUntil.HasValue
            ? BotTexts.Paused(_localClock.Format(result.Data.UserPausedUntil.Value))
            : BotTexts.Resumed;

        await ReplyAsync(context, html, Keyboards.MainMenu(), cancellationToken);
    }
}