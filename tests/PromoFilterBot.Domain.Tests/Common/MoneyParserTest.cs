/*
 * File: MoneyParserTest.cs
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

public class MoneyParserTest {

    [Theory]
    [InlineData("1.299,90", 1299.90)]
    [InlineData("1299,90", 1299.90)]
    [InlineData("1.299", 1299)]
    [InlineData("1299.90", 1299.90)]
    [InlineData("R$ 2.000", 2000)]
    [InlineData("r$1.499,9", 1499.9)]
    [InlineData("2000", 2000)]
    [InlineData("1,5", 1.5)]
    [InlineData("1.000.000", 1000000)]
    [InlineData("R$ 99,90.", 99.90)]
    public void TryParse_ShouldReadTheAmountAsPeopleWriteIt(string text, double expected) {
        Assert.True(MoneyParser.TryParse(text, out decimal value));
        Assert.Equal((decimal)expected, value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("abc")]
    [InlineData("1.2.3")]
    [InlineData("12,345")]
    [InlineData("-10")]
    public void TryParse_ShouldRefuseWhatIsNotAnAmount(string? text) {
        Assert.False(MoneyParser.TryParse(text, out _));
    }
}