/*
 * File: IncomingCommand.cs
 * Project: Common
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

using PromoFilterBot.Domain.Common;

namespace PromoFilterBot.Bot.Common;

/// <summary>
/// A command typed to the bot: <c>/adicionar rtx 4060 max 2000</c> is "adicionar" with "rtx 4060 max 2000".
/// </summary>
public sealed record IncomingCommand(string Name, string Arguments) {
    public static IncomingCommand? Parse(string? text) {
        if (string.IsNullOrWhiteSpace(text)) return null;

        string trimmed = text.Trim();

        if (!trimmed.StartsWith('/')) return null;

        int separator = trimmed.IndexOfAny([' ', '\n', '\t']);
        string head = separator < 0 ? trimmed : trimmed[..separator];
        string arguments = separator < 0 ? string.Empty : trimmed[(separator + 1)..].Trim();

        // In a group, Telegram appends the bot: /alertas@PromoFilterBot
        string name = head[1..];
        int mention = name.IndexOf('@');

        if (mention >= 0) name = name[..mention];

        name = TextNormalizer.Fold(name);

        return name.Length == 0 ? null : new IncomingCommand(name, arguments);
    }
}