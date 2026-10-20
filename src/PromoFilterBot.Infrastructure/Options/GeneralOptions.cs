/*
 * File: GeneralOptions.cs
 * Project: Options
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

namespace PromoFilterBot.Infrastructure.Options;

public class GeneralOptions {
    public const string SectionName = "General";

    /// <summary>
    /// Chat of the admin, the only one who registers the channels and sees /admin.
    /// </summary>
    public long AdminChatId { get; set; }

    /// <summary>
    /// Time zone of the users, for "até amanhã", the dates shown and the day of an offer.
    /// </summary>
    public string TimeZone { get; set; } = "America/Sao_Paulo";

    public bool IsAdmin(long chatId) => AdminChatId != 0 && chatId == AdminChatId;
}