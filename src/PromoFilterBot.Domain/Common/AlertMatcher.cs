/*
 * File: AlertMatcher.cs
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
/// An offer matches an alert when it has every term of the alert, none of its excluded terms, and costs up to its limit.
/// </summary>
public static class AlertMatcher {
    public static bool MatchesTerms(AlertQuery query, OfferText offer) {
        return query.IsValid && query.RequiredTerms.All(offer.Contains) && !query.ExcludedTerms.Any(offer.Contains);
    }

    /// <summary>
    /// The match, or null when the offer does not match the alert. An offer without a price the extractor could read
    /// still matches an alert with a limit when <paramref name="notifyWhenPriceUnknown"/>, flagged as such.
    /// </summary>
    public static AlertMatch? Evaluate(IndexedAlert alert, OfferText offer, decimal? price, bool notifyWhenPriceUnknown) {
        if (!MatchesTerms(alert.Query, offer)) return null;

        if (alert.MaxPrice.HasValue) {
            if (!price.HasValue) return notifyWhenPriceUnknown ? new AlertMatch(alert, PriceUnknown: true) : null;
            if (price.Value > alert.MaxPrice.Value) return null;
        }

        return new AlertMatch(alert, PriceUnknown: false);
    }

    public static IReadOnlyList<AlertMatch> FindMatches(AlertIndex index, OfferText offer, decimal? price, bool notifyWhenPriceUnknown) {
        List<AlertMatch> matches = [];

        foreach (IndexedAlert candidate in index.FindCandidates(offer)) {
            AlertMatch? match = Evaluate(candidate, offer, price, notifyWhenPriceUnknown);

            if (match != null) matches.Add(match);
        }

        return matches;
    }
}