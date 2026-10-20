/*
 * File: ReaderMessageMapper.cs
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

using TL;

namespace PromoFilterBot.Reader.Common;

/// <summary>
/// Turns a post of a channel into an offer for the matcher: the text, its formatting as HTML (for the copy sent to the
/// users), and the links of the text, the ones hidden behind words and the one of the preview.
/// </summary>
public static class ReaderMessageMapper {
    /// <summary>
    /// Longest part of a post copied to the users; Telegram accepts 4096 characters, and the bot adds the channel and the
    /// alert around it.
    /// </summary>
    public const int MaxCopyLength = 3500;

    public static ReceivedOfferDto? Map(Message message, long channelId, DateTime receivedAt) {
        string text = message.message ?? string.Empty;

        if (string.IsNullOrWhiteSpace(text)) return null;

        List<string> links = [];
        List<TextEntity> entities = [];

        foreach (MessageEntity entity in message.entities ?? []) {
            switch (entity) {
                case MessageEntityTextUrl textUrl when !string.IsNullOrWhiteSpace(textUrl.url):
                    links.Add(textUrl.url);
                    break;
                // The offsets are in UTF-16 code units, the same as the C# string
                case MessageEntityUrl when entity.offset >= 0 && entity.offset + entity.length <= text.Length:
                    links.Add(text.Substring(entity.offset, entity.length));
                    break;
            }

            TextEntity? textEntity = ToTextEntity(entity);

            if (textEntity != null) entities.Add(textEntity);
        }

        if (message.media is MessageMediaWebPage { webpage: WebPage webPage } && !string.IsNullOrWhiteSpace(webPage.url)) {
            links.Add(webPage.url);
        }

        return new ReceivedOfferDto {
            TelegramChannelId = channelId,
            TelegramMessageId = message.id,
            Text = text,
            Html = TelegramHtml.FromEntities(text, entities, MaxCopyLength),
            Links = links.Distinct(StringComparer.Ordinal).ToList(),
            ReceivedAt = receivedAt
        };
    }

    // Mentions, hashtags, plain links and custom emojis stay as text: Telegram shows them by itself
    private static TextEntity? ToTextEntity(MessageEntity entity) => entity switch {
        MessageEntityBold => new TextEntity(TextEntityType.Bold, entity.offset, entity.length),
        MessageEntityItalic => new TextEntity(TextEntityType.Italic, entity.offset, entity.length),
        MessageEntityUnderline => new TextEntity(TextEntityType.Underline, entity.offset, entity.length),
        MessageEntityStrike => new TextEntity(TextEntityType.Strikethrough, entity.offset, entity.length),
        MessageEntitySpoiler => new TextEntity(TextEntityType.Spoiler, entity.offset, entity.length),
        MessageEntityCode => new TextEntity(TextEntityType.Code, entity.offset, entity.length),
        MessageEntityPre pre => new TextEntity(TextEntityType.Pre, entity.offset, entity.length, Language: pre.language),
        MessageEntityTextUrl textUrl => new TextEntity(TextEntityType.TextLink, entity.offset, entity.length, Url: textUrl.url),
        MessageEntityBlockquote => new TextEntity(TextEntityType.Blockquote, entity.offset, entity.length),
        _ => null
    };
}