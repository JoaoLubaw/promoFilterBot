/*
 * File: LinkExtractor.cs
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

using System.Text.RegularExpressions;

namespace PromoFilterBot.Domain.Common;

/// <summary>
/// Finds the links written in the text of a post. The reader also takes the links hidden behind words, which only
/// Telegram knows.
/// </summary>
public static class LinkExtractor {
    private static readonly Regex UrlRegex = new(
        @"(?:https?://|www\.)[^\s<>""'«»]+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly char[] TrailingPunctuation = ['.', ',', ';', ':', '!', '?', ')', ']', '}', '*', '_'];

    public static IReadOnlyList<string> Extract(string? text) {
        if (string.IsNullOrEmpty(text)) return [];

        return UrlRegex.Matches(text)
            .Select(m => Normalize(m.Value))
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// The link with a scheme ("amzn.to/abc" becomes "https://amzn.to/abc"), or null when it is not a web link.
    /// </summary>
    public static string? Normalize(string? link) {
        if (string.IsNullOrWhiteSpace(link)) return null;

        string trimmed = link.Trim().TrimEnd(TrailingPunctuation);

        if (!trimmed.Contains("://", StringComparison.Ordinal)) trimmed = $"https://{trimmed}";

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out Uri? uri)) return null;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return null;
        if (!uri.Host.Contains('.')) return null;

        return uri.AbsoluteUri;
    }

    public static Uri? ToUri(string? link) {
        string? normalized = Normalize(link);

        return normalized == null ? null : new Uri(normalized);
    }

    /// <summary>
    /// Links to Telegram itself (other channels, posts) are not offers.
    /// </summary>
    public static bool IsTelegramLink(string link) {
        Uri? uri = ToUri(link);

        return uri != null && uri.Host is "t.me" or "telegram.me" or "telegram.dog";
    }
}