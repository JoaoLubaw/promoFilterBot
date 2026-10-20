/*
 * File: MonitoringOptions.cs
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

namespace PromoFilterBot.Host.Options;

public class MonitoringOptions {
    public const string SectionName = "Monitoring";

    public int CheckIntervalMinutes { get; set; } = 5;

    /// <summary>
    /// The admin is told when the reader account stays disconnected for this long.
    /// </summary>
    public int DisconnectedAlertMinutes { get; set; } = 10;

    /// <summary>
    /// Heartbeat: the admin is told when no post arrives from the channels for this long.
    /// </summary>
    public int SilenceAlertHours { get; set; } = 6;

    public int CleanupIntervalHours { get; set; } = 24;
}