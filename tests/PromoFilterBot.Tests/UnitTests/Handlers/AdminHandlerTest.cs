/*
 * File: AdminHandlerTest.cs
 * Project: Handlers
 * Create Date: Tuesday, 20th October 2026
 * Author: João Lubawaski (joaolubawaski@gmail.com)
 * -----
 * Last Modified: Tuesday, 20th October 2026 7:00:00 pm
 * Modified by: João Lubawaski (joaolubawaski@gmail.com)
 * -----
 * Copyright (c) 2026 João Lubawaski
 * ----
 * HISTORY:
 * Date      	By	Comments
 * ----------	---	---------------------------------------------------------
 */

using Moq;

using PromoFilterBot.Bot.Common;
using PromoFilterBot.Domain.Models;
using PromoFilterBot.Infrastructure.Common;
using PromoFilterBot.Infrastructure.Dto;
using PromoFilterBot.Tests.Helpers;

namespace PromoFilterBot.Tests.Handlers;

public class AdminHandlerTest : HandlerTestBase {
    private static ChannelAdditionDto Addition(string reference, ResultCode code, string? username = null) {
        ChannelDto? channel = username == null ? null : new ChannelDto { ChannelId = 1, ChannelUsername = username, ChannelStatus = ChannelStatus.Approved };

        return new ChannelAdditionDto { Reference = reference, Code = code, Channel = channel };
    }

    [Fact]
    public async Task HandleAdmin_AddsTheChannelsTyped_AndTellsEachOutcome() {
        _mockChannelService.Setup(s => s.AddChannelsAsync(TestData.ChatId, "@lobao @achados xyz"))
            .ReturnsAsync(Success(new List<ChannelAdditionDto> {
                Addition("@lobao", ResultCode.NO_ERROR, "lobao"),
                Addition("@achados", ResultCode.CHANNEL_ALREADY_MONITORED, "achados"),
                Addition("xyz", ResultCode.INVALID_CHANNEL_REFERENCE)
            }));

        await CreateAdminHandler().HandleAdminAsync(MessageContext(isAdmin: true), "adicionar @lobao @achados xyz", CancellationToken.None);

        string message = Assert.Single(_sentMessages);
        Assert.Contains("✅ @lobao — na fila da conta leitora", message);
        Assert.Contains("ℹ️ @achados — já estava na lista", message);
        Assert.Contains("❌ xyz — não reconheci", message);
    }

    [Fact]
    public async Task HandleAdmin_AsksTheChannels_WithoutAReference() {
        await CreateAdminHandler().HandleAdminAsync(MessageContext(isAdmin: true), "adicionar", CancellationToken.None);

        _mockConversationService.Verify(s => s.SaveStateAsync(TestData.ChatId, ConversationStep.AwaitingChannelsToAdd, null), Times.Once);
        Assert.Equal(BotTexts.AskChannelsToAdd, Assert.Single(_sentMessages));
    }

    [Fact]
    public async Task HandleCallback_DoesNotAskTheChannels_OfWhoIsNotTheAdmin() {
        await CreateAdminHandler().HandleCallbackAsync(CallbackContext(), new CallbackData(Callbacks.Admin, Callbacks.AdminAddChannels), CancellationToken.None);

        _mockConversationService.Verify(s => s.SaveStateAsync(It.IsAny<long>(), It.IsAny<ConversationStep>(), It.IsAny<AlertDraftDto?>()), Times.Never);
        _mockMessenger.Verify(m => m.AnswerCallbackAsync(CallbackQueryId, ResultMessages.Get(ResultCode.NOT_ADMIN), true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleChannelsText_LeavesTheStep_AndAddsTheChannels() {
        _mockChannelService.Setup(s => s.AddChannelsAsync(TestData.ChatId, "@lobao"))
            .ReturnsAsync(Success(new List<ChannelAdditionDto> { Addition("@lobao", ResultCode.NO_ERROR, "lobao") }));

        await CreateAdminHandler().HandleChannelsTextAsync(MessageContext(isAdmin: true), "@lobao", CancellationToken.None);

        _mockConversationService.Verify(s => s.ClearStateAsync(TestData.ChatId), Times.Once);
        Assert.Contains("@lobao — na fila", Assert.Single(_sentMessages));
    }

    [Fact]
    public async Task HandleAdmin_ShowsTheErrorOfTheService() {
        _mockChannelService.Setup(s => s.AddChannelsAsync(TestData.ChatId, "@lobao")).ReturnsAsync(Failure<List<ChannelAdditionDto>>(ResultCode.NOT_ADMIN));

        await CreateAdminHandler().HandleAdminAsync(MessageContext(), "adicionar @lobao", CancellationToken.None);

        Assert.Contains(ResultMessages.Get(ResultCode.NOT_ADMIN), Assert.Single(_sentMessages));
    }

    [Fact]
    public async Task HandleAdmin_RemovesAChannel() {
        _mockChannelService.Setup(s => s.RemoveChannelAsync(TestData.ChatId, "@lobao"))
            .ReturnsAsync(Success(new ChannelDto { ChannelId = 1, ChannelUsername = "lobao", ChannelStatus = ChannelStatus.Blocked }));

        await CreateAdminHandler().HandleAdminAsync(MessageContext(isAdmin: true), "remover @lobao", CancellationToken.None);

        Assert.Contains("Parei de ler @lobao", Assert.Single(_sentMessages));
    }
}