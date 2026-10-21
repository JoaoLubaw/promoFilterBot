/*
 * File: ShortLinkResolverTest.cs
 * Project: Services
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

using Microsoft.Extensions.Logging.Abstractions;

using PromoFilterBot.Domain.Common;
using PromoFilterBot.Infrastructure.Options;
using PromoFilterBot.Infrastructure.Services;
using PromoFilterBot.Tests.Helpers;

using MsOptions = Microsoft.Extensions.Options.Options;

namespace PromoFilterBot.Tests.Services;

public class ShortLinkResolverTest {
    private const string MeliLink = "https://meli.la/1jg43sS";
    private const string MeliSocialPage = "https://www.mercadolivre.com.br/social/agenciarice?matt_word=julia&ref=BFl8Jwe1";

    private readonly StubHttpHandler _handler = new StubHttpHandler();

    private ShortLinkResolver CreateResolver() {
        return new ShortLinkResolver(
            new HttpClient(_handler),
            MsOptions.Create(new MatchingOptions { ShortLinkTimeoutSeconds = 3 }),
            NullLogger<ShortLinkResolver>.Instance);
    }

    [Theory]
    [InlineData("https://meli.la/1iHTEvw", true)]
    [InlineData("https://amzlink.to/az0oRojhBySYt", true)]
    [InlineData("https://www.amzn.to/3abc", true)]
    [InlineData("https://www.amazon.com.br/dp/B0H3412W8D", false)]
    [InlineData("https://produto.mercadolivre.com.br/MLB-123456789", false)]
    public void IsShortLink_ShouldKnowTheShortenersOfTheStores(string link, bool expected) {
        Assert.Equal(expected, CreateResolver().IsShortLink(link));
    }

    [Fact]
    public async Task Resolve_ShouldStopAtThePageOfTheProduct_OfAnAmazonAffiliateLink() {
        _handler
            .Redirect("https://amzlink.to/az0oRojhBySYt", "https://r.amzlink.to/?btn_url=https%3A%2F%2Fwww.amazon.com.br%2Fdp%2FB0H3412W8D")
            .Redirect("https://r.amzlink.to/?btn_url=https%3A%2F%2Fwww.amazon.com.br%2Fdp%2FB0H3412W8D", "https://www.amazon.com.br/Lola-Cosmetics/dp/B0H3412W8D?tag=canal-20");

        string? resolved = await CreateResolver().ResolveAsync("https://amzlink.to/az0oRojhBySYt");

        Assert.Equal("amazon:B0H3412W8D", ProductKeyGenerator.FromLink(resolved));
        // The page of the store itself is not downloaded: its address is enough
        Assert.DoesNotContain(_handler.Requests, url => url.StartsWith("https://www.amazon.com.br", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Resolve_ShouldTakeTheFirstItem_OfTheAffiliatePageOfMercadoLivre() {
        _handler
            .Redirect(MeliLink, MeliSocialPage, System.Net.HttpStatusCode.MovedPermanently)
            .Page(MeliSocialPage, "<html><img src=\"D_NQ_NP_830614-MLB110045875480_042026-O.webp\"/>" + new string(' ', 40_000) +
                "<a href=\"https://www.mercadolivre.com.br/tenis/up/MLBU3950205394?pdp_filters=item_id%3AMLB6722992126\">" +
                "<a href=\"https://www.mercadolivre.com.br/outro/up/MLBU1?pdp_filters=item_id%3AMLB6722991536\"></html>");

        string? resolved = await CreateResolver().ResolveAsync(MeliLink);

        Assert.Equal("https://produto.mercadolivre.com.br/MLB-6722992126", resolved);
        Assert.Equal("mlb:6722992126", ProductKeyGenerator.FromLink(resolved));
        Assert.Equal("mercadolivre", StoreDetector.FromLink(resolved));
    }

    [Fact]
    public async Task Resolve_ShouldReturnTheLastPage_WhenTheAffiliatePageHasNoItem() {
        _handler
            .Redirect(MeliLink, MeliSocialPage, System.Net.HttpStatusCode.MovedPermanently)
            .Page(MeliSocialPage, "<html>sem produtos</html>");

        string? resolved = await CreateResolver().ResolveAsync(MeliLink);

        Assert.Equal(MeliSocialPage, resolved);
        Assert.Null(ProductKeyGenerator.FromLink(resolved));
    }

    [Fact]
    public async Task Resolve_ShouldReturnNull_WhenTheLinkDoesNotRedirect() {
        string? resolved = await CreateResolver().ResolveAsync("https://bit.ly/nada");

        Assert.Null(resolved);
    }
}