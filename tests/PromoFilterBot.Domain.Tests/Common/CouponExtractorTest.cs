/*
 * File: CouponExtractorTest.cs
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

public class CouponExtractorTest {
    [Theory]
    [InlineData("🎟️ Cupom: TECH10", "TECH10")]
    [InlineData("Use o cupom PRIMEIRA10 e ganhe 10% OFF", "PRIMEIRA10")]
    [InlineData("Cupom de R$ 20 OFF: MELI20", "MELI20")]
    [InlineData("CUPOM: 15OFF", "15OFF")]
    [InlineData("cupom: vale20", "VALE20")]
    [InlineData("Código BEMVINDO na primeira compra", "BEMVINDO")]
    [InlineData("𝗖𝘂𝗽𝗼𝗺: 𝗧𝗘𝗖𝗛𝟭𝟬", "TECH10")]
    public void Extract_ShouldFindTheCode_OnTheLineOfTheCoupon(string text, string expected) {
        Assert.Equal([expected], CouponExtractor.Extract(text));
    }

    [Fact]
    public void Extract_ShouldLookAtTheNextLine_WhenTheCouponLineHasNoCode() {
        Assert.Equal(["MELI50"], CouponExtractor.Extract("SSD 1TB por R$ 399\nCupom de desconto:\nMELI50"));
    }

    [Theory]
    [InlineData("Cupom na página")]
    [InlineData("🔥 CUPOM DE 10% OFF 🔥")]
    [InlineData("CUPOM ATIVADO NA PÁGINA")]
    [InlineData("Cupom: https://amzn.to/abc123")]
    [InlineData("RTX 4060 por R$ 1.899 em 10x")]
    [InlineData("")]
    public void Extract_ShouldFindNothing_WithoutACode(string text) {
        Assert.Empty(CouponExtractor.Extract(text));
    }

    [Fact]
    public void Extract_ShouldReturnTheCodesInOrder_WithoutRepeating() {
        string text = "Cupom: ZETA5 ou ALFA10\nCupom: ALFA10";

        Assert.Equal(["ALFA10", "ZETA5"], CouponExtractor.Extract(text));
        Assert.Equal("ALFA10,ZETA5", CouponExtractor.ToKey(CouponExtractor.Extract(text)));
    }

    [Fact]
    public void Extract_ShouldIgnoreTheWordsOfTheOfferThatComeInCapitals() {
        Assert.Equal(["BF20"], CouponExtractor.Extract("CUPOM BLACK FRIDAY: BF20"));
    }
}