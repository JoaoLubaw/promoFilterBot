/*
 * File: NotificationFormatter.cs
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

using System.Text;

using PromoFilterBot.Infrastructure.Dto;

namespace PromoFilterBot.Bot.Common;

/// <summary>
/// The message of an offer: a copy of the post as the channel published it (formatting, coupon and links with the
/// affiliate code of the channel), under the name of the channel, which opens the original post.
/// </summary>
public static class NotificationFormatter {
    // Telegram accepts 4096 characters; the reader keeps 3500 of the post, and the rest fits in what is left
    private const int MaxPlainTextLength = 3500;

    public static string Format(NotificationDispatchDto notification) {
        string body = string.IsNullOrWhiteSpace(notification.OfferHtml) ? PlainBody(notification) : notification.OfferHtml.Trim();

        return Build(notification, body);
    }

    /// <summary>
    /// The same message without the formatting of the post, for when Telegram does not accept it.
    /// </summary>
    public static string FormatPlain(NotificationDispatchDto notification) => Build(notification, PlainBody(notification));

    private static string Build(NotificationDispatchDto notification, string body) {
        StringBuilder message = new StringBuilder()
            .AppendLine($"📢 <b>{ChannelName(notification)}</b>")
            .AppendLine()
            .AppendLine(body)
            .AppendLine()
            .Append($"🔔 Seu alerta: <b>{Html.Escape(notification.AlertText)}</b> · {Formatters.MaxPrice(notification.AlertMaxPrice)}");

        if (notification.PriceUnknown) message.AppendLine().Append("⚠️ Não consegui ler o preço desta oferta; confira antes de comprar.");

        return message.ToString();
    }

    private static string PlainBody(NotificationDispatchDto notification) {
        return Html.Escape(Html.Truncate(notification.OfferText.Trim(), MaxPlainTextLength));
    }

    // The name of the channel opens the original post
    private static string ChannelName(NotificationDispatchDto notification) {
        string name = !string.IsNullOrWhiteSpace(notification.ChannelTitle)
            ? notification.ChannelTitle
            : string.IsNullOrEmpty(notification.ChannelUsername) ? "Canal de ofertas" : $"@{notification.ChannelUsername}";

        string escaped = Html.Escape(name);

        return notification.PostLink == null ? escaped : $"<a href=\"{Html.EscapeAttribute(notification.PostLink)}\">{escaped}</a>";
    }
}