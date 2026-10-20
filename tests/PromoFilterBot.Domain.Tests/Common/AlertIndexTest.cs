/*
 * File: AlertIndexTest.cs
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

public class AlertIndexTest {
    private static IndexedAlert Alert(int alertId, string search) {
        return new IndexedAlert { AlertId = alertId, UserId = alertId, Query = AlertQueryParser.Parse(search) };
    }

    [Fact]
    public void Build_ShouldFileEachAlertUnderItsRarestWord() {
        AlertIndex index = AlertIndex.Build([Alert(1, "rtx 4060"), Alert(2, "rtx 4070")]);

        Assert.Equal([1], index.FindCandidates(OfferText.FromRawText("rtx 4060")).Select(a => a.AlertId));
        Assert.Equal([2], index.FindCandidates(OfferText.FromRawText("rtx 4070")).Select(a => a.AlertId));
        // "rtx" alone is in both, but it is not the anchor of either
        Assert.Empty(index.FindCandidates(OfferText.FromRawText("rtx 3060")));
    }

    [Fact]
    public void ChooseAnchor_ShouldPreferTheWordWithDigitsAndThenTheLonger_OnATie() {
        Dictionary<string, int> frequency = new() { ["mouse"] = 1, ["g"] = 1, ["305"] = 1 };

        Assert.Equal("305", AlertIndex.ChooseAnchor(["mouse", "g", "305"], frequency));
        Assert.Equal("logitech", AlertIndex.ChooseAnchor(["mouse", "logitech"], new Dictionary<string, int> { ["mouse"] = 1, ["logitech"] = 1 }));
        Assert.Equal("mouse", AlertIndex.ChooseAnchor(["mouse", "305"], new Dictionary<string, int> { ["mouse"] = 1, ["305"] = 4 }));
    }

    [Fact]
    public void Build_ShouldLeaveOutTheAlertsWithoutTerms() {
        AlertIndex index = AlertIndex.Build([Alert(1, "de a"), Alert(2, "air fryer")]);

        Assert.Equal(1, index.Count);
        Assert.Equal(0, AlertIndex.Empty.Count);
    }

    [Fact]
    public void FindCandidates_ShouldFindAPhraseAlertByAWordOfIt() {
        AlertIndex index = AlertIndex.Build([Alert(1, "\"air fryer\"")]);

        Assert.Single(index.FindCandidates(OfferText.FromRawText("Air Fryer Mondial")));
    }
}