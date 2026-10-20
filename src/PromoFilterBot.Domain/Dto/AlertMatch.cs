/*
 * File: AlertMatch.cs
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
/// An alert matched by an offer. <paramref name="PriceUnknown"/> when the alert has a limit and the offer has no price
/// the extractor could read, so the user is told it was not checked.
/// </summary>
public sealed record AlertMatch(IndexedAlert Alert, bool PriceUnknown);