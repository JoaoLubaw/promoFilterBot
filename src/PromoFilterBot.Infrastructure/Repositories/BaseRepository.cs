/*
 * File: BaseRepository.cs
 * Project: Repositories
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

using System.Diagnostics.CodeAnalysis;

using PromoFilterBot.Infrastructure.Models;

namespace PromoFilterBot.Infrastructure.Repositories;

[ExcludeFromCodeCoverage]
public class BaseRepository {
    internal readonly PromoFilterBotDbContext _dbContext;

    public BaseRepository(PromoFilterBotDbContext dbContext) {
        _dbContext = dbContext;
    }
}