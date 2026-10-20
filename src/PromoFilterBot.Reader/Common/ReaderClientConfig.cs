/*
 * File: ReaderClientConfig.cs
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

using System.Globalization;

using PromoFilterBot.Reader.Options;

namespace PromoFilterBot.Reader.Common;

/// <summary>
/// Answers the questions WTelegramClient asks while it connects. The ones only a person can answer (the code sent by
/// Telegram, the 2FA password) go to askPerson, which, outside the interactive login, throws.
/// </summary>
public static class ReaderClientConfig {
    private static readonly HashSet<string> PersonItems = new(StringComparer.Ordinal) {
        "verification_code", "password", "email", "email_verification_code", "first_name", "last_name"
    };

    public static Func<string, string?> Create(ReaderOptions options, Func<string, string?> askPerson) {
        return what => {
            if (PersonItems.Contains(what)) return askPerson(what);

            return what switch {
                "api_id" => options.ApiId.ToString(CultureInfo.InvariantCulture),
                "api_hash" => options.ApiHash,
                "phone_number" => options.PhoneNumber,
                "session_pathname" => options.SessionPath,
                _ => null
            };
        };
    }

    public static Func<string, string?> CreateHeadless(ReaderOptions options) {
        return Create(options, what => throw new ReaderLoginRequiredException(what));
    }

    public static void EnsureDirectories(ReaderOptions options) {
        foreach (string path in new[] { options.SessionPath, options.UpdatesStatePath }) {
            string? directory = Path.GetDirectoryName(Path.GetFullPath(path));

            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        }
    }
}