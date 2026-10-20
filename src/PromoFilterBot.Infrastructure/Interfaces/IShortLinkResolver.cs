/*
 * File: IShortLinkResolver.cs
 * Project: Interfaces
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

namespace PromoFilterBot.Infrastructure.Interfaces;

public interface IShortLinkResolver {
    bool IsShortLink(string link);

    // Follows the redirects of a short link, up to the page of the product. Null when it cannot be followed.
    Task<string?> ResolveAsync(string link, CancellationToken cancellationToken = default);
}