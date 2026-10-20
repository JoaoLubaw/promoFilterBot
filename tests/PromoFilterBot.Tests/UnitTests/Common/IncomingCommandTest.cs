/*
 * File: IncomingCommandTest.cs
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

public class IncomingCommandTest {

    [Theory]
    [InlineData("/adicionar rtx 4060 max 2000", "adicionar", "rtx 4060 max 2000")]
    [InlineData("/alertas@PromoFilterBot", "alertas", "")]
    [InlineData("  /Pausar   8h ", "pausar", "8h")]
    [InlineData("/apagarConta", "apagarconta", "")]
    [InlineData("/opções", "opcoes", "")]
    public void Parse_ShouldReadTheCommandAndItsArguments(string text, string name, string arguments) {
        IncomingCommand? command = IncomingCommand.Parse(text);

        Assert.Equal(name, command!.Name);
        Assert.Equal(arguments, command.Arguments);
    }

    [Theory]
    [InlineData("rtx 4060")]
    [InlineData("/")]
    [InlineData("")]
    [InlineData(null)]
    public void Parse_ShouldBeNull_ForWhatIsNotACommand(string? text) {
        Assert.Null(IncomingCommand.Parse(text));
    }
}