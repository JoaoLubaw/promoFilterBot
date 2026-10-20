/*
 * File: ReaderMessageMapperTest.cs
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

using PromoFilterBot.Domain.Dto;
using PromoFilterBot.Reader.Common;
using PromoFilterBot.Tests.Helpers;

using TL;

namespace PromoFilterBot.Tests.Common;

public class ReaderMessageMapperTest {

    [Fact]
    public void Map_ShouldTakeTheTextAndEveryLinkOfThePost() {
        // "🔥" takes two UTF-16 units, as in the offsets of Telegram
        Message message = new Message {
            id = 10,
            message = "🔥 Oferta aqui https://amzn.to/abc",
            entities = [
                new MessageEntityTextUrl { offset = 3, length = 6, url = "https://www.kabum.com.br/produto/1" },
                new MessageEntityUrl { offset = 15, length = 19 },
                new MessageEntityUrl { offset = 30, length = 50 }
            ],
            media = new MessageMediaWebPage { webpage = new WebPage { url = "https://amzn.to/abc" } }
        };

        ReceivedOfferDto? offer = ReaderMessageMapper.Map(message, 777, TestData.Now);

        Assert.NotNull(offer);
        Assert.Equal(777, offer.TelegramChannelId);
        Assert.Equal(10, offer.TelegramMessageId);
        Assert.Equal(message.message, offer.Text);
        Assert.Equal(["https://www.kabum.com.br/produto/1", "https://amzn.to/abc"], offer.Links);
        Assert.Equal(TestData.Now, offer.ReceivedAt);
    }

    [Fact]
    public void Map_ShouldKeepTheFormattingOfThePost_AsHtml() {
        Message message = new Message {
            id = 10,
            message = "Air Fryer R$ 299\nCupom: TECH10\nComprar",
            entities = [
                new MessageEntityBold { offset = 0, length = 9 },
                new MessageEntityCode { offset = 24, length = 6 },
                new MessageEntityTextUrl { offset = 31, length = 7, url = "https://amzn.to/abc" },
                new MessageEntityMention { offset = 0, length = 3 }
            ]
        };

        ReceivedOfferDto? offer = ReaderMessageMapper.Map(message, 777, TestData.Now);

        Assert.Equal("<b>Air Fryer</b> R$ 299\nCupom: <code>TECH10</code>\n<a href=\"https://amzn.to/abc\">Comprar</a>", offer!.Html);
    }

    [Fact]
    public void Map_ShouldBeNull_ForAPostWithoutText() {
        Assert.Null(ReaderMessageMapper.Map(new Message { id = 1, message = "  " }, 777, TestData.Now));
    }
}