/*
 * File: TelegramHtml.cs
 * Project: Common
 * Create Date: Tuesday, 20th October 2026
 * Author: João Lubawaski (joaolubawaski@gmail.com)
 * -----
 * Last Modified: Tuesday, 20th October 2026 7:00:00 pm
 * Modified by: João Lubawaski (joaolubawaski@gmail.com)
 * -----
 * Copyright (c) 2026 João Lubawaski
 * ----
 * HISTORY:
 * Date      	By	Comments
 * ----------	---	---------------------------------------------------------
 */

using System.Text;

using PromoFilterBot.Domain.Dto;

namespace PromoFilterBot.Domain.Common;

/// <summary>
/// Rebuilds the formatting of a post (bold, links hidden behind words, coupons in monospace...) as the HTML of the Bot
/// API, so the copy sent to the user looks like the original.
/// </summary>
public static class TelegramHtml {
    private const string Ellipsis = "…";

    public static string Escape(string? text) {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }

    public static string EscapeAttribute(string? text) => Escape(text).Replace("\"", "&quot;");

    /// <summary>
    /// The text with its entities as HTML. A text longer than <paramref name="maxLength"/> is cut, with the entities that
    /// pass the cut, and ends with "…".
    /// </summary>
    public static string FromEntities(string? text, IReadOnlyList<TextEntity>? entities, int maxLength = int.MaxValue) {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        bool truncated = text.Length > maxLength;

        if (truncated) {
            int cut = Math.Max(0, maxLength);

            // Never between the two halves of an emoji
            if (cut > 0 && char.IsHighSurrogate(text[cut - 1])) cut--;

            text = text[..cut].TrimEnd();
        }

        List<TextEntity> sorted = (entities ?? [])
            .Select(entity => Clip(entity, text.Length))
            .OfType<TextEntity>()
            .OrderBy(entity => entity.Offset)
            .ThenByDescending(entity => entity.Length)
            .ToList();

        StringBuilder html = new StringBuilder(text.Length + 64);
        List<TextEntity> open = [];
        int next = 0;

        for (int position = 0; position <= text.Length; position++) {
            CloseEndingAt(position, open, html);

            while (next < sorted.Count && sorted[next].Offset == position) {
                html.Append(OpenTag(sorted[next]));
                open.Add(sorted[next]);
                next++;
            }

            if (position < text.Length) AppendEscaped(text[position], html);
        }

        if (truncated) html.Append(Ellipsis);

        return html.ToString();
    }

    // Closes the entities that end here, the inner ones first. One that goes on past an entity that ends (they overlap)
    // is closed and opened again, so the tags stay nested
    private static void CloseEndingAt(int position, List<TextEntity> open, StringBuilder html) {
        int lowest = open.FindIndex(entity => entity.End == position);

        if (lowest < 0) return;

        List<TextEntity> reopen = [];

        for (int i = open.Count - 1; i >= lowest; i--) {
            html.Append(CloseTag(open[i]));

            if (open[i].End != position) reopen.Insert(0, open[i]);
        }

        open.RemoveRange(lowest, open.Count - lowest);

        foreach (TextEntity entity in reopen) {
            html.Append(OpenTag(entity));
            open.Add(entity);
        }
    }

    private static TextEntity? Clip(TextEntity entity, int textLength) {
        if (entity.Offset < 0 || entity.Length <= 0 || entity.Offset >= textLength) return null;
        if (entity.Type == TextEntityType.TextLink && string.IsNullOrWhiteSpace(entity.Url)) return null;

        int length = Math.Min(entity.Length, textLength - entity.Offset);

        return length == entity.Length ? entity : entity with { Length = length };
    }

    private static string OpenTag(TextEntity entity) => entity.Type switch {
        TextEntityType.Bold => "<b>",
        TextEntityType.Italic => "<i>",
        TextEntityType.Underline => "<u>",
        TextEntityType.Strikethrough => "<s>",
        TextEntityType.Spoiler => "<tg-spoiler>",
        TextEntityType.Code => "<code>",
        TextEntityType.Pre when !string.IsNullOrWhiteSpace(entity.Language) => $"<pre><code class=\"language-{EscapeAttribute(entity.Language)}\">",
        TextEntityType.Pre => "<pre>",
        TextEntityType.TextLink => $"<a href=\"{EscapeAttribute(entity.Url)}\">",
        TextEntityType.Blockquote => "<blockquote>",
        _ => string.Empty
    };

    private static string CloseTag(TextEntity entity) => entity.Type switch {
        TextEntityType.Bold => "</b>",
        TextEntityType.Italic => "</i>",
        TextEntityType.Underline => "</u>",
        TextEntityType.Strikethrough => "</s>",
        TextEntityType.Spoiler => "</tg-spoiler>",
        TextEntityType.Code => "</code>",
        TextEntityType.Pre when !string.IsNullOrWhiteSpace(entity.Language) => "</code></pre>",
        TextEntityType.Pre => "</pre>",
        TextEntityType.TextLink => "</a>",
        TextEntityType.Blockquote => "</blockquote>",
        _ => string.Empty
    };

    private static void AppendEscaped(char c, StringBuilder html) {
        switch (c) {
            case '&':
                html.Append("&amp;");
                break;
            case '<':
                html.Append("&lt;");
                break;
            case '>':
                html.Append("&gt;");
                break;
            default:
                html.Append(c);
                break;
        }
    }
}