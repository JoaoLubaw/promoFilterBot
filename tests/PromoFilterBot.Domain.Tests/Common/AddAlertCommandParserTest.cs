/*
 * File: AddAlertCommandParserTest.cs
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
using PromoFilterBot.Domain.Dto;

namespace PromoFilterBot.Domain.Tests.Common;

public class AddAlertCommandParserTest {

    [Theory]
    [InlineData("rtx 4060 max 2000", "rtx 4060", 2000)]
    [InlineData("Monitor MAX 1500", "Monitor", 1500)]
    [InlineData("air fryer até R$ 350,00", "air fryer", 350)]
    [InlineData("ssd 1tb máximo: 1.299,90", "ssd 1tb", 1299.90)]
    public void Parse_ShouldSplitTheSearchAndTheLimit(string input, string text, double maxPrice) {
        AddAlertCommand command = AddAlertCommandParser.Parse(input);

        Assert.Equal(text, command.Text);
        Assert.Equal((decimal)maxPrice, command.MaxPrice);
        Assert.False(command.HasInvalidMaxPrice);
    }

    [Theory]
    [InlineData("rtx 4060")]
    [InlineData("iPhone 15 Pro Max")]
    [InlineData("iPhone 15 Pro Max 256gb")]
    public void Parse_ShouldKeepTheWholeText_WithoutALimit(string input) {
        AddAlertCommand command = AddAlertCommandParser.Parse(input);

        Assert.Equal(input, command.Text);
        Assert.Null(command.MaxPrice);
    }

    [Fact]
    public void Parse_ShouldFlagALimitThatIsNotAnAmount() {
        AddAlertCommand command = AddAlertCommandParser.Parse("ssd max 1,2,3");

        Assert.Equal("ssd", command.Text);
        Assert.True(command.HasInvalidMaxPrice);
    }
}