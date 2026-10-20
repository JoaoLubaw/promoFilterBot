/*
 * File: BotOptions.cs
 * Project: Options
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

namespace PromoFilterBot.Bot.Options;

public class BotOptions {
    public const string SectionName = "Bot";

    /// <summary>
    /// Token of the bot created in @BotFather.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Ignores the messages sent to the bot while it was offline.
    /// </summary>
    public bool DropPendingUpdates { get; set; }

    /// <summary>
    /// Telegram allows about 30 messages per second in total...
    /// </summary>
    public int GlobalMessagesPerSecond { get; set; } = 30;

    /// <summary>
    /// ...and about one per second to the same chat.
    /// </summary>
    public int PerChatIntervalMilliseconds { get; set; } = 1000;

    /// <summary>
    /// Notifications sent at the same time (to different users).
    /// </summary>
    public int NotifierConcurrency { get; set; } = 4;

    public int MaxSendAttempts { get; set; } = 3;
}