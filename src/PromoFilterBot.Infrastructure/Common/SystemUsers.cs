/*
 * File: SystemUsers.cs
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
/// Who changed a row (row_creation_user / row_update_user).
/// </summary>
public static class SystemUsers {
    public const string SYSTEM = "system";
    public const string READER = "reader";
    public const string MATCHER = "matcher";
    public const string NOTIFIER = "notifier";

    public static string FromChat(long chatId) => $"chat:{chatId}";
}