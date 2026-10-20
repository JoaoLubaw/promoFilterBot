/*
 * File: IndexedAlert.cs
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

namespace PromoFilterBot.Domain.Dto;

/// <summary>
/// What the matcher needs of an active alert, kept in memory.
/// </summary>
public sealed class IndexedAlert {
    public required int AlertId { get; init; }

    public required int UserId { get; init; }

    public required AlertQuery Query { get; init; }

    public decimal? MaxPrice { get; init; }
}