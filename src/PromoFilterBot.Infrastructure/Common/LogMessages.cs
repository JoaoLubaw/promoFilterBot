/*
 * File: LogMessages.cs
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

namespace PromoFilterBot.Infrastructure.Common;

/// <summary>
/// Log messages shared by the services, for the expected failures.
/// </summary>
public static class LogMessages {
    public const string UNREGISTERED_USER = "Call attempt from chat {ChatId}, which is not registered";
    public const string NOT_ADMIN = "Admin call attempt from chat {ChatId}, which is not the admin";
    public const string EMPTY_REQUEST = "Call attempt without a request object from chat {ChatId}";
    public const string INVALID_ALERT_TEXT = "Alert without any term to look for from chat {ChatId}";
    public const string ALERT_NOT_FOUND = "Alert {AlertId} not found for chat {ChatId}";
    public const string CHANNEL_NOT_FOUND = "Channel {ChannelId} not found";
    public const string INVALID_CHANNEL_REFERENCE = "Channel reference '{Reference}' from chat {ChatId} could not be read";
}