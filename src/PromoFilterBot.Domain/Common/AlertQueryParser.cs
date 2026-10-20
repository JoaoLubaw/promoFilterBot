/*
 * File: AlertQueryParser.cs
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
/// Reads the search typed by the user:
/// <list type="bullet">
/// <item><c>rtx 4060</c>: contains "rtx" and "4060", in any order;</item>
/// <item><c>"air fryer"</c>: the exact phrase;</item>
/// <item><c>ssd 1tb -sata</c>: contains "ssd" and "1tb", but not "sata".</item>
/// </list>
/// A word the normalization splits ("rtx4060", "wi-fi") becomes a phrase, so "1tb" does not match a "1" and a "tb" far
/// apart. Loose connectives ("cadeira de escritório") are ignored, since the offers often leave them out.
/// </summary>
public static class AlertQueryParser {
    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal) {
        "a", "o", "as", "os", "e", "ou", "de", "da", "do", "das", "dos", "em", "no", "na", "nos", "nas", "com", "para",
        "pra", "por", "um", "uma"
    };

    public static AlertQuery Parse(string? input) {
        List<AlertTerm> requiredTerms = [];
        List<AlertTerm> excludedTerms = [];

        if (string.IsNullOrWhiteSpace(input)) return new AlertQuery(requiredTerms, excludedTerms);

        int position = 0;

        while (position < input.Length) {
            if (char.IsWhiteSpace(input[position])) {
                position++;
                continue;
            }

            bool excluded = false;

            if (input[position] == '-' && position + 1 < input.Length && !char.IsWhiteSpace(input[position + 1])) {
                excluded = true;
                position++;
            }

            bool quoted = IsQuote(input[position]);
            string raw = quoted ? ReadQuoted(input, ref position) : ReadWord(input, ref position);
            string normalized = TextNormalizer.Normalize(raw);

            if (normalized.Length == 0) continue;
            if (!quoted && !excluded && StopWords.Contains(normalized)) continue;

            AlertTerm term = new AlertTerm(normalized);
            List<AlertTerm> terms = excluded ? excludedTerms : requiredTerms;

            if (!terms.Contains(term)) terms.Add(term);
        }

        return new AlertQuery(requiredTerms, excludedTerms);
    }

    // Phones type curly quotes, so they open and close phrases too
    private static bool IsQuote(char c) => c is '"' or '“' or '”' or '„' or '«' or '»';

    private static string ReadQuoted(string input, ref int position) {
        int start = ++position;

        while (position < input.Length && !IsQuote(input[position])) position++;

        string phrase = input[start..position];

        // Skips the closing quote; an unclosed one takes the rest of the text
        if (position < input.Length) position++;

        return phrase;
    }

    private static string ReadWord(string input, ref int position) {
        int start = position;

        while (position < input.Length && !char.IsWhiteSpace(input[position]) && !IsQuote(input[position])) position++;

        return input[start..position];
    }
}