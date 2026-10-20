/*
 * File: TextNormalizerTest.cs
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

public class TextNormalizerTest {

    [Theory]
    [InlineData("RTX4060 Ti", "rtx 4060 ti")]
    [InlineData("rtx 4060", "rtx 4060")]
    [InlineData("Fritadeira Elétrica Air Fryer 🔥🔥", "fritadeira eletrica air fryer")]
    [InlineData("Ação, promoção!!!", "acao promocao")]
    [InlineData("  muitos     espaços  ", "muitos espacos")]
    [InlineData("R$ 1.299,90", "r 1 299 90")]
    [InlineData("i5-12400F", "i 5 12400 f")]
    [InlineData("wi-fi", "wi fi")]
    [InlineData("SSD 1TB", "ssd 1 tb")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData(null, "")]
    public void Normalize_ShouldPutTheTextInTheFormTheMatcherCompares(string? text, string expected) {
        Assert.Equal(expected, TextNormalizer.Normalize(text));
    }

    [Fact]
    public void Normalize_ShouldTurnTheStyledLettersOfTheChannelsIntoPlainOnes() {
        // 𝗥𝗧𝗫 𝟰𝟬𝟲𝟬 (mathematical sans-serif bold) and ｒｔｘ (full width)
        string styled = "\U0001D5E5\U0001D5E7\U0001D5EB \U0001D7F0\U0001D7EC\U0001D7F2\U0001D7EC";

        Assert.Equal("rtx 4060", TextNormalizer.Normalize(styled));
        Assert.Equal("rtx", TextNormalizer.Normalize("ｒｔｘ"));
    }

    [Fact]
    public void Tokenize_ShouldSplitTheNormalizedWords() {
        Assert.Equal(["mouse", "logitech", "g", "305"], TextNormalizer.Tokenize("Mouse Logitech G305!"));
        Assert.Empty(TextNormalizer.Tokenize("🔥"));
    }

    [Fact]
    public void Fold_ShouldKeepThePunctuation() {
        Assert.Equal("por: r$ 1.299,90 a vista", TextNormalizer.Fold("POR: R$ 1.299,90 À vista"));
        Assert.Equal(string.Empty, TextNormalizer.Fold(null));
    }
}