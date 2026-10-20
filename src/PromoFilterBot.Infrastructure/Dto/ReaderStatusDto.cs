/*
 * File: ReaderStatusDto.cs
 * Project: Dto
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

namespace PromoFilterBot.Infrastructure.Dto;

/// <summary>
/// State of the reader account, kept in memory.
/// </summary>
public class ReaderStatusDto {
    public bool IsEnabled { get; set; } = true;

    public bool IsConnected { get; set; }

    /// <summary>
    /// The session is missing or was revoked, so someone has to run the login again.
    /// </summary>
    public bool LoginRequired { get; set; }

    public string? AccountName { get; set; }

    public DateTime? ConnectedSince { get; set; }

    public DateTime? DisconnectedSince { get; set; }

    public string? LastError { get; set; }

    public DateTime? LastMessageAt { get; set; }

    public int? LastFloodWaitSeconds { get; set; }

    public DateTime? LastFloodWaitAt { get; set; }
}