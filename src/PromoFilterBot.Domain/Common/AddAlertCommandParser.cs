/*
 * File: AddAlertCommandParser.cs
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
/// Splits <c>rtx 4060 max 2000</c> (or <c>até R$ 2.000</c>) into the search and the highest price.
/// </summary>
public static class AddAlertCommandParser {
    private static readonly Regex MaxPriceRegex = new(
        @"\s+(?:m[aá]x(?:imo)?|at[eé])\s*:?\s*(?:r\$\s*)?(?<price>\d[\d.,]*)\s*(?:reais)?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static AddAlertCommand Parse(string? input) {
        string text = input?.Trim() ?? string.Empty;
        Match match = MaxPriceRegex.Match(text);

        if (!match.Success) return new AddAlertCommand(text, null, false);

        string search = text[..match.Index].Trim();

        if (!MoneyParser.TryParse(match.Groups["price"].Value, out decimal maxPrice)) {
            return new AddAlertCommand(search, null, true);
        }

        return new AddAlertCommand(search, maxPrice, false);
    }
}