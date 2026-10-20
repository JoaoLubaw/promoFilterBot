/*
 * File: PauseDurationParserTest.cs
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

public class PauseDurationParserTest {
    private static readonly TimeZoneInfo Brasilia = TimeZoneInfo.CreateCustomTimeZone("UTC-03", TimeSpan.FromHours(-3), "UTC-03", "UTC-03");

    [Theory]
    [InlineData("8h", 8 * 60)]
    [InlineData("1 hora", 60)]
    [InlineData("30m", 30)]
    [InlineData("30min", 30)]
    [InlineData("2 dias", 2 * 24 * 60)]
    public void Parse_ShouldReadADuration(string input, int minutes) {
        PauseRequest? request = PauseDurationParser.Parse(input);

        Assert.NotNull(request);
        Assert.Equal(PauseKind.Duration, request.Kind);
        Assert.Equal(TimeSpan.FromMinutes(minutes), request.Duration);
    }

    [Theory]
    [InlineData("amanhã")]
    [InlineData("até amanhã")]
    [InlineData("amanha")]
    public void Parse_ShouldReadUntilTomorrow(string input) {
        Assert.Equal(PauseKind.UntilTomorrow, PauseDurationParser.Parse(input)!.Kind);
    }

    [Theory]
    [InlineData("retomar")]
    [InlineData("Voltar")]
    public void Parse_ShouldReadTheResume(string input) {
        Assert.Equal(PauseKind.Resume, PauseDurationParser.Parse(input)!.Kind);
    }

    [Theory]
    [InlineData("31d")]
    [InlineData("0h")]
    [InlineData("1h30")]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData(null)]
    public void Parse_ShouldBeNull_ForWhatIsNotADuration(string? input) {
        Assert.Null(PauseDurationParser.Parse(input));
    }

    [Fact]
    public void ResolveUntil_ShouldBeTheNextMorning_InTheTimeZoneOfTheUsers() {
        DateTime nowUtc = new DateTime(2026, 10, 20, 15, 0, 0, DateTimeKind.Utc);

        DateTime? until = PauseRequest.UntilTomorrow.ResolveUntil(nowUtc, Brasilia);

        Assert.Equal(new DateTime(2026, 10, 21, 11, 0, 0, DateTimeKind.Utc), until);
    }

    [Fact]
    public void ResolveUntil_ShouldAddTheDuration_OrResume() {
        DateTime nowUtc = new DateTime(2026, 10, 20, 15, 0, 0, DateTimeKind.Utc);

        Assert.Equal(nowUtc.AddHours(8), PauseRequest.For(TimeSpan.FromHours(8)).ResolveUntil(nowUtc, Brasilia));
        Assert.Null(PauseRequest.Resume.ResolveUntil(nowUtc, Brasilia));
    }
}