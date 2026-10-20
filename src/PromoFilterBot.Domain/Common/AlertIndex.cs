/*
 * File: AlertIndex.cs
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
/// Inverted index of the active alerts, so an offer is only compared with the alerts that can match it. An alert needs
/// every one of its words, so it is filed under its rarest word (the one fewer alerts have) and only checked when the
/// offer has that word.
/// </summary>
public sealed class AlertIndex {
    private readonly Dictionary<string, List<IndexedAlert>> _alertsByAnchor;

    private AlertIndex(Dictionary<string, List<IndexedAlert>> alertsByAnchor, int count) {
        _alertsByAnchor = alertsByAnchor;
        Count = count;
    }

    public static AlertIndex Empty { get; } = new AlertIndex(new Dictionary<string, List<IndexedAlert>>(StringComparer.Ordinal), 0);

    /// <summary>
    /// Number of alerts in the index.
    /// </summary>
    public int Count { get; }

    public static AlertIndex Build(IEnumerable<IndexedAlert> alerts) {
        List<IndexedAlert> validAlerts = alerts.Where(a => a.Query.IsValid).ToList();
        Dictionary<string, int> frequency = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (IndexedAlert alert in validAlerts) {
            foreach (string token in RequiredTokens(alert)) {
                frequency[token] = frequency.GetValueOrDefault(token) + 1;
            }
        }

        Dictionary<string, List<IndexedAlert>> alertsByAnchor = new Dictionary<string, List<IndexedAlert>>(StringComparer.Ordinal);

        foreach (IndexedAlert alert in validAlerts) {
            string anchor = ChooseAnchor(RequiredTokens(alert), frequency);

            if (!alertsByAnchor.TryGetValue(anchor, out List<IndexedAlert>? anchored)) {
                anchored = [];
                alertsByAnchor[anchor] = anchored;
            }

            anchored.Add(alert);
        }

        return new AlertIndex(alertsByAnchor, validAlerts.Count);
    }

    /// <summary>
    /// The alerts whose rarest word is in the offer. They still have to be checked in full.
    /// </summary>
    public IReadOnlyList<IndexedAlert> FindCandidates(OfferText offer) {
        List<IndexedAlert> candidates = [];

        foreach (string token in offer.TokenSet) {
            if (_alertsByAnchor.TryGetValue(token, out List<IndexedAlert>? anchored)) candidates.AddRange(anchored);
        }

        return candidates;
    }

    /// <summary>
    /// The word an alert is filed under, exposed for the tests.
    /// </summary>
    internal static string ChooseAnchor(IEnumerable<string> tokens, IReadOnlyDictionary<string, int> frequency) {
        // On a tie, a word with digits (a model, "4060") and then a longer one are less common in the offers
        return tokens
            .OrderBy(t => frequency.GetValueOrDefault(t))
            .ThenByDescending(t => t.Any(char.IsDigit))
            .ThenByDescending(t => t.Length)
            .ThenBy(t => t, StringComparer.Ordinal)
            .First();
    }

    private static IEnumerable<string> RequiredTokens(IndexedAlert alert) {
        return alert.Query.RequiredTerms.SelectMany(t => t.Tokens).Distinct(StringComparer.Ordinal);
    }
}