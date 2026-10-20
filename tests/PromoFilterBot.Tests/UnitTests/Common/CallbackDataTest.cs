/*
 * File: CallbackDataTest.cs
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

namespace PromoFilterBot.Tests.Common;

public class CallbackDataTest {

    [Fact]
    public void Build_ShouldBeReadBackByParse() {
        string data = Callbacks.Build(Callbacks.Alert, Callbacks.AlertDelete, 42);

        CallbackData? parsed = CallbackData.Parse(data);

        Assert.Equal("a:del:42", data);
        Assert.Equal(new CallbackData("a", "del", "42"), parsed);
        Assert.Equal(42, parsed!.IntArgument);
        Assert.Equal(42L, parsed.LongArgument);
    }

    [Fact]
    public void Parse_ShouldReadAnActionWithoutArgument() {
        CallbackData? parsed = CallbackData.Parse("m:new");

        Assert.Equal("m", parsed!.Prefix);
        Assert.Equal("new", parsed.Action);
        Assert.Null(parsed.Argument);
        Assert.Null(parsed.IntArgument);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("semdoispontos")]
    [InlineData(":new")]
    [InlineData("m:")]
    public void Parse_ShouldBeNull_ForUnknownData(string? data) {
        Assert.Null(CallbackData.Parse(data));
    }

    [Fact]
    public void ToString_ShouldRefuseDataLongerThanTelegramAllows() {
        Assert.Throws<InvalidOperationException>(() => new CallbackData("a", "b", new string('x', 70)).ToString());
    }
}