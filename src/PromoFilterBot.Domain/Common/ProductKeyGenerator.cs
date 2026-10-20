/*
 * File: ProductKeyGenerator.cs
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

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace PromoFilterBot.Domain.Common;

/// <summary>
/// Identifies the product of an offer, so the same offer posted in several channels is sent once. The id of the store
/// in the link is used when it is known (ASIN of Amazon, MLB code of Mercado Livre, item of Shopee...); otherwise, a
/// hash of the normalized text.
/// </summary>
public static class ProductKeyGenerator {
    public const string TextKeyPrefix = "text:";

    private const RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled;

    private static readonly StoreRule[] Stores = [
        new StoreRule(
            new Regex(@"(^|\.)amazon\.", Options),
            new Regex(@"/(?:dp|gp/product|gp/aw/d|exec/obidos/asin|o/asin|product)/(?<id>[A-Z0-9]{10})(?=[/?#&]|$)", Options),
            m => $"amazon:{m.Groups["id"].Value.ToUpperInvariant()}"),
        new StoreRule(
            new Regex(@"(^|\.)mercadoli(vre|bre)\.", Options),
            new Regex(@"MLB-?(?<id>\d{6,})", Options),
            m => $"mlb:{m.Groups["id"].Value}"),
        new StoreRule(
            new Regex(@"(^|\.)shopee\.", Options),
            new Regex(@"/product/(?<shop>\d+)/(?<item>\d+)|-i\.(?<shop>\d+)\.(?<item>\d+)", Options),
            m => $"shopee:{m.Groups["shop"].Value}.{m.Groups["item"].Value}"),
        new StoreRule(
            new Regex(@"(^|\.)(magazineluiza|magazinevoce|magalu)\.", Options),
            new Regex(@"/p/(?<id>[a-z0-9]{6,})(?=[/?#]|$)", Options),
            m => $"magalu:{m.Groups["id"].Value.ToLowerInvariant()}"),
        new StoreRule(
            new Regex(@"(^|\.)kabum\.", Options),
            new Regex(@"/produto/(?<id>\d+)", Options),
            m => $"kabum:{m.Groups["id"].Value}"),
        new StoreRule(
            new Regex(@"(^|\.)aliexpress\.", Options),
            new Regex(@"/item/(?<id>\d+)\.html", Options),
            m => $"aliexpress:{m.Groups["id"].Value}")
    ];

    public static string Generate(IEnumerable<string> links, string normalizedText) {
        return FromLinks(links) ?? FromText(normalizedText);
    }

    public static string? FromLinks(IEnumerable<string> links) {
        foreach (string link in links) {
            string? key = FromLink(link);

            if (key != null) return key;
        }

        return null;
    }

    /// <summary>
    /// Key of the product from a link of a known store, or null.
    /// </summary>
    public static string? FromLink(string? link) {
        Uri? uri = LinkExtractor.ToUri(link);

        if (uri == null) return null;

        string pathAndQuery = Uri.UnescapeDataString(uri.PathAndQuery);

        foreach (StoreRule store in Stores) {
            if (!store.Host.IsMatch(uri.Host)) continue;

            Match match = store.Product.Match(pathAndQuery);

            if (match.Success) return store.Key(match);
        }

        return null;
    }

    public static string FromText(string normalizedText) {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedText));

        return $"{TextKeyPrefix}{Convert.ToHexStringLower(hash)[..16]}";
    }

    private sealed record StoreRule(Regex Host, Regex Product, Func<Match, string> Key);
}