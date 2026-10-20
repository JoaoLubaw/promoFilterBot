/*
 * File: CreateAlertRequestDto.cs
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
/// Create alert request.
/// </summary>
public class CreateAlertRequestDto {
    public required string AlertText { get; set; }

    /// <summary>
    /// Highest price accepted. Null means no limit.
    /// </summary>
    public decimal? AlertMaxPrice { get; set; }

    /// <summary>
    /// Minimum presence rule. The rules of the terms and the limits are checked by the service.
    /// </summary>
    public bool IsValid() {
        return !string.IsNullOrWhiteSpace(AlertText);
    }
}