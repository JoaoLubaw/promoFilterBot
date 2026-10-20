/*
 * File: ReaderLoginRequiredException.cs
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

namespace PromoFilterBot.Reader.Common;

/// <summary>
/// Telegram asked for a code (or the 2FA password) outside the interactive login: the session is missing or was revoked.
/// </summary>
public class ReaderLoginRequiredException : Exception {
    public ReaderLoginRequiredException(string requestedItem)
        : base($"The reader account needs an interactive login (Telegram asked for '{requestedItem}'). Run the app with --login.") {
        RequestedItem = requestedItem;
    }

    public string RequestedItem { get; }
}