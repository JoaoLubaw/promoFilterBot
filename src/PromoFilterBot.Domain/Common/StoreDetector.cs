/*
 * File: StoreDetector.cs
 * Project: Common
 * Create Date: Tuesday, 20th October 2026
 * Author: João Lubawaski (joaolubawaski@gmail.com)
 * -----
 * Last Modified: Tuesday, 20th October 2026 7:00:00 pm
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
/// Finds the store of an offer: by the host of its links, or, without a known link, by the name of the store in the
/// text. The same product, at the same price, in two stores is two offers.
/// </summary>
public static class StoreDetector {
    public const string Unknown = "";

    private const RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled;

    private static readonly StoreRule[] Stores = [
        new StoreRule("amazon", new Regex(@"(^|\.)amazon\.|^(amzn\.to|amzn\.com|a\.co)$", Options), new Regex(@"\bamazon\b", Options)),
        new StoreRule("mercadolivre", new Regex(@"(^|\.)mercadoli(vre|bre)\.", Options), new Regex(@"\bmercado ?livre\b", Options)),
        new StoreRule("shopee", new Regex(@"(^|\.)shopee\.", Options), new Regex(@"\bshopee\b", Options)),
        new StoreRule("magalu", new Regex(@"(^|\.)(magazineluiza|magazinevoce|magalu)\.", Options), new Regex(@"\bmagalu\b|\bmagazine luiza\b", Options)),
        new StoreRule("kabum", new Regex(@"(^|\.)kabum\.", Options), new Regex(@"\bkabum\b", Options)),
        new StoreRule("aliexpress", new Regex(@"(^|\.)aliexpress\.", Options), new Regex(@"\baliexpress\b", Options)),
        new StoreRule("americanas", new Regex(@"(^|\.)americanas\.", Options), new Regex(@"\bamericanas\b", Options)),
        new StoreRule("casasbahia", new Regex(@"(^|\.)casasbahia\.", Options), new Regex(@"\bcasas bahia\b", Options)),
        new StoreRule("pontofrio", new Regex(@"(^|\.)pontofrio\.", Options), new Regex(@"\bponto frio\b", Options)),
        new StoreRule("carrefour", new Regex(@"(^|\.)carrefour\.", Options), new Regex(@"\bcarrefour\b", Options)),
        new StoreRule("submarino", new Regex(@"(^|\.)submarino\.", Options), new Regex(@"\bsubmarino\b", Options)),
        new StoreRule("shein", new Regex(@"(^|\.)shein\.", Options), new Regex(@"\bshein\b", Options)),
        new StoreRule("temu", new Regex(@"(^|\.)temu\.", Options), new Regex(@"\btemu\b", Options)),
        new StoreRule("terabyte", new Regex(@"(^|\.)terabyteshop\.", Options), new Regex(@"\bterabyte\b", Options)),
        new StoreRule("pichau", new Regex(@"(^|\.)pichau\.", Options), new Regex(@"\bpichau\b", Options)),
        new StoreRule("fastshop", new Regex(@"(^|\.)fastshop\.", Options), new Regex(@"\bfast ?shop\b", Options)),
        new StoreRule("netshoes", new Regex(@"(^|\.)netshoes\.", Options), new Regex(@"\bnetshoes\b", Options)),
        new StoreRule("centauro", new Regex(@"(^|\.)centauro\.", Options), new Regex(@"\bcentauro\b", Options))
    ];

    /// <summary>
    /// The store of the first link of a known store, or else the store named first in the normalized text, or
    /// <see cref="Unknown"/>.
    /// </summary>
    public static string Detect(IEnumerable<string> links, string normalizedText) {
        foreach (string link in links) {
            string? store = FromLink(link);

            if (store != null) return store;
        }

        return FromText(normalizedText) ?? Unknown;
    }

    public static string? FromLink(string? link) {
        Uri? uri = LinkExtractor.ToUri(link);

        if (uri == null) return null;

        return Stores.FirstOrDefault(store => store.Host.IsMatch(uri.Host))?.Id;
    }

    public static string? FromText(string? normalizedText) {
        if (string.IsNullOrEmpty(normalizedText)) return null;

        string? found = null;
        int foundAt = int.MaxValue;

        foreach (StoreRule store in Stores) {
            Match match = store.Name.Match(normalizedText);

            if (match.Success && match.Index < foundAt) {
                found = store.Id;
                foundAt = match.Index;
            }
        }

        return found;
    }

    private sealed record StoreRule(string Id, Regex Host, Regex Name);
}