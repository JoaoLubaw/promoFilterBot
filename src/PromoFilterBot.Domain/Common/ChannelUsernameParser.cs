/*
 * File: ChannelUsernameParser.cs
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

using System.Text.RegularExpressions;

using PromoFilterBot.Domain.Dto;

namespace PromoFilterBot.Domain.Common;

/// <summary>
/// Reads a channel the admin sends: <c>@canal</c>, <c>canal</c>, <c>t.me/canal</c> or the link of a post of it. Invite
/// links (<c>t.me/+abc</c>) are of private channels, which are not monitored.
/// </summary>
public static class ChannelUsernameParser {
    private static readonly Regex UsernameRegex = new(@"^[a-z][a-z0-9_]{3,31}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly string[] TelegramHosts = ["t.me/", "telegram.me/", "telegram.dog/"];

    public static ChannelReference Parse(string? input) {
        if (string.IsNullOrWhiteSpace(input)) return ChannelReference.Invalid;

        string reference = input.Trim();

        foreach (string prefix in new[] { "https://", "http://", "www." }) {
            if (reference.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) reference = reference[prefix.Length..];
        }

        string? host = TelegramHosts.FirstOrDefault(h => reference.StartsWith(h, StringComparison.OrdinalIgnoreCase));

        if (host != null) {
            reference = reference[host.Length..];

            if (reference.StartsWith('+') || reference.StartsWith("joinchat/", StringComparison.OrdinalIgnoreCase)) {
                return new ChannelReference(null, IsPrivateLink: true);
            }

            if (reference.StartsWith("s/", StringComparison.OrdinalIgnoreCase)) reference = reference[2..];

            reference = reference.Split('/', '?', '#')[0];
        }

        string username = reference.TrimStart('@').ToLowerInvariant();

        return UsernameRegex.IsMatch(username) ? new ChannelReference(username, IsPrivateLink: false) : ChannelReference.Invalid;
    }
}