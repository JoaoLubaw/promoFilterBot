/*
 * File: NotificationFormatterTest.cs
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
using PromoFilterBot.Infrastructure.Dto;

using Telegram.Bot.Types.ReplyMarkups;

namespace PromoFilterBot.Tests.Common;

public class NotificationFormatterTest {
    private static NotificationDispatchDto Notification(bool priceUnknown = false, string? offerLink = "https://amzn.to/3abc", string? html = null) {
        return new NotificationDispatchDto {
            NotificationId = 900,
            ChatId = 1001,
            AlertId = 10,
            AlertText = "rtx 4060",
            AlertMaxPrice = 2000,
            OfferText = "<b>RTX 4060</b> por R$ 1.899 & frete grátis\nCupom: TECH10",
            OfferHtml = html,
            OfferLink = offerLink,
            PostLink = "https://t.me/lobao/55",
            ChannelUsername = "lobao",
            ChannelTitle = "Lobão & Ofertas",
            Price = priceUnknown ? null : 1899,
            PriceUnknown = priceUnknown
        };
    }

    [Fact]
    public void Format_ShouldCopyThePostWithItsFormatting_UnderTheNameOfTheChannel() {
        string message = NotificationFormatter.Format(Notification(html: "<b>RTX 4060</b> por R$ 1.899\nCupom: <code>TECH10</code>"));

        Assert.StartsWith("📢 <b><a href=\"https://t.me/lobao/55\">Lobão &amp; Ofertas</a></b>", message);
        Assert.Contains("<b>RTX 4060</b> por R$ 1.899\nCupom: <code>TECH10</code>", message);
        Assert.Contains("Seu alerta: <b>rtx 4060</b> · até R$ 2.000,00", message);
        Assert.DoesNotContain("Não consegui ler o preço", message);
    }

    [Fact]
    public void Format_ShouldEscapeTheText_WithoutTheFormattedCopy() {
        string message = NotificationFormatter.Format(Notification());

        Assert.Contains("&lt;b&gt;RTX 4060&lt;/b&gt; por R$ 1.899 &amp; frete grátis\nCupom: TECH10", message);
    }

    [Fact]
    public void FormatPlain_ShouldLeaveTheFormattingOut() {
        string message = NotificationFormatter.FormatPlain(Notification(html: "<b>RTX 4060</b>"));

        Assert.DoesNotContain("<b>RTX 4060</b>", message);
        Assert.Contains("&lt;b&gt;RTX 4060&lt;/b&gt;", message);
    }

    [Fact]
    public void Format_ShouldUseTheUsername_WithoutATitle() {
        NotificationDispatchDto notification = Notification();
        notification.ChannelTitle = null;
        notification.PostLink = null;

        Assert.StartsWith("📢 <b>@lobao</b>", NotificationFormatter.Format(notification));
    }

    [Fact]
    public void Format_ShouldSayWhenThePriceWasNotIdentified() {
        string message = NotificationFormatter.Format(Notification(priceUnknown: true));

        Assert.Contains("Não consegui ler o preço desta oferta", message);
    }

    [Fact]
    public void Format_ShouldFitInATelegramMessage() {
        NotificationDispatchDto notification = Notification();
        notification.OfferText = new string('a', 10_000);

        Assert.True(NotificationFormatter.Format(notification).Length < 4096);
    }

    [Fact]
    public void NotificationKeyboard_ShouldOpenTheOfferAndThePost_AndTurnTheAlertOff() {
        InlineKeyboardMarkup keyboard = Keyboards.Notification(Notification());
        InlineKeyboardButton[] links = keyboard.InlineKeyboard.First().ToArray();
        InlineKeyboardButton[] actions = keyboard.InlineKeyboard.Last().ToArray();

        Assert.Equal(["https://amzn.to/3abc", "https://t.me/lobao/55"], links.Select(b => b.Url));
        Assert.Equal("n:off:10", actions[0].CallbackData);
        Assert.Equal("n:fb:900", actions[1].CallbackData);
    }

    [Fact]
    public void NotificationKeyboard_ShouldOpenOnlyThePost_WithoutALink() {
        InlineKeyboardButton withPost = Keyboards.Notification(Notification(offerLink: null)).InlineKeyboard.First().Single();

        Assert.Equal("https://t.me/lobao/55", withPost.Url);
    }

    [Fact]
    public void Keyboards_ShouldKeepTheCallbackDataWithinTheLimit_EvenWithLargeIds() {
        NotificationDispatchDto notification = Notification();
        notification.NotificationId = long.MaxValue;
        notification.AlertId = int.MaxValue;

        IEnumerable<InlineKeyboardButton> buttons = Keyboards.Notification(notification).InlineKeyboard.SelectMany(row => row)
            .Concat(Keyboards.EditAlert(int.MaxValue).InlineKeyboard.SelectMany(row => row))
            .Concat(Keyboards.MainMenu().InlineKeyboard.SelectMany(row => row))
            .Concat(Keyboards.AdminMenu().InlineKeyboard.SelectMany(row => row));

        Assert.All(buttons.Where(b => b.CallbackData != null), b => Assert.True(b.CallbackData!.Length <= CallbackData.MaxLength));
    }
}