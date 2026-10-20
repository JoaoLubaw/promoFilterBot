/*
 * File: OfferText.cs
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

using PromoFilterBot.Domain.Dto;

namespace PromoFilterBot.Domain.Common;

/// <summary>
/// Normalized words of an offer, ready to look for the terms of the alerts.
/// </summary>
public sealed class OfferText {
    public OfferText(string normalizedText) {
        NormalizedText = normalizedText;
        Tokens = normalizedText.Length == 0 ? [] : normalizedText.Split(' ');
        TokenSet = new HashSet<string>(Tokens, StringComparer.Ordinal);
    }

    public string NormalizedText { get; }

    public IReadOnlyList<string> Tokens { get; }

    public IReadOnlySet<string> TokenSet { get; }

    public static OfferText FromRawText(string? text) => new OfferText(TextNormalizer.Normalize(text));

    /// <summary>
    /// A word must be one of the words of the offer; a phrase, its words one after the other.
    /// </summary>
    public bool Contains(AlertTerm term) {
        if (!term.IsPhrase) return TokenSet.Contains(term.Text);

        IReadOnlyList<string> phrase = term.Tokens;

        if (!TokenSet.Contains(phrase[0])) return false;

        for (int start = 0; start <= Tokens.Count - phrase.Count; start++) {
            int matched = 0;

            while (matched < phrase.Count && string.Equals(Tokens[start + matched], phrase[matched], StringComparison.Ordinal)) {
                matched++;
            }

            if (matched == phrase.Count) return true;
        }

        return false;
    }
}