/*
 * File: ConversationStateDto.cs
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

using PromoFilterBot.Domain.Models;

namespace PromoFilterBot.Infrastructure.Dto;

public class ConversationStateDto {
    public ConversationStep Step { get; set; }

    public AlertDraftDto Draft { get; set; } = new AlertDraftDto();
}

/// <summary>
/// Answers of the guided /adicionar so far.
/// </summary>
public class AlertDraftDto {
    /// <summary>
    /// Alert being edited, or null for a new one.
    /// </summary>
    public int? EditingAlertId { get; set; }

    public string? AlertText { get; set; }

    public bool PriceChosen { get; set; }

    public decimal? AlertMaxPrice { get; set; }
}