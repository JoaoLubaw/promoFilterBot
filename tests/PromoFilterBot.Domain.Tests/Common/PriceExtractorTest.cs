/*
 * File: PriceExtractorTest.cs
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

public class PriceExtractorTest {

    [Theory]
    [InlineData("🔥 RTX 4060 de R$ 2.499 por R$ 1.899,90 no Pix", 1899.90)]
    [InlineData("Mouse Logitech G305 R$ 149,90", 149.90)]
    [InlineData("Por: R$ 89,90", 89.90)]
    [InlineData("SSD 1TB POR APENAS R$ 399", 399)]
    [InlineData("de R$ 1.999,00 por R$ 1.499,00 ou 12x de R$ 124,92", 1499)]
    [InlineData("R$ 1.299,90 ou R$ 999", 999)]
    public void Extract_ShouldReadThePriceOfTheOffer(string text, double expected) {
        Assert.Equal((decimal)expected, PriceExtractor.Extract(text));
    }

    [Theory]
    [InlineData("Notebook R$ 3.999 à vista ou 10x de R$ 449,90", 3999)]
    [InlineData("Monitor R$ 899 ou 12x sem juros de R$ 74,92", 899)]
    [InlineData("Cupom de R$ 50 OFF! Air Fryer por R$ 299", 299)]
    [InlineData("Economize R$ 300 — SSD R$ 399,90", 399.90)]
    [InlineData("Frete grátis acima de R$ 79. Fone R$ 59,90", 59.90)]
    [InlineData("Teclado R$ 199 + R$ 20 de cashback", 199)]
    public void Extract_ShouldLeaveOutTheValuesThatAreNotThePrice(string text, double expected) {
        Assert.Equal((decimal)expected, PriceExtractor.Extract(text));
    }

    [Theory]
    [InlineData("Sem preço aqui")]
    [InlineData("12x sem juros de R$ 99,90")]
    [InlineData("R$ 0,00")]
    [InlineData("")]
    [InlineData(null)]
    public void Extract_ShouldBeNull_WithoutAPrice(string? text) {
        Assert.Null(PriceExtractor.Extract(text));
    }
}