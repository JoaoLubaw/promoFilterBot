/*
 * File: ReaderOptions.cs
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

namespace PromoFilterBot.Reader.Options;

/// <summary>
/// The reader account: the user account of the admin, used only to read the public channels the admin registered. It
/// never lists or reads the chats of the account, and only joins, mutes and archives those channels.
/// </summary>
public class ReaderOptions {
    public const string SectionName = "Reader";

    public bool Enabled { get; set; } = true;

    /// <summary>
    /// api_id and api_hash of my.telegram.org.
    /// </summary>
    public int ApiId { get; set; }

    public string ApiHash { get; set; } = string.Empty;

    /// <summary>
    /// Phone of the reader account, in international format (+5511999999999).
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// The session file is worth the login of the account: keep it in a volume, never in the repository or the image.
    /// </summary>
    public string SessionPath { get; set; } = "session/wtelegram.session";

    /// <summary>
    /// Where the update state is saved, so the posts of a downtime are fetched on the next start.
    /// </summary>
    public string UpdatesStatePath { get; set; } = "session/updates.state";

    /// <summary>
    /// At most one channel is joined per interval, plus a random delay of up to the jitter.
    /// </summary>
    public int JoinIntervalMinutes { get; set; } = 15;

    public int JoinJitterMinutes { get; set; } = 10;

    public int MaxJoinsPerDay { get; set; } = 10;

    /// <summary>
    /// A channel the account is already in needs no join, so the next one is looked at after this (and the queue of new
    /// channels is checked this often).
    /// </summary>
    public int ResolveIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// The channels the reader joins are muted and archived, so they do not disturb the account on the phone.
    /// </summary>
    public bool MuteJoinedChannels { get; set; } = true;

    public bool ArchiveJoinedChannels { get; set; } = true;

    /// <summary>
    /// Limit of channels of an account without Premium.
    /// </summary>
    public int MaxChannels { get; set; } = 500;

    /// <summary>
    /// A FLOOD_WAIT shorter than this is waited for by WTelegramClient itself.
    /// </summary>
    public int FloodRetryThresholdSeconds { get; set; } = 60;

    /// <summary>
    /// A FLOOD_WAIT from this long on is reported to the admin.
    /// </summary>
    public int LongFloodWaitAlertSeconds { get; set; } = 300;

    public int ReconnectDelaySeconds { get; set; } = 30;

    public int ActiveChannelsRefreshSeconds { get; set; } = 120;

    public int StateSaveIntervalSeconds { get; set; } = 60;

    public bool IsConfigured => ApiId != 0 && !string.IsNullOrWhiteSpace(ApiHash) && !string.IsNullOrWhiteSpace(PhoneNumber);
}