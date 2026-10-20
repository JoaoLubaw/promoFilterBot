/*
 * File: StoreDetectorTest.cs
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

using PromoFilterBot.Domain.Common;

namespace PromoFilterBot.Domain.Tests.Common;

public class StoreDetectorTest {
    [Theory]
    [InlineData("https://www.amazon.com.br/dp/B0ABCDEFGH", "amazon")]
    [InlineData("https://amzn.to/3abc", "amazon")]
    [InlineData("https://produto.mercadolivre.com.br/MLB-123456789", "mercadolivre")]
    [InlineData("https://s.shopee.com.br/abc", "shopee")]
    [InlineData("https://www.magazineluiza.com.br/produto/p/abc123def/", "magalu")]
    [InlineData("https://www.kabum.com.br/produto/123", "kabum")]
    [InlineData("https://pt.aliexpress.com/item/100.html", "aliexpress")]
    public void FromLink_ShouldFindTheStore_ByTheHost(string link, string store) {
        Assert.Equal(store, StoreDetector.FromLink(link));
    }

    [Fact]
    public void FromLink_ShouldReturnNull_ForAnUnknownHost() {
        Assert.Null(StoreDetector.FromLink("https://bit.ly/abc"));
        Assert.Null(StoreDetector.FromLink("não é link"));
    }

    [Fact]
    public void Detect_ShouldPreferTheLink_AndElseTheFirstStoreNamedInTheText() {
        string text = TextNormalizer.Normalize("Air fryer no Mercado Livre, mais barata que na Amazon");

        Assert.Equal("kabum", StoreDetector.Detect(["https://bit.ly/x", "https://www.kabum.com.br/produto/1"], text));
        Assert.Equal("mercadolivre", StoreDetector.Detect(["https://bit.ly/x"], text));
        Assert.Equal(StoreDetector.Unknown, StoreDetector.Detect([], TextNormalizer.Normalize("Fone bluetooth barato")));
    }
}