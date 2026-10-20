/*
 * File: AlertQuery.cs
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
/// What an offer must contain, and must not contain, to match an alert.
/// </summary>
public sealed class AlertQuery {
    /// <summary>
    /// Separates the terms when they are saved. Normalized terms only have letters, digits and spaces.
    /// </summary>
    public const char TermSeparator = '|';

    public AlertQuery(IReadOnlyList<AlertTerm> requiredTerms, IReadOnlyList<AlertTerm> excludedTerms) {
        RequiredTerms = requiredTerms;
        ExcludedTerms = excludedTerms;
    }

    public IReadOnlyList<AlertTerm> RequiredTerms { get; }

    public IReadOnlyList<AlertTerm> ExcludedTerms { get; }

    /// <summary>
    /// An alert needs at least one term to look for.
    /// </summary>
    public bool IsValid => RequiredTerms.Count > 0;

    public string StoredRequiredTerms => string.Join(TermSeparator, RequiredTerms.Select(t => t.Text));

    public string StoredExcludedTerms => string.Join(TermSeparator, ExcludedTerms.Select(t => t.Text));

    public static AlertQuery FromStored(string? requiredTerms, string? excludedTerms) {
        return new AlertQuery(ParseStored(requiredTerms), ParseStored(excludedTerms));
    }

    /// <summary>
    /// How the bot shows the terms to the user, such as <c>rtx · 4060 · "air fryer" · sem: sata</c>.
    /// </summary>
    public string Describe() {
        string required = string.Join(" · ", RequiredTerms.Select(Describe));

        if (ExcludedTerms.Count == 0) return required;

        return $"{required} · sem: {string.Join(", ", ExcludedTerms.Select(Describe))}";
    }

    private static string Describe(AlertTerm term) => term.IsPhrase ? $"\"{term.Text}\"" : term.Text;

    private static List<AlertTerm> ParseStored(string? stored) {
        if (string.IsNullOrWhiteSpace(stored)) return [];

        return stored
            .Split(TermSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => new AlertTerm(t))
            .Distinct()
            .ToList();
    }
}