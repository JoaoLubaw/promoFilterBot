/*
 * File: LinkExtractorTest.cs
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

using PromoFilterBot.Domain.Common;

namespace PromoFilterBot.Domain.Tests.Common;

public class LinkExtractorTest {

    [Fact]
    public void Extract_ShouldFindTheLinksOfTheText_WithoutTheFinalPunctuation() {
        IReadOnlyList<string> links = LinkExtractor.Extract("Compre: https://amzn.to/3abc. Ou www.kabum.com.br/produto/1!");

        Assert.Equal(["https://amzn.to/3abc", "https://www.kabum.com.br/produto/1"], links);
    }

    [Fact]
    public void Extract_ShouldKeepEachLinkOnce() {
        Assert.Single(LinkExtractor.Extract("https://amzn.to/x https://amzn.to/x"));
        Assert.Empty(LinkExtractor.Extract(null));
    }

    [Theory]
    [InlineData("amzn.to/abc", "https://amzn.to/abc")]
    [InlineData("https://amzn.to", "https://amzn.to/")]
    [InlineData(" http://a.co/x) ", "http://a.co/x")]
    public void Normalize_ShouldGiveTheLinkAScheme(string link, string expected) {
        Assert.Equal(expected, LinkExtractor.Normalize(link));
    }

    [Theory]
    [InlineData("not a link")]
    [InlineData("ftp://files.com/a")]
    [InlineData("localhost")]
    [InlineData("")]
    public void Normalize_ShouldBeNull_ForWhatIsNotAWebLink(string link) {
        Assert.Null(LinkExtractor.Normalize(link));
    }

    [Fact]
    public void IsTelegramLink_ShouldTellThePostsOfTelegramFromTheOffers() {
        Assert.True(LinkExtractor.IsTelegramLink("https://t.me/canal/123"));
        Assert.False(LinkExtractor.IsTelegramLink("https://amzn.to/abc"));
    }
}