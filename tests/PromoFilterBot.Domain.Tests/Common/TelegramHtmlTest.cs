/*
 * File: TelegramHtmlTest.cs
 * Project: Common
 * Create Date: Tuesday, 20th October 2026
 * Author: João Lubawaski (joaolubawaski@gmail.com)
 * -----
 * Last Modified: Tuesday, 20th October 2026 7:00:00 pm
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

public class TelegramHtmlTest {
    [Fact]
    public void FromEntities_ShouldEscapeTheText_WithoutEntities() {
        Assert.Equal("Fone &lt;JBL&gt; &amp; capa", TelegramHtml.FromEntities("Fone <JBL> & capa", null));
    }

    [Fact]
    public void FromEntities_ShouldKeepTheHiddenLinkAndTheCouponInMonospace() {
        string text = "🔥 Air Fryer por R$ 299\nCupom: TECH10\nComprar aqui";
        int coupon = text.IndexOf("TECH10", StringComparison.Ordinal);
        int link = text.IndexOf("Comprar aqui", StringComparison.Ordinal);

        string html = TelegramHtml.FromEntities(text, [
            new TextEntity(TextEntityType.Bold, 0, text.IndexOf('\n')),
            new TextEntity(TextEntityType.Code, coupon, 6),
            new TextEntity(TextEntityType.TextLink, link, 12, Url: "https://amzn.to/abc?tag=canal-20&x=1")
        ]);

        Assert.Equal(
            "<b>🔥 Air Fryer por R$ 299</b>\nCupom: <code>TECH10</code>\n<a href=\"https://amzn.to/abc?tag=canal-20&amp;x=1\">Comprar aqui</a>",
            html);
    }

    [Fact]
    public void FromEntities_ShouldNestTheTags_OfEntitiesInsideOthers() {
        string html = TelegramHtml.FromEntities("SSD 1TB barato", [
            new TextEntity(TextEntityType.Bold, 0, 14),
            new TextEntity(TextEntityType.Italic, 4, 3)
        ]);

        Assert.Equal("<b>SSD <i>1TB</i> barato</b>", html);
    }

    [Fact]
    public void FromEntities_ShouldCloseAndReopen_EntitiesThatOverlap() {
        string html = TelegramHtml.FromEntities("abcdef", [
            new TextEntity(TextEntityType.Bold, 0, 4),
            new TextEntity(TextEntityType.Italic, 2, 4)
        ]);

        Assert.Equal("<b>ab<i>cd</i></b><i>ef</i>", html);
    }

    [Fact]
    public void FromEntities_ShouldIgnoreEntitiesOutsideTheTextAndLinksWithoutUrl() {
        string html = TelegramHtml.FromEntities("oferta", [
            new TextEntity(TextEntityType.Bold, 10, 3),
            new TextEntity(TextEntityType.Bold, 0, 0),
            new TextEntity(TextEntityType.TextLink, 0, 6, Url: " ")
        ]);

        Assert.Equal("oferta", html);
    }

    [Fact]
    public void FromEntities_ShouldCutALongText_AndTheEntitiesThatPassTheCut() {
        string html = TelegramHtml.FromEntities("Monitor gamer 27 polegadas", [new TextEntity(TextEntityType.Bold, 8, 18)], maxLength: 13);

        Assert.Equal("Monitor <b>gamer</b>…", html);
    }

    [Fact]
    public void FromEntities_ShouldWriteTheLanguageOfACodeBlock() {
        string html = TelegramHtml.FromEntities("x = 1", [new TextEntity(TextEntityType.Pre, 0, 5, Language: "python")]);

        Assert.Equal("<pre><code class=\"language-python\">x = 1</code></pre>", html);
    }
}