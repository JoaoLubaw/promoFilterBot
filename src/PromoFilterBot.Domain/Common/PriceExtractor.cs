/*
 * File: PriceExtractor.cs
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
/// Finds the price of an offer in its original text. Offers usually say "de R$ X por R$ Y", so the value after "por" is
/// the price; without it, the lowest value is. Values that are not the price of the product are left out: installments
/// ("10x de R$ 129,90"), coupons, cashback, shipping and discounts ("R$ 50 OFF").
/// </summary>
public static class PriceExtractor {
    private const int ContextLength = 25;

    private static readonly Regex PriceRegex = new(
        @"R\$\s*(?<value>\d{1,3}(?:\.\d{3})+(?:,\d{1,2})?|\d+(?:[.,]\d{1,2})?)(?!\d)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex InstallmentBefore = new(
        @"\d+\s*x\s*(sem juros\s*)?(de\s*)?$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex NotThePriceBefore = new(
        @"(economi[a-z]*|desconto( de)?|cashback( de)?|frete( de)?|cupom( de)?|ganhe|acima de|compras? de|off de|bonus de)\s*:?\s*$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex NotThePriceAfter = new(
        @"^\s*(off\b|de desconto|em desconto|de cashback|em cashback|de volta|de frete|em compras|no cupom)",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex PorBefore = new(
        @"\bpor\b[^\d]{0,15}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static decimal? Extract(string? text) {
        if (string.IsNullOrEmpty(text)) return null;

        List<decimal> prices = [];
        List<decimal> pricesAfterPor = [];

        foreach (Match match in PriceRegex.Matches(text)) {
            if (!MoneyParser.TryParse(match.Groups["value"].Value, out decimal value) || value <= 0) continue;

            int contextStart = Math.Max(0, match.Index - ContextLength);
            string before = TextNormalizer.Fold(text[contextStart..match.Index]);

            int end = match.Index + match.Length;
            string after = TextNormalizer.Fold(text.Substring(end, Math.Min(ContextLength, text.Length - end)));

            if (InstallmentBefore.IsMatch(before) || NotThePriceBefore.IsMatch(before) || NotThePriceAfter.IsMatch(after)) {
                continue;
            }

            prices.Add(value);

            if (PorBefore.IsMatch(before)) pricesAfterPor.Add(value);
        }

        if (pricesAfterPor.Count > 0) return pricesAfterPor.Min();

        return prices.Count > 0 ? prices.Min() : null;
    }
}