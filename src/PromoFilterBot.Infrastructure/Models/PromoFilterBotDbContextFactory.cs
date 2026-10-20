/*
 * File: PromoFilterBotDbContextFactory.cs
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

using System.Diagnostics.CodeAnalysis;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PromoFilterBot.Infrastructure.Models;

/// <summary>
/// Used by <c>dotnet ef</c> to create the migrations, which do not need a database. Applying them by hand
/// (<c>dotnet ef database update</c>) reads the connection string of ConnectionStrings__PromoFilterBotPostgres.
/// </summary>
[ExcludeFromCodeCoverage]
public class PromoFilterBotDbContextFactory : IDesignTimeDbContextFactory<PromoFilterBotDbContext> {
    private const string LocalConnectionString = "Host=localhost;Port=5432;Database=promofilterbot;Username=promofilterbot;Password=promofilterbot";

    public PromoFilterBotDbContext CreateDbContext(string[] args) {
        string connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PromoFilterBotPostgres") ?? LocalConnectionString;

        DbContextOptionsBuilder<PromoFilterBotDbContext> optionsBuilder = new DbContextOptionsBuilder<PromoFilterBotDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new PromoFilterBotDbContext(optionsBuilder.Options);
    }
}