/*
 * File: EditAlertRequestDto.cs
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
/// Edit alert request. The guided flow asks every field again, so the alert is replaced as a whole.
/// </summary>
public class EditAlertRequestDto {
    public required string AlertText { get; set; }

    public decimal? AlertMaxPrice { get; set; }

    public bool IsValid() {
        return !string.IsNullOrWhiteSpace(AlertText);
    }
}