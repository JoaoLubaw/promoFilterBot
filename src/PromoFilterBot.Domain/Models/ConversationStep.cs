/*
 * File: ConversationStep.cs
 * Project: Models
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

namespace PromoFilterBot.Domain.Models;

/// <summary>
/// Step of a guided flow the user is in.
/// </summary>
public enum ConversationStep {
    None = 0,
    AwaitingAlertText = 1,
    AwaitingMaxPrice = 2,
    AwaitingCustomPrice = 3,
    ConfirmingAlert = 4,

    /// <summary>
    /// The admin is sending the channels to add.
    /// </summary>
    AwaitingChannelsToAdd = 5
}