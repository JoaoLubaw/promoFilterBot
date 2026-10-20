/*
 * File: ChannelServiceTest.cs
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

using Moq;

using PromoFilterBot.Domain.Models;
using PromoFilterBot.Infrastructure.Common;
using PromoFilterBot.Infrastructure.Dto;
using PromoFilterBot.Infrastructure.Interfaces;
using PromoFilterBot.Infrastructure.Options;
using PromoFilterBot.Infrastructure.Services;
using PromoFilterBot.Tests.Helpers;

using MsOptions = Microsoft.Extensions.Options.Options;

namespace PromoFilterBot.Tests.Services;

public class ChannelServiceTest {
    private readonly Mock<IChannelRepository> _mockChannelRepository = new();
    private readonly Mock<ILogger<ChannelService>> _mockLogger = new();
    private readonly ChannelService _service;

    public ChannelServiceTest() {
        _service = new ChannelService(
            _mockChannelRepository.Object,
            MsOptions.Create(new GeneralOptions { AdminChatId = TestData.AdminChatId }),
            _mockLogger.Object);

        _mockChannelRepository
            .Setup(r => r.SetChannelStatusAsync(It.IsAny<Channel>(), It.IsAny<ChannelStatus>(), It.IsAny<string?>(), It.IsAny<string>()))
            .ReturnsAsync((Channel channel, ChannelStatus status, string? reason, string _) => {
                channel.ChannelStatus = status;
                channel.ChannelStatusReason = reason;
                return channel;
            });
        _mockChannelRepository
            .Setup(r => r.CreateChannelAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string username, string _) => TestData.Channel(50, username, ChannelStatus.Approved));
    }

    [Fact]
    public async Task AddChannels_ReturnsNotAdmin_ForAnyoneElse() {
        ServiceResult<List<ChannelAdditionDto>> result = await _service.AddChannelsAsync(TestData.ChatId, "@lobao");

        Assert.Equal(ResultCode.NOT_ADMIN, result.Code);
        _mockChannelRepository.Verify(r => r.CreateChannelAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" , \n ")]
    public async Task AddChannels_ReturnsEmptyRequest_WithoutAChannel(string? references) {
        ServiceResult<List<ChannelAdditionDto>> result = await _service.AddChannelsAsync(TestData.AdminChatId, references);

        Assert.Equal(ResultCode.EMPTY_REQUEST, result.Code);
    }

    [Fact]
    public async Task AddChannels_TellsTheOutcomeOfEachChannel() {
        _mockChannelRepository.Setup(r => r.GetChannelByUsernameAsync("lobao")).ReturnsAsync(TestData.Channel(1, "lobao", ChannelStatus.Active));

        ServiceResult<List<ChannelAdditionDto>> result = await _service.AddChannelsAsync(TestData.AdminChatId, "@Novo, t.me/lobao\nt.me/+abc um-canal @novo");

        Assert.Equal(ResultCode.NO_ERROR, result.Code);
        Assert.Equal(
            [ResultCode.NO_ERROR, ResultCode.CHANNEL_ALREADY_MONITORED, ResultCode.PRIVATE_CHANNEL_LINK, ResultCode.INVALID_CHANNEL_REFERENCE],
            result.Data!.Select(a => a.Code));
        Assert.Equal("novo", result.Data![0].Channel!.ChannelUsername);
        _mockChannelRepository.Verify(r => r.CreateChannelAsync("novo", "chat:9999"), Times.Once);
    }

    [Fact]
    public async Task AddChannels_BringsBackARemovedChannel() {
        _mockChannelRepository.Setup(r => r.GetChannelByUsernameAsync("lobao")).ReturnsAsync(TestData.Channel(1, "lobao", ChannelStatus.Blocked));

        ServiceResult<List<ChannelAdditionDto>> result = await _service.AddChannelsAsync(TestData.AdminChatId, "@lobao");

        ChannelAdditionDto addition = Assert.Single(result.Data!);
        Assert.True(addition.IsAdded);
        Assert.Equal(ChannelStatus.Approved, addition.Channel!.ChannelStatus);
        _mockChannelRepository.Verify(r => r.CreateChannelAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RemoveChannel_ReturnsNotAdmin_ForAnyoneElse() {
        ServiceResult<ChannelDto> result = await _service.RemoveChannelAsync(TestData.ChatId, "@lobao");

        Assert.Equal(ResultCode.NOT_ADMIN, result.Code);
    }

    [Fact]
    public async Task RemoveChannel_ReturnsInvalidReference_ForWhatIsNotAChannel() {
        ServiceResult<ChannelDto> result = await _service.RemoveChannelAsync(TestData.AdminChatId, "um canal");

        Assert.Equal(ResultCode.INVALID_CHANNEL_REFERENCE, result.Code);
    }

    [Fact]
    public async Task RemoveChannel_ReturnsNotFound_ForAnUnknownChannel() {
        _mockChannelRepository.Setup(r => r.GetChannelByUsernameAsync("lobao")).ReturnsAsync((Channel?)null);

        ServiceResult<ChannelDto> result = await _service.RemoveChannelAsync(TestData.AdminChatId, "@lobao");

        Assert.Equal(ResultCode.CHANNEL_NOT_FOUND, result.Code);
    }

    [Fact]
    public async Task RemoveChannel_BlocksWithTheReason() {
        _mockChannelRepository.Setup(r => r.GetChannelByUsernameAsync("lobao")).ReturnsAsync(TestData.Channel(1, "lobao"));

        ServiceResult<ChannelDto> result = await _service.RemoveChannelAsync(TestData.AdminChatId, "t.me/lobao");

        Assert.Equal(ChannelStatus.Blocked, result.Data!.ChannelStatus);
        Assert.NotNull(result.Data.ChannelStatusReason);
    }

    [Fact]
    public async Task GetChannels_ReturnsNotAdmin_ForAnyoneElse() {
        ServiceResult<List<ChannelDto>> result = await _service.GetChannelsAsync(TestData.ChatId, null);

        Assert.Equal(ResultCode.NOT_ADMIN, result.Code);
    }

    [Fact]
    public async Task MarkChannelJoined_ReturnsNotFound_ForAnUnknownChannel() {
        _mockChannelRepository.Setup(r => r.GetChannelByIdAsync(2)).ReturnsAsync((Channel?)null);

        ServiceResult<ChannelDto> result = await _service.MarkChannelJoinedAsync(2, 777, 123, "Lobão");

        Assert.Equal(ResultCode.CHANNEL_NOT_FOUND, result.Code);
    }

    [Fact]
    public async Task MarkChannelJoined_BlocksAUsernameOfAChannelAlreadyMonitored() {
        Channel channel = TestData.Channel(2, "lobao2", ChannelStatus.Approved);
        _mockChannelRepository.Setup(r => r.GetChannelByIdAsync(2)).ReturnsAsync(channel);
        _mockChannelRepository.Setup(r => r.GetChannelByTelegramIdAsync(777)).ReturnsAsync(TestData.Channel(1, "lobao", ChannelStatus.Active, 777));

        ServiceResult<ChannelDto> result = await _service.MarkChannelJoinedAsync(2, 777, 123, "Lobão");

        Assert.Equal(ResultCode.CHANNEL_ALREADY_MONITORED, result.Code);
        Assert.Equal(ChannelStatus.Blocked, channel.ChannelStatus);
        _mockChannelRepository.Verify(r => r.SetChannelJoinedAsync(It.IsAny<Channel>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<string?>(), It.IsAny<DateTime>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task MarkChannelJoined_ActivatesTheChannel() {
        Channel channel = TestData.Channel(2, "lobao", ChannelStatus.Approved);
        _mockChannelRepository.Setup(r => r.GetChannelByIdAsync(2)).ReturnsAsync(channel);
        _mockChannelRepository.Setup(r => r.GetChannelByTelegramIdAsync(777)).ReturnsAsync((Channel?)null);
        _mockChannelRepository
            .Setup(r => r.SetChannelJoinedAsync(channel, 777, 123, "Lobão", It.IsAny<DateTime>(), SystemUsers.READER))
            .ReturnsAsync(() => { channel.ChannelStatus = ChannelStatus.Active; return channel; });

        ServiceResult<ChannelDto> result = await _service.MarkChannelJoinedAsync(2, 777, 123, "Lobão");

        Assert.Equal(ResultCode.NO_ERROR, result.Code);
        Assert.Equal(ChannelStatus.Active, result.Data!.ChannelStatus);
    }

    [Fact]
    public async Task MarkChannelUnavailable_BlocksWithTheReason() {
        _mockChannelRepository.Setup(r => r.GetChannelByIdAsync(2)).ReturnsAsync(TestData.Channel(2, "grupo", ChannelStatus.Approved));

        ServiceResult<ChannelDto> result = await _service.MarkChannelUnavailableAsync(2, "Não é um canal público");

        Assert.Equal(ChannelStatus.Blocked, result.Data!.ChannelStatus);
        Assert.Equal("Não é um canal público", result.Data.ChannelStatusReason);
    }
}