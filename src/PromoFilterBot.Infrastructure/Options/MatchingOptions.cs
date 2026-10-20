/*
 * File: MatchingOptions.cs
 * Project: Options
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

namespace PromoFilterBot.Infrastructure.Options;

public class MatchingOptions {
    public const string SectionName = "Matching";

    /// <summary>
    /// An offer whose price could not be read is sent to an alert with a limit, flagged "preço não identificado".
    /// </summary>
    public bool NotifyWhenPriceUnknown { get; set; } = true;

    /// <summary>
    /// Follows the short links (amzn.to, s.shopee.com.br...) to find the product and the store, so the same offer posted
    /// with the short links of different channels is sent once.
    /// </summary>
    public bool ResolveShortLinks { get; set; } = true;

    /// <summary>
    /// Hosts of short links. Empty uses the built-in list.
    /// </summary>
    public List<string> ShortLinkHosts { get; set; } = [];

    /// <summary>
    /// The offer waits for the short link at most this long; without an answer, it goes on identified by its text.
    /// </summary>
    public int ShortLinkTimeoutSeconds { get; set; } = 3;

    /// <summary>
    /// The alert index is rebuilt when an alert changes, and at least this often.
    /// </summary>
    public int AlertIndexRefreshMinutes { get; set; } = 10;

    public int OfferQueueCapacity { get; set; } = 1000;
}