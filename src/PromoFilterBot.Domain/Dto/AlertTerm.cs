/*
 * File: AlertTerm.cs
 * Project: Dto
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

namespace PromoFilterBot.Domain.Dto;

/// <summary>
/// A normalized word, or a phrase whose words must appear together and in order.
/// </summary>
public sealed class AlertTerm : IEquatable<AlertTerm> {
    public AlertTerm(string normalizedText) {
        Text = normalizedText;
        Tokens = normalizedText.Split(' ');
    }

    public string Text { get; }

    public IReadOnlyList<string> Tokens { get; }

    public bool IsPhrase => Tokens.Count > 1;

    public bool Equals(AlertTerm? other) => other != null && string.Equals(Text, other.Text, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as AlertTerm);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Text);

    public override string ToString() => Text;
}