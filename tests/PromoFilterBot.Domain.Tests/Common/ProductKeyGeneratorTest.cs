/*
 * File: ProductKeyGeneratorTest.cs
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

public class ProductKeyGeneratorTest {

    [Theory]
    [InlineData("https://www.amazon.com.br/dp/B0BX4KQ3Z5?tag=lobao-20", "amazon:B0BX4KQ3Z5")]
    [InlineData("https://www.amazon.com.br/Monitor-Gamer/dp/b0bx4kq3z5/ref=sr_1_1", "amazon:B0BX4KQ3Z5")]
    [InlineData("amazon.com.br/gp/product/B08N5WRWNW", "amazon:B08N5WRWNW")]
    [InlineData("https://produto.mercadolivre.com.br/MLB-1234567890-mouse-logitech-_JM", "mlb:1234567890")]
    [InlineData("https://www.mercadolivre.com.br/mouse-gamer/p/MLB19876543", "mlb:19876543")]
    [InlineData("https://shopee.com.br/Fone-Bluetooth-i.123456.7890123", "shopee:123456.7890123")]
    [InlineData("https://shopee.com.br/product/123456/7890123?smtt=0", "shopee:123456.7890123")]
    [InlineData("https://www.magazineluiza.com.br/air-fryer/p/237654800/ep/epfr/", "magalu:237654800")]
    [InlineData("https://www.kabum.com.br/produto/123456/ssd-kingston", "kabum:123456")]
    [InlineData("https://pt.aliexpress.com/item/1005001234567890.html", "aliexpress:1005001234567890")]
    public void FromLink_ShouldUseTheIdOfTheStore(string link, string expected) {
        Assert.Equal(expected, ProductKeyGenerator.FromLink(link));
    }

    [Theory]
    [InlineData("https://amzn.to/3abcDEF")]
    [InlineData("https://notamazon.com/dp/B0BX4KQ3Z5")]
    [InlineData("https://www.amazon.com.br/s?k=rtx")]
    [InlineData("not a link")]
    [InlineData(null)]
    public void FromLink_ShouldBeNull_WithoutAKnownProduct(string? link) {
        Assert.Null(ProductKeyGenerator.FromLink(link));
    }

    [Fact]
    public void Generate_ShouldUseTheFirstLinkOfAKnownStore() {
        string key = ProductKeyGenerator.Generate(["https://t.me/canal", "https://www.kabum.com.br/produto/42"], "ssd kingston");

        Assert.Equal("kabum:42", key);
    }

    [Fact]
    public void Generate_ShouldHashTheText_WithoutAKnownLink() {
        string key = ProductKeyGenerator.Generate(["https://amzn.to/3abc"], "ssd kingston 1 tb");

        Assert.StartsWith(ProductKeyGenerator.TextKeyPrefix, key);
        Assert.Equal(ProductKeyGenerator.TextKeyPrefix.Length + 16, key.Length);
        Assert.Equal(key, ProductKeyGenerator.FromText("ssd kingston 1 tb"));
        Assert.NotEqual(key, ProductKeyGenerator.FromText("ssd kingston 2 tb"));
    }
}