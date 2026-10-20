/*
 * File: AlertMatcherTest.cs
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

public class AlertMatcherTest {
    private static IndexedAlert Alert(string search, decimal? maxPrice = null, int alertId = 1, int userId = 1) {
        return new IndexedAlert {
            AlertId = alertId,
            UserId = userId,
            Query = AlertQueryParser.Parse(search),
            MaxPrice = maxPrice
        };
    }

    [Fact]
    public void OfferText_ShouldFindAPhrase_OnlyWithItsWordsInOrder() {
        OfferText offer = OfferText.FromRawText("Compre agora: Air Fryer 4L");

        Assert.True(offer.Contains(new AlertTerm("air fryer")));
        Assert.False(offer.Contains(new AlertTerm("fryer air")));
        Assert.True(offer.Contains(new AlertTerm("4 l")));
        Assert.False(offer.Contains(new AlertTerm("fry")));
    }

    [Fact]
    public void Evaluate_ShouldMatch_WithEveryTermAndThePriceUnderTheLimit() {
        OfferText offer = OfferText.FromRawText("Placa de vídeo RTX 4060 Ti por R$ 1.899");

        AlertMatch? match = AlertMatcher.Evaluate(Alert("rtx 4060", 2000), offer, price: 1899, notifyWhenPriceUnknown: true);

        Assert.NotNull(match);
        Assert.False(match.PriceUnknown);
    }

    [Fact]
    public void Evaluate_ShouldNotMatch_AboveTheLimit() {
        OfferText offer = OfferText.FromRawText("RTX 4060 por R$ 2.100");

        Assert.Null(AlertMatcher.Evaluate(Alert("rtx 4060", 2000), offer, 2100, true));
    }

    [Fact]
    public void Evaluate_ShouldFlagTheMatch_WhenThePriceCouldNotBeRead() {
        OfferText offer = OfferText.FromRawText("RTX 4060 em promoção, confira");

        Assert.True(AlertMatcher.Evaluate(Alert("rtx 4060", 2000), offer, null, notifyWhenPriceUnknown: true)!.PriceUnknown);
        Assert.Null(AlertMatcher.Evaluate(Alert("rtx 4060", 2000), offer, null, notifyWhenPriceUnknown: false));
        Assert.False(AlertMatcher.Evaluate(Alert("rtx 4060"), offer, null, notifyWhenPriceUnknown: true)!.PriceUnknown);
    }

    [Fact]
    public void Evaluate_ShouldNotMatch_WithAnExcludedTermOrAMissingOne() {
        Assert.Null(AlertMatcher.Evaluate(Alert("rtx 4060 -ti"), OfferText.FromRawText("RTX 4060 Ti"), null, true));
        Assert.Null(AlertMatcher.Evaluate(Alert("rtx 4060"), OfferText.FromRawText("RTX 4070"), null, true));
        Assert.Null(AlertMatcher.Evaluate(Alert("ssd 1tb"), OfferText.FromRawText("SSD 1 de 2 TB"), null, true));
    }

    [Fact]
    public void FindMatches_ShouldCheckOnlyTheCandidatesOfTheIndex() {
        AlertIndex index = AlertIndex.Build([
            Alert("rtx 4060", alertId: 1),
            Alert("rtx 4070", alertId: 2),
            Alert("mouse g305", alertId: 3)
        ]);

        IReadOnlyList<AlertMatch> matches = AlertMatcher.FindMatches(index, OfferText.FromRawText("RTX 4060 Ti"), null, true);

        Assert.Equal([1], matches.Select(m => m.Alert.AlertId));
    }
}