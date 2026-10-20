/*
 * File: Formatters.cs
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

namespace PromoFilterBot.Bot.Common;

/// <summary>
/// Values as a Brazilian reads them. The number format is built here, so it does not depend on the ICU of the image.
/// </summary>
public static class Formatters {
    private static readonly NumberFormatInfo BrazilianNumbers = new NumberFormatInfo {
        NumberDecimalSeparator = ",",
        NumberGroupSeparator = ".",
        NumberGroupSizes = [3]
    };

    public static string Price(decimal value) => $"R$ {value.ToString("N2", BrazilianNumbers)}";

    public static string Price(decimal? value) => value.HasValue ? Price(value.Value) : "—";

    public static string MaxPrice(decimal? value) => value.HasValue ? $"até {Price(value.Value)}" : "sem limite";

    public static string LocalDateTime(DateTime utc, TimeZoneInfo timeZone) {
        DateTime local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), timeZone);

        return local.ToString("dd/MM 'às' HH:mm", CultureInfo.InvariantCulture);
    }
}