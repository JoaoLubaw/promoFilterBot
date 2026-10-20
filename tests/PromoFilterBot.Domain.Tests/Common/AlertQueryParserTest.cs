/*
 * File: AlertQueryParserTest.cs
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

public class AlertQueryParserTest {

    [Fact]
    public void Parse_ShouldRequireEveryWord_InAnyOrder() {
        AlertQuery query = AlertQueryParser.Parse("rtx 4060");

        Assert.Equal(["rtx", "4060"], query.RequiredTerms.Select(t => t.Text));
        Assert.Empty(query.ExcludedTerms);
        Assert.True(query.IsValid);
    }

    [Fact]
    public void Parse_ShouldReadTheQuotedTextAsAPhrase() {
        AlertQuery query = AlertQueryParser.Parse("\"air fryer\"");

        AlertTerm term = Assert.Single(query.RequiredTerms);
        Assert.Equal("air fryer", term.Text);
        Assert.True(term.IsPhrase);
    }

    [Fact]
    public void Parse_ShouldReadTheCurlyQuotesOfThePhones() {
        AlertQuery query = AlertQueryParser.Parse("“air fryer” -“usado”");

        Assert.Equal(["air fryer"], query.RequiredTerms.Select(t => t.Text));
        Assert.Equal(["usado"], query.ExcludedTerms.Select(t => t.Text));
    }

    [Fact]
    public void Parse_ShouldExcludeTheWordsWithAMinus_AndKeepASplitWordTogether() {
        AlertQuery query = AlertQueryParser.Parse("ssd 1tb -sata");

        Assert.Equal(["ssd", "1 tb"], query.RequiredTerms.Select(t => t.Text));
        Assert.Equal(["sata"], query.ExcludedTerms.Select(t => t.Text));
        Assert.True(query.RequiredTerms[1].IsPhrase);
    }

    [Fact]
    public void Parse_ShouldIgnoreTheConnectives_OutsideAPhrase() {
        Assert.Equal(["cadeira", "escritorio"], AlertQueryParser.Parse("cadeira de escritório").RequiredTerms.Select(t => t.Text));
        Assert.Equal(["cadeira de escritorio"], AlertQueryParser.Parse("\"cadeira de escritório\"").RequiredTerms.Select(t => t.Text));
    }

    [Fact]
    public void Parse_ShouldKeepEachTermOnce() {
        Assert.Single(AlertQueryParser.Parse("RTX rtx Rtx").RequiredTerms);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("🔥🔥")]
    [InlineData("de a para")]
    [InlineData("-usado")]
    public void Parse_ShouldBeInvalid_WithoutATermToLookFor(string? input) {
        Assert.False(AlertQueryParser.Parse(input).IsValid);
    }

    [Fact]
    public void StoredTerms_ShouldBeReadBackAsTheSameQuery() {
        AlertQuery query = AlertQueryParser.Parse("ssd 1tb -sata -\"usado ok\"");

        AlertQuery stored = AlertQuery.FromStored(query.StoredRequiredTerms, query.StoredExcludedTerms);

        Assert.Equal("ssd|1 tb", query.StoredRequiredTerms);
        Assert.Equal("sata|usado ok", query.StoredExcludedTerms);
        Assert.Equal(query.RequiredTerms, stored.RequiredTerms);
        Assert.Equal(query.ExcludedTerms, stored.ExcludedTerms);
    }

    [Fact]
    public void Describe_ShouldShowTheTermsAsTheMatcherReadsThem() {
        Assert.Equal("ssd · \"1 tb\" · sem: sata", AlertQueryParser.Parse("ssd 1tb -sata").Describe());
        Assert.Equal("rtx · 4060", AlertQueryParser.Parse("rtx 4060").Describe());
    }
}