/*
 * File: ActiveChannelCache.cs
 * Project: Services
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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using PromoFilterBot.Infrastructure.Dto;
using PromoFilterBot.Infrastructure.Interfaces;
using PromoFilterBot.Reader.Interfaces;

namespace PromoFilterBot.Reader.Services;

public sealed class ActiveChannelCache : IActiveChannelCache {
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ActiveChannelCache> _logger;

    // Replaced as a whole on each refresh, so the reads need no lock
    private volatile Dictionary<long, ChannelPeerDto> _channels = [];

    public ActiveChannelCache(IServiceScopeFactory scopeFactory, ILogger<ActiveChannelCache> logger) {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public int Count => _channels.Count;

    public IReadOnlyCollection<ChannelPeerDto> Peers => _channels.Values;

    public bool IsMonitored(long telegramChannelId) => _channels.ContainsKey(telegramChannelId);

    public async Task RefreshAsync(CancellationToken cancellationToken = default) {
        using IServiceScope scope = _scopeFactory.CreateScope();
        IChannelService channelService = scope.ServiceProvider.GetRequiredService<IChannelService>();

        List<ChannelPeerDto> peers = await channelService.GetActiveChannelPeersAsync();

        if (peers.Count != _channels.Count) _logger.LogInformation("Monitoring {Count} channels", peers.Count);

        _channels = peers.DistinctBy(p => p.TelegramId).ToDictionary(p => p.TelegramId);
    }
}