/*
 * File: PauseDurationParser.cs
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

using PromoFilterBot.Domain.Dto;

namespace PromoFilterBot.Domain.Common;

/// <summary>
/// Reads the argument of <c>/pausar</c>: <c>30m</c>, <c>8h</c>, <c>2 dias</c>, <c>amanhã</c> or <c>retomar</c>.
/// </summary>
public static class PauseDurationParser {
    public static readonly TimeSpan MaxDuration = TimeSpan.FromDays(30);

    private static readonly HashSet<string> ResumeWords = new(StringComparer.Ordinal) {
        "retomar", "retoma", "voltar", "despausar", "continuar", "reativar"
    };

    private static readonly Regex DurationRegex = new(
        @"^(?<amount>\d+) (?<unit>m|min|mins|minuto|minutos|h|hr|hrs|hora|horas|d|dia|dias)$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static PauseRequest? Parse(string? input) {
        string normalized = TextNormalizer.Normalize(input);

        if (normalized.Length == 0) return null;
        if (ResumeWords.Contains(normalized)) return PauseRequest.Resume;
        if (normalized.Split(' ').Contains("amanha")) return PauseRequest.UntilTomorrow;

        Match match = DurationRegex.Match(normalized);

        if (!match.Success || !int.TryParse(match.Groups["amount"].Value, out int amount) || amount <= 0) return null;

        TimeSpan duration = match.Groups["unit"].Value[0] switch {
            'm' => TimeSpan.FromMinutes(amount),
            'h' => TimeSpan.FromHours(amount),
            _ => TimeSpan.FromDays(amount)
        };

        return duration > MaxDuration ? null : PauseRequest.For(duration);
    }
}