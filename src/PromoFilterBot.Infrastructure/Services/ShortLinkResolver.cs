/*
 * File: ShortLinkResolver.cs
 * Project: Services
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

using System.Collections.Concurrent;
using System.Net;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using PromoFilterBot.Domain.Common;
using PromoFilterBot.Infrastructure.Interfaces;
using PromoFilterBot.Infrastructure.Options;

namespace PromoFilterBot.Infrastructure.Services;

/// <summary>
/// Follows the redirects of a short link one by one (the HttpClient is registered without automatic redirects),
/// stopping at the first link of a known store.
/// </summary>
public sealed class ShortLinkResolver : IShortLinkResolver {
    private const int MaxRedirects = 6;
    private const int MaxCachedLinks = 5000;

    private static readonly string[] DefaultShortLinkHosts = [
        "amzn.to", "a.co", "amzn.divulgador.link", "mercadolivre.com", "mercadolibre.com", "s.shopee.com.br", "shope.ee",
        "magalu.lu", "tidd.ly", "bit.ly", "tinyurl.com", "cutt.ly", "is.gd", "encurtador.com.br", "s.click.aliexpress.com",
        "a.aliexpress.com", "kbm.vc"
    ];

    private readonly HttpClient _httpClient;
    private readonly MatchingOptions _matchingOptions;
    private readonly ILogger<ShortLinkResolver> _logger;
    private readonly ConcurrentDictionary<string, string?> _cache = new ConcurrentDictionary<string, string?>(StringComparer.Ordinal);
    private readonly HashSet<string> _shortLinkHosts;

    public ShortLinkResolver(HttpClient httpClient, IOptions<MatchingOptions> matchingOptions, ILogger<ShortLinkResolver> logger) {
        _httpClient = httpClient;
        _matchingOptions = matchingOptions.Value;
        _logger = logger;

        IEnumerable<string> hosts = _matchingOptions.ShortLinkHosts.Count > 0 ? _matchingOptions.ShortLinkHosts : DefaultShortLinkHosts;
        _shortLinkHosts = hosts.Select(h => h.Trim().ToLowerInvariant()).ToHashSet(StringComparer.Ordinal);
    }

    public bool IsShortLink(string link) {
        Uri? uri = LinkExtractor.ToUri(link);

        if (uri == null) return false;

        string host = uri.Host.ToLowerInvariant();

        if (host.StartsWith("www.", StringComparison.Ordinal)) host = host[4..];

        // mercadolivre.com/sec/... is short; mercadolivre.com.br is the store itself
        return _shortLinkHosts.Contains(host);
    }

    public async Task<string?> ResolveAsync(string link, CancellationToken cancellationToken = default) {
        if (_cache.TryGetValue(link, out string? cached)) return cached;

        string? resolved = await FollowRedirectsAsync(link, cancellationToken);

        if (_cache.Count >= MaxCachedLinks) _cache.Clear();

        _cache[link] = resolved;

        return resolved;
    }

    private async Task<string?> FollowRedirectsAsync(string link, CancellationToken cancellationToken) {
        Uri? current = LinkExtractor.ToUri(link);

        if (current == null) return null;

        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_matchingOptions.ShortLinkTimeoutSeconds));

        try {
            for (int redirect = 0; redirect < MaxRedirects; redirect++) {
                if (ProductKeyGenerator.FromLink(current.AbsoluteUri) != null) return current.AbsoluteUri;

                using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, current);
                using HttpResponseMessage response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);

                bool isRedirect = (int)response.StatusCode is >= 300 and < 400 && response.Headers.Location != null;

                if (!isRedirect) break;

                current = response.Headers.Location!.IsAbsoluteUri ? response.Headers.Location : new Uri(current, response.Headers.Location);
            }

            return current.AbsoluteUri == LinkExtractor.Normalize(link) ? null : current.AbsoluteUri;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or WebException) {
            if (cancellationToken.IsCancellationRequested) throw;

            _logger.LogDebug(ex, "Short link {Link} could not be followed", link);
            return null;
        }
    }
}