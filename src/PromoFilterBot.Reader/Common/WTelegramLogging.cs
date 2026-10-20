/*
 * File: WTelegramLogging.cs
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

using Microsoft.Extensions.Logging;

namespace PromoFilterBot.Reader.Common;

/// <summary>
/// Sends the log of WTelegramClient (on the console by default) to the app logger. Its information level reports every
/// connection step, so it is logged as debug.
/// </summary>
public static class WTelegramLogging {
    public static void RedirectTo(ILogger logger) {
        WTelegram.Helpers.Log = (level, message) => logger.Log(ToLogLevel(level), "[WTelegram] {Message}", message);
    }

    private static LogLevel ToLogLevel(int level) => level switch {
        0 => LogLevel.Trace,
        1 or 2 => LogLevel.Debug,
        3 => LogLevel.Warning,
        4 => LogLevel.Error,
        _ => LogLevel.Critical
    };
}