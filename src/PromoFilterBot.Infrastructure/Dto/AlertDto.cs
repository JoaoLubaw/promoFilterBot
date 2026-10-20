/*
 * File: AlertDto.cs
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

public class AlertDto {
    public int AlertId { get; set; }

    /// <summary>
    /// Number of the alert in /alertas, the one /remover uses.
    /// </summary>
    public int AlertPosition { get; set; }

    public required string AlertOriginalText { get; set; }

    /// <summary>
    /// The terms as the matcher reads them, such as <c>rtx · 4060 · sem: sata</c>.
    /// </summary>
    public required string AlertDescription { get; set; }

    public decimal? AlertMaxPrice { get; set; }

    public bool AlertIsActive { get; set; }
}