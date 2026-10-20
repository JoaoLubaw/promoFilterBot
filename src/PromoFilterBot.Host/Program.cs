/*
 * File: Program.cs
 * Project: PromoFilterBot.Host
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
using System.Net;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using PromoFilterBot.Bot.BackgroundServices;
using PromoFilterBot.Bot.Handlers;
using PromoFilterBot.Bot.Interfaces;
using PromoFilterBot.Bot.Options;
using PromoFilterBot.Bot.Services;
using PromoFilterBot.Host.BackgroundServices;
using PromoFilterBot.Host.Options;
using PromoFilterBot.Infrastructure.Interfaces;
using PromoFilterBot.Infrastructure.Models;
using PromoFilterBot.Infrastructure.Options;
using PromoFilterBot.Infrastructure.Repositories;
using PromoFilterBot.Infrastructure.Services;
using PromoFilterBot.Reader.BackgroundServices;
using PromoFilterBot.Reader.Interfaces;
using PromoFilterBot.Reader.Options;
using PromoFilterBot.Reader.Services;

using Serilog;

using Telegram.Bot;

[assembly: SuppressMessage(
    "SonarQube",
    "S3776:CognitiveComplexity",
    Justification = "Program.cs serves as the application's composition root, where configuration complexity is intentionally centralized."
)]

const string LoginArgument = "--login";
const int MigrationAttempts = 10;

// Logger initialization
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try {
    // First login of the reader account, typed by a person: docker compose run --rm app --login
    if (args.Contains(LoginArgument)) {
        string environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? Environments.Production;

        IConfiguration loginConfiguration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        ReaderOptions loginOptions = loginConfiguration.GetSection(ReaderOptions.SectionName).Get<ReaderOptions>() ?? new ReaderOptions();

        return await ReaderLogin.RunInteractiveAsync(loginOptions);
    }

    Log.Information("Starting up PromoFilterBot!");

    HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog((services, lc) => lc.ReadFrom.Configuration(builder.Configuration));

    /* Options */
    builder.Services.AddOptions<GeneralOptions>().Bind(builder.Configuration.GetSection(GeneralOptions.SectionName));
    builder.Services.AddOptions<LimitsOptions>().Bind(builder.Configuration.GetSection(LimitsOptions.SectionName));
    builder.Services.AddOptions<MatchingOptions>().Bind(builder.Configuration.GetSection(MatchingOptions.SectionName));
    builder.Services.AddOptions<ReaderOptions>().Bind(builder.Configuration.GetSection(ReaderOptions.SectionName));
    builder.Services.AddOptions<MonitoringOptions>().Bind(builder.Configuration.GetSection(MonitoringOptions.SectionName));
    builder.Services.AddOptions<BotOptions>()
        .Bind(builder.Configuration.GetSection(BotOptions.SectionName))
        .Validate(o => !string.IsNullOrWhiteSpace(o.Token), "Bot:Token is not configured (BOT_TOKEN, the token of @BotFather).")
        .ValidateOnStart();

    builder.Services.AddSingleton(TimeProvider.System);

    /* Database */
    builder.Services.AddDbContext<PromoFilterBotDbContext>(options => {
        options.UseNpgsql(builder.Configuration.GetConnectionString("PromoFilterBotPostgres"));
    });

    /* Repositories */
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IAlertRepository, AlertRepository>();
    builder.Services.AddScoped<IChannelRepository, ChannelRepository>();
    builder.Services.AddScoped<IChannelMessageRepository, ChannelMessageRepository>();
    builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
    builder.Services.AddScoped<IConversationStateRepository, ConversationStateRepository>();
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

    /* Services */
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IAlertService, AlertService>();
    builder.Services.AddScoped<IChannelService, ChannelService>();
    builder.Services.AddScoped<IConversationService, ConversationService>();
    builder.Services.AddScoped<IMatchingService, MatchingService>();
    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddScoped<IAdminService, AdminService>();
    builder.Services.AddScoped<IMaintenanceService, MaintenanceService>();

    /* Shared state (singletons, without DbContext) */
    builder.Services.AddSingleton<IAlertIndexProvider, AlertIndexProvider>();
    builder.Services.AddSingleton<IReaderStatusService, ReaderStatusService>();
    builder.Services.AddSingleton<IOfferQueue, OfferQueue>();
    builder.Services.AddSingleton<INotificationQueue, NotificationQueue>();
    builder.Services.AddSingleton<IShortLinkResolver>(sp => {
        MatchingOptions matchingOptions = sp.GetRequiredService<IOptions<MatchingOptions>>().Value;

        // Redirects are followed one by one by the resolver, to stop at the first link of a known store
        HttpClient httpClient = new HttpClient(new SocketsHttpHandler {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.All,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5)
        }) {
            Timeout = TimeSpan.FromSeconds(matchingOptions.ShortLinkTimeoutSeconds)
        };
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; PromoFilterBot/1.0)");

        return new ShortLinkResolver(httpClient, sp.GetRequiredService<IOptions<MatchingOptions>>(), sp.GetRequiredService<ILogger<ShortLinkResolver>>());
    });

    /* Reader */
    builder.Services.AddSingleton<IReaderSession, ReaderSession>();
    builder.Services.AddSingleton<IActiveChannelCache, ActiveChannelCache>();
    builder.Services.AddSingleton<IReaderUpdateHandler, ReaderUpdateHandler>();

    /* Bot */
    builder.Services.AddSingleton<ITelegramBotClient>(sp => new TelegramBotClient(sp.GetRequiredService<IOptions<BotOptions>>().Value.Token));
    builder.Services.AddSingleton<ITelegramRateLimiter, TelegramRateLimiter>();
    builder.Services.AddSingleton<IBotMessenger, TelegramBotMessenger>();
    builder.Services.AddSingleton<IBotNotifier, TelegramBotNotifier>();
    builder.Services.AddSingleton<ILocalClock, LocalClock>();
    builder.Services.AddSingleton<BotUpdateHandler>();

    /* Handlers (one scope per update) */
    builder.Services.AddScoped<UpdateRouter>();
    builder.Services.AddScoped<StartHandler>();
    builder.Services.AddScoped<AlertHandler>();
    builder.Services.AddScoped<AlertWizardHandler>();
    builder.Services.AddScoped<PauseHandler>();
    builder.Services.AddScoped<AccountHandler>();
    builder.Services.AddScoped<AdminHandler>();
    builder.Services.AddScoped<NotificationHandler>();

    /* Hosted services */
    builder.Services.AddHostedService<BotPollingWorker>();
    builder.Services.AddHostedService<NotifierWorker>();
    builder.Services.AddHostedService<MatcherWorker>();
    builder.Services.AddHostedService<ReaderWorker>();
    builder.Services.AddHostedService<ChannelJoinWorker>();
    builder.Services.AddHostedService<MonitoringWorker>();
    builder.Services.AddHostedService<CleanupWorker>();

    IHost host = builder.Build();

    if (builder.Configuration.GetValue("Database:ApplyMigrationsOnStartup", true)) {
        await ApplyMigrationsAsync(host);
    }

    await host.RunAsync();

    return 0;
}
catch (Exception ex) {
    Log.Fatal(ex, "An unhandled exception occurred during bootstrapping");

    return 1;
}
finally {
    await Log.CloseAndFlushAsync();
}

// The schema is the one of the EF migrations; the database container may still be starting
static async Task ApplyMigrationsAsync(IHost host) {
    using IServiceScope scope = host.Services.CreateScope();
    PromoFilterBotDbContext dbContext = scope.ServiceProvider.GetRequiredService<PromoFilterBotDbContext>();

    for (int attempt = 1; ; attempt++) {
        try {
            await dbContext.Database.MigrateAsync();
            Log.Information("Database schema up to date");

            return;
        }
        catch (Exception ex) when (attempt < MigrationAttempts) {
            Log.Warning(ex, "Database not ready (attempt {Attempt} of {Attempts}), trying again in 5s", attempt, MigrationAttempts);
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}

[ExcludeFromCodeCoverage]
public partial class Program {
    protected Program() { }
}