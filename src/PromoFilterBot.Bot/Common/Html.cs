/*
 * File: Html.cs
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

namespace PromoFilterBot.Bot.Common;

/// <summary>
/// Telegram HTML: only &lt;, &gt; and &amp; must be escaped in the text (and the quotes in the attributes).
/// </summary>
public static class Html {
    public static string Escape(string? text) {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }

    public static string EscapeAttribute(string? text) => Escape(text).Replace("\"", "&quot;");

    /// <summary>
    /// Cuts the text at <paramref name="maxLength"/> characters with an ellipsis, without splitting an emoji.
    /// </summary>
    public static string Truncate(string? text, int maxLength) {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength) return text ?? string.Empty;

        int length = maxLength - 1;

        if (length > 0 && char.IsHighSurrogate(text[length - 1])) length--;

        return $"{text[..length].TrimEnd()}…";
    }
}