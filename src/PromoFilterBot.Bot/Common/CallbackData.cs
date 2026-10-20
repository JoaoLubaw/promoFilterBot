/*
 * File: CallbackData.cs
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

namespace PromoFilterBot.Bot.Common;

/// <summary>
/// Data of a button. Telegram allows only 64 bytes, so it holds a short code (<c>a:del:42</c>) and the rest is read from
/// the database.
/// </summary>
public sealed record CallbackData(string Prefix, string Action, string? Argument = null) {
    public const int MaxLength = 64;
    private const char Separator = ':';

    public int? IntArgument => int.TryParse(Argument, NumberStyles.None, CultureInfo.InvariantCulture, out int value) ? value : null;

    public long? LongArgument => long.TryParse(Argument, NumberStyles.None, CultureInfo.InvariantCulture, out long value) ? value : null;

    public static CallbackData? Parse(string? data) {
        if (string.IsNullOrWhiteSpace(data)) return null;

        string[] parts = data.Split(Separator, 3);

        if (parts.Length < 2 || parts[0].Length == 0 || parts[1].Length == 0) return null;

        return new CallbackData(parts[0], parts[1], parts.Length == 3 ? parts[2] : null);
    }

    public override string ToString() {
        string data = Argument == null ? $"{Prefix}{Separator}{Action}" : $"{Prefix}{Separator}{Action}{Separator}{Argument}";

        if (data.Length > MaxLength) throw new InvalidOperationException($"Callback data '{data}' is longer than {MaxLength} bytes.");

        return data;
    }
}

/// <summary>
/// The buttons of the bot.
/// </summary>
public static class Callbacks {
    public const string Menu = "m";
    public const string Alert = "a";
    public const string Wizard = "w";
    public const string Pause = "p";
    public const string Account = "x";
    public const string Admin = "d";
    public const string Notification = "n";

    /* Menu */
    public const string MenuHome = "home";
    public const string MenuNewAlert = "new";
    public const string MenuAlerts = "alerts";
    public const string MenuPause = "pause";
    public const string MenuHelp = "help";

    /* Alert */
    public const string AlertToggle = "toggle";
    public const string AlertEdit = "edit";
    public const string AlertDelete = "del";

    /* Wizard */
    public const string WizardKeepText = "keep";
    public const string WizardPrice = "price";
    public const string WizardSave = "save";
    public const string WizardFix = "fix";
    public const string WizardCancel = "cancel";

    public const string PriceNone = "none";
    public const string PriceCustom = "custom";
    public const string PriceKeep = "keep";

    /* Account */
    public const string AccountDeleteConfirm = "yes";
    public const string AccountDeleteCancel = "no";

    /* Admin */
    public const string AdminStats = "stats";
    public const string AdminChannels = "ch";
    public const string AdminAddChannels = "add";

    /* Notification */
    public const string NotificationTurnOffAlert = "off";
    public const string NotificationNotRelevant = "fb";

    public static string Build(string prefix, string action, object? argument = null) {
        string? value = argument == null ? null : Convert.ToString(argument, CultureInfo.InvariantCulture);

        return new CallbackData(prefix, action, value).ToString();
    }
}