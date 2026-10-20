/*
 * File: IReaderUpdateHandler.cs
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

using TL;

namespace PromoFilterBot.Reader.Interfaces;

public interface IReaderUpdateHandler {
    // Publishes the new posts of the monitored channels to the offer queue. Everything else is ignored: the account
    // only reads.
    Task HandleAsync(Update update);
}