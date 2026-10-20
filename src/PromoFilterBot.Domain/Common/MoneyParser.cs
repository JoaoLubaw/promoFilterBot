/*
 * File: MoneyParser.cs
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

using System.Globalization;
using System.Text.RegularExpressions;

namespace PromoFilterBot.Domain.Common;

/// <summary>
/// Reads an amount in reais as people write it: <c>1.299,90</c>, <c>1299,90</c>, <c>1.299</c>, <c>1299.90</c> or
/// <c>R$ 1.299,90</c>.
/// </summary>
public static class MoneyParser {
    private static readonly Regex CurrencyPrefix = new(@"^r\$\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex AmountShape = new(@"^\d[\d.,]*$", RegexOptions.Compiled);
    private static readonly Regex ThousandsWithDots = new(@"^\d{1,3}(\.\d{3})+$", RegexOptions.Compiled);
    private static readonly Regex DecimalWithDot = new(@"^\d+(\.\d{1,2})?$", RegexOptions.Compiled);

    public static bool TryParse(string? text, out decimal value) {
        value = 0;

        if (string.IsNullOrWhiteSpace(text)) return false;

        string amount = CurrencyPrefix.Replace(text.Trim(), string.Empty).TrimEnd('.', ' ');

        if (!AmountShape.IsMatch(amount)) return false;

        if (amount.Contains(',')) {
            // The comma is the decimal separator, and the dots before it group the thousands
            int comma = amount.LastIndexOf(',');
            string integerPart = amount[..comma].Replace(".", string.Empty);
            string decimalPart = amount[(comma + 1)..];

            if (integerPart.Length == 0 || decimalPart.Length is 0 or > 2) return false;
            if (!integerPart.All(char.IsAsciiDigit) || !decimalPart.All(char.IsAsciiDigit)) return false;

            amount = $"{integerPart}.{decimalPart}";
        }
        else if (ThousandsWithDots.IsMatch(amount)) {
            amount = amount.Replace(".", string.Empty);
        }
        else if (!DecimalWithDot.IsMatch(amount)) {
            return false;
        }

        return decimal.TryParse(amount, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value);
    }
}