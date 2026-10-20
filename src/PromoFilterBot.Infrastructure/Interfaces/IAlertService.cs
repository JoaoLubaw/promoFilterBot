/*
 * File: IAlertService.cs
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

using PromoFilterBot.Infrastructure.Common;
using PromoFilterBot.Infrastructure.Dto;

namespace PromoFilterBot.Infrastructure.Interfaces;

public interface IAlertService {
    Task<ServiceResult<AlertDto>> CreateAlertAsync(long chatId, CreateAlertRequestDto requestDto);

    Task<ServiceResult<List<AlertDto>>> GetAlertsAsync(long chatId);

    Task<ServiceResult<AlertDto>> GetAlertAsync(long chatId, int alertId);

    Task<ServiceResult<AlertDto>> EditAlertAsync(long chatId, int alertId, EditAlertRequestDto requestDto);

    // Pauses an active alert, or resumes a paused one.
    Task<ServiceResult<AlertDto>> ToggleAlertAsync(long chatId, int alertId);

    Task<ServiceResult<AlertDto>> PauseAlertAsync(long chatId, int alertId);

    Task<ServiceResult<AlertDto>> DeleteAlertAsync(long chatId, int alertId);

    // Deletes the alert by its number in /alertas (/remover 3).
    Task<ServiceResult<AlertDto>> DeleteAlertByPositionAsync(long chatId, int position);
}