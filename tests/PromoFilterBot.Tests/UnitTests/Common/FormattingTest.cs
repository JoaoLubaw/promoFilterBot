/*
 * File: FormattingTest.cs
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

using PromoFilterBot.Bot.Common;
using PromoFilterBot.Tests.Helpers;

namespace PromoFilterBot.Tests.Common;

public class FormattingTest {

    [Theory]
    [InlineData(1299.9, "R$ 1.299,90")]
    [InlineData(1000000, "R$ 1.000.000,00")]
    [InlineData(99, "R$ 99,00")]
    public void Price_ShouldBeWrittenAsInBrazil(double value, string expected) {
        Assert.Equal(expected, Formatters.Price((decimal)value));
    }

    [Fact]
    public void MaxPrice_ShouldSayWhenThereIsNoLimit() {
        Assert.Equal("sem limite", Formatters.MaxPrice(null));
        Assert.Equal("até R$ 2.000,00", Formatters.MaxPrice(2000));
    }

    [Fact]
    public void LocalDateTime_ShouldBeInTheTimeZoneOfTheUsers() {
        TimeZoneInfo brasilia = TimeZoneInfo.CreateCustomTimeZone("UTC-03", TimeSpan.FromHours(-3), "UTC-03", "UTC-03");

        Assert.Equal("20/10 às 12:00", Formatters.LocalDateTime(TestData.Now, brasilia));
    }

    [Fact]
    public void Escape_ShouldEscapeWhatTelegramReadsAsHtml() {
        Assert.Equal("&lt;b&gt;Tom &amp; Jerry&lt;/b&gt;", Html.Escape("<b>Tom & Jerry</b>"));
        Assert.Equal("&quot;a&quot;", Html.EscapeAttribute("\"a\""));
        Assert.Equal(string.Empty, Html.Escape(null));
    }

    [Fact]
    public void Truncate_ShouldNotSplitAnEmoji() {
        string truncated = Html.Truncate("abc😀def", 5);

        Assert.Equal("abc…", truncated);
        Assert.Equal("curto", Html.Truncate("curto", 10));
    }

    [Fact]
    public void Truncate_ShouldKeepAShortText() {
        Assert.Equal("RTX 4060", Html.Truncate("RTX 4060", 80));
    }
}