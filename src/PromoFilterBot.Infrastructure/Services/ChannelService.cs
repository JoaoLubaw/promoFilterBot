/*
 * File: ChannelService.cs
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

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using PromoFilterBot.Domain.Common;
using PromoFilterBot.Domain.Dto;
using PromoFilterBot.Domain.Models;
using PromoFilterBot.Infrastructure.Common;
using PromoFilterBot.Infrastructure.Dto;
using PromoFilterBot.Infrastructure.Interfaces;
using PromoFilterBot.Infrastructure.Options;

namespace PromoFilterBot.Infrastructure.Services;

public class ChannelService : IChannelService {
    private const string RemovedByAdminReason = "Removido pelo administrador";

    private static readonly char[] ReferenceSeparators = [' ', ',', ';', '\n', '\r', '\t'];

    private readonly IChannelRepository _channelRepository;
    private readonly GeneralOptions _generalOptions;
    private readonly ILogger<ChannelService> _logger;

    public ChannelService(
        IChannelRepository channelRepository,
        IOptions<GeneralOptions> generalOptions,
        ILogger<ChannelService> logger) {
        _channelRepository = channelRepository;
        _generalOptions = generalOptions.Value;
        _logger = logger;
    }

    public async Task<ServiceResult<List<ChannelAdditionDto>>> AddChannelsAsync(long chatId, string? references) {
        if (!_generalOptions.IsAdmin(chatId)) {
            _logger.LogWarning(LogMessages.NOT_ADMIN, chatId);
            return new ServiceResult<List<ChannelAdditionDto>>(ResultCode.NOT_ADMIN, null);
        }

        List<string> tokens = (references ?? string.Empty)
            .Split(ReferenceSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (tokens.Count == 0) {
            _logger.LogWarning(LogMessages.EMPTY_REQUEST, chatId);
            return new ServiceResult<List<ChannelAdditionDto>>(ResultCode.EMPTY_REQUEST, null);
        }

        List<ChannelAdditionDto> additions = [];

        foreach (string token in tokens) additions.Add(await AddChannelAsync(chatId, token));

        return new ServiceResult<List<ChannelAdditionDto>>(ResultCode.NO_ERROR, additions);
    }

    public async Task<ServiceResult<ChannelDto>> RemoveChannelAsync(long chatId, string? reference) {
        if (!_generalOptions.IsAdmin(chatId)) {
            _logger.LogWarning(LogMessages.NOT_ADMIN, chatId);
            return new ServiceResult<ChannelDto>(ResultCode.NOT_ADMIN, null);
        }

        ChannelReference channelReference = ChannelUsernameParser.Parse(reference);

        if (channelReference.Username == null) {
            _logger.LogWarning(LogMessages.INVALID_CHANNEL_REFERENCE, reference, chatId);
            return new ServiceResult<ChannelDto>(ResultCode.INVALID_CHANNEL_REFERENCE, null);
        }

        Channel? channel = await _channelRepository.GetChannelByUsernameAsync(channelReference.Username);

        if (channel == null) {
            _logger.LogWarning("Channel @{Username} not found", channelReference.Username);
            return new ServiceResult<ChannelDto>(ResultCode.CHANNEL_NOT_FOUND, null);
        }

        if (channel.ChannelStatus != ChannelStatus.Blocked) {
            channel = await _channelRepository.SetChannelStatusAsync(channel, ChannelStatus.Blocked, RemovedByAdminReason, SystemUsers.FromChat(chatId));
            _logger.LogInformation("Channel @{Username} removed by the admin", channel.ChannelUsername);
        }

        return new ServiceResult<ChannelDto>(ResultCode.NO_ERROR, channel.ToDto());
    }

    public async Task<ServiceResult<List<ChannelDto>>> GetChannelsAsync(long chatId, ChannelStatus? status) {
        if (!_generalOptions.IsAdmin(chatId)) {
            _logger.LogWarning(LogMessages.NOT_ADMIN, chatId);
            return new ServiceResult<List<ChannelDto>>(ResultCode.NOT_ADMIN, null);
        }

        List<Channel> channels = status.HasValue
            ? await _channelRepository.GetChannelsAsync(status.Value)
            : await _channelRepository.GetChannelsAsync();

        return new ServiceResult<List<ChannelDto>>(ResultCode.NO_ERROR, channels.Select(c => c.ToDto()).ToList());
    }

    public async Task<ChannelDto?> GetNextChannelToJoinAsync() {
        Channel? channel = await _channelRepository.GetNextChannelToJoinAsync();

        return channel?.ToDto();
    }

    public async Task<ServiceResult<ChannelDto>> MarkChannelJoinedAsync(int channelId, long telegramId, long accessHash, string? title) {
        Channel? channel = await _channelRepository.GetChannelByIdAsync(channelId);

        if (channel == null) {
            _logger.LogWarning(LogMessages.CHANNEL_NOT_FOUND, channelId);
            return new ServiceResult<ChannelDto>(ResultCode.CHANNEL_NOT_FOUND, null);
        }

        Channel? sameChannel = await _channelRepository.GetChannelByTelegramIdAsync(telegramId);

        // The username now belongs to a channel already monitored under another username
        if (sameChannel != null && sameChannel.ChannelId != channel.ChannelId) {
            channel = await _channelRepository.SetChannelStatusAsync(
                channel, ChannelStatus.Blocked, $"Mesmo canal que @{sameChannel.ChannelUsername}", SystemUsers.READER);

            _logger.LogWarning("Channel @{Username} is the same as @{Other}", channel.ChannelUsername, sameChannel.ChannelUsername);

            return new ServiceResult<ChannelDto>(ResultCode.CHANNEL_ALREADY_MONITORED, channel.ToDto());
        }

        DateTime joinedAt = DateTime.UtcNow;
        channel = await _channelRepository.SetChannelJoinedAsync(channel, telegramId, accessHash, title, joinedAt, SystemUsers.READER);

        _logger.LogInformation("Channel @{Username} ({TelegramId}) is now monitored", channel.ChannelUsername, telegramId);

        return new ServiceResult<ChannelDto>(ResultCode.NO_ERROR, channel.ToDto());
    }

    public async Task<ServiceResult<ChannelDto>> MarkChannelUnavailableAsync(int channelId, string reason) {
        Channel? channel = await _channelRepository.GetChannelByIdAsync(channelId);

        if (channel == null) {
            _logger.LogWarning(LogMessages.CHANNEL_NOT_FOUND, channelId);
            return new ServiceResult<ChannelDto>(ResultCode.CHANNEL_NOT_FOUND, null);
        }

        channel = await _channelRepository.SetChannelStatusAsync(channel, ChannelStatus.Blocked, reason, SystemUsers.READER);
        _logger.LogWarning("Channel @{Username} cannot be read: {Reason}", channel.ChannelUsername, reason);

        return new ServiceResult<ChannelDto>(ResultCode.NO_ERROR, channel.ToDto());
    }

    public async Task<List<ChannelPeerDto>> GetActiveChannelPeersAsync() {
        return await _channelRepository.GetActiveChannelPeersAsync();
    }

    private async Task<ChannelAdditionDto> AddChannelAsync(long chatId, string reference) {
        ChannelReference channelReference = ChannelUsernameParser.Parse(reference);

        if (channelReference.IsPrivateLink) {
            _logger.LogWarning("Chat {ChatId} sent the link of a private channel", chatId);
            return new ChannelAdditionDto { Reference = reference, Code = ResultCode.PRIVATE_CHANNEL_LINK };
        }

        if (channelReference.Username == null) {
            _logger.LogWarning(LogMessages.INVALID_CHANNEL_REFERENCE, reference, chatId);
            return new ChannelAdditionDto { Reference = reference, Code = ResultCode.INVALID_CHANNEL_REFERENCE };
        }

        Channel? channel = await _channelRepository.GetChannelByUsernameAsync(channelReference.Username);

        if (channel != null && channel.ChannelStatus is ChannelStatus.Active or ChannelStatus.Approved) {
            return new ChannelAdditionDto { Reference = reference, Code = ResultCode.CHANNEL_ALREADY_MONITORED, Channel = channel.ToDto() };
        }

        // A removed (or unreadable) channel goes back to the queue of the reader account
        channel = channel == null
            ? await _channelRepository.CreateChannelAsync(channelReference.Username, SystemUsers.FromChat(chatId))
            : await _channelRepository.SetChannelStatusAsync(channel, ChannelStatus.Approved, null, SystemUsers.FromChat(chatId));

        _logger.LogInformation("Channel @{Username} added by the admin", channel.ChannelUsername);

        return new ChannelAdditionDto { Reference = reference, Code = ResultCode.NO_ERROR, Channel = channel.ToDto() };
    }
}