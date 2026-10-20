/*
 * File: TextEntity.cs
 * Project: Dto
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

namespace PromoFilterBot.Domain.Dto;

public enum TextEntityType {
    Bold = 0,
    Italic = 1,
    Underline = 2,
    Strikethrough = 3,
    Spoiler = 4,
    Code = 5,
    Pre = 6,
    TextLink = 7,
    Blockquote = 8
}

/// <summary>
/// Formatting of a stretch of a post, as Telegram sends it: offset and length in UTF-16 code units, the same as a C#
/// string.
/// </summary>
public sealed record TextEntity(TextEntityType Type, int Offset, int Length, string? Url = null, string? Language = null) {
    public int End => Offset + Length;
}