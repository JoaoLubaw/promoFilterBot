/*
 * File: ChannelUsernameParserTest.cs
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

public class ChannelUsernameParserTest {

    [Theory]
    [InlineData("@Lobao_Ofertas", "lobao_ofertas")]
    [InlineData("lobaoofertas", "lobaoofertas")]
    [InlineData("https://t.me/lobaoofertas", "lobaoofertas")]
    [InlineData("t.me/s/achados", "achados")]
    [InlineData("t.me/achados/1234", "achados")]
    [InlineData("http://telegram.me/OfertasGamers?start=1", "ofertasgamers")]
    public void Parse_ShouldReadTheUsernameOfThePublicChannel(string input, string expected) {
        ChannelReference reference = ChannelUsernameParser.Parse(input);

        Assert.Equal(expected, reference.Username);
        Assert.False(reference.IsPrivateLink);
    }

    [Theory]
    [InlineData("https://t.me/+AbCdEf123")]
    [InlineData("t.me/joinchat/xyz")]
    public void Parse_ShouldFlagTheInviteLinksOfPrivateChannels(string input) {
        ChannelReference reference = ChannelUsernameParser.Parse(input);

        Assert.True(reference.IsPrivateLink);
        Assert.Null(reference.Username);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("1canal")]
    [InlineData("canal com espaço")]
    [InlineData("")]
    [InlineData(null)]
    public void Parse_ShouldRefuseWhatIsNotAUsername(string? input) {
        Assert.Null(ChannelUsernameParser.Parse(input).Username);
    }
}