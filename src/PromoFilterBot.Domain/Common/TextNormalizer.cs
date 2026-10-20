/*
 * File: TextNormalizer.cs
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
using System.Text;

namespace PromoFilterBot.Domain.Common;

/// <summary>
/// Puts the text of an offer and the search of an alert in the same form, so they are compared word by word: lower case,
/// without accents, emojis and punctuation, with single spaces, and with the numbers apart from the letters ("RTX4060"
/// and "rtx 4060" are both "rtx 4060").
/// </summary>
public static class TextNormalizer {
    public static string Normalize(string? text) {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        // The compatibility decomposition also turns the styled letters the channels use (𝗥𝗧𝗫, ｒｔｘ) into plain ones, and
        // splits the accents from the letters, so they can be dropped
        string decomposed = text.Normalize(NormalizationForm.FormKD).ToLowerInvariant();
        StringBuilder builder = new StringBuilder(decomposed.Length);
        char previous = ' ';

        foreach (char c in decomposed) {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;

            char current = char.IsLetterOrDigit(c) ? c : ' ';

            if (current == ' ') {
                if (previous != ' ') builder.Append(' ');
            }
            else {
                if (previous != ' ' && char.IsDigit(current) != char.IsDigit(previous)) builder.Append(' ');
                builder.Append(current);
            }

            previous = current;
        }

        return builder.ToString().Trim();
    }

    public static string[] Tokenize(string? text) {
        string normalized = Normalize(text);

        return normalized.Length == 0 ? [] : normalized.Split(' ');
    }

    /// <summary>
    /// Lower case and without accents, but keeping the punctuation and the spaces, to read the words around a value.
    /// </summary>
    public static string Fold(string? text) {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        string decomposed = text.Normalize(NormalizationForm.FormD).ToLowerInvariant();
        StringBuilder builder = new StringBuilder(decomposed.Length);

        foreach (char c in decomposed) {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) builder.Append(c);
        }

        return builder.ToString();
    }
}