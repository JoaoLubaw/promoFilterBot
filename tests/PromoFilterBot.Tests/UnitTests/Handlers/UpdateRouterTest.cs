/*
 * File: UpdateRouterTest.cs
 * Project: Handlers
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

using Moq;

using PromoFilterBot.Bot.Common;
using PromoFilterBot.Domain.Models;
using PromoFilterBot.Infrastructure.Common;
using PromoFilterBot.Infrastructure.Dto;
using PromoFilterBot.Tests.Helpers;

using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

using TelegramUser = Telegram.Bot.Types.User;

namespace PromoFilterBot.Tests.Handlers;

public class UpdateRouterTest : HandlerTestBase {
    public UpdateRouterTest() {
        _mockUserService.Setup(s => s.RegisterUserAsync(TestData.ChatId, It.IsAny<string?>())).ReturnsAsync(Success(TestData.UserDto()));
    }

    private static Update TextUpdate(string text, ChatType chatType = ChatType.Private) {
        return new Update {
            Message = new Message {
                Chat = new Chat { Id = TestData.ChatId, Type = chatType },
                From = new TelegramUser { Id = TestData.ChatId, FirstName = "João" },
                Text = text
            }
        };
    }

    private static Update ButtonUpdate(string data) {
        return new Update {
            CallbackQuery = new CallbackQuery {
                Id = CallbackQueryId,
                Data = data,
                From = new TelegramUser { Id = TestData.ChatId, FirstName = "João" },
                Message = new Message { Id = MessageId, Chat = new Chat { Id = TestData.ChatId, Type = ChatType.Private } }
            }
        };
    }

    [Fact]
    public async Task Route_IgnoresTheGroups() {
        await CreateRouter().RouteAsync(TextUpdate("/start", ChatType.Group), CancellationToken.None);

        _mockUserService.Verify(s => s.RegisterUserAsync(It.IsAny<long>(), It.IsAny<string?>()), Times.Never);
        Assert.Empty(_sentMessages);
    }

    [Fact]
    public async Task Route_RegistersTheUserWithTheName() {
        await CreateRouter().RouteAsync(TextUpdate("/start"), CancellationToken.None);

        _mockUserService.Verify(s => s.RegisterUserAsync(TestData.ChatId, "João"), Times.Once);
        Assert.Contains("Olá, João", Assert.Single(_sentMessages));
    }

    [Fact]
    public async Task Route_StartsTheGuidedFlow_ForAnAddWithoutArguments() {
        await CreateRouter().RouteAsync(TextUpdate("/adicionar"), CancellationToken.None);

        _mockConversationService.Verify(s => s.ClearStateAsync(TestData.ChatId), Times.Once);
        _mockConversationService.Verify(s => s.SaveStateAsync(TestData.ChatId, ConversationStep.AwaitingAlertText, It.IsAny<AlertDraftDto>()), Times.Once);
    }

    [Fact]
    public async Task Route_SendsTheTextToTheStepOfTheFlow() {
        _mockConversationService.Setup(s => s.GetStateAsync(TestData.ChatId))
            .ReturnsAsync(new ConversationStateDto { Step = ConversationStep.AwaitingAlertText });

        await CreateRouter().RouteAsync(TextUpdate("air fryer"), CancellationToken.None);

        _mockConversationService.Verify(s => s.SaveStateAsync(TestData.ChatId, ConversationStep.AwaitingMaxPrice, It.Is<AlertDraftDto>(d => d.AlertText == "air fryer")), Times.Once);
    }

    [Fact]
    public async Task Route_SendsTheTextToTheChannelsOfTheAdmin() {
        _mockConversationService.Setup(s => s.GetStateAsync(TestData.ChatId))
            .ReturnsAsync(new ConversationStateDto { Step = ConversationStep.AwaitingChannelsToAdd });
        _mockChannelService.Setup(s => s.AddChannelsAsync(TestData.ChatId, "@lobao"))
            .ReturnsAsync(Success(new List<ChannelAdditionDto> {
                new ChannelAdditionDto {
                    Reference = "@lobao",
                    Code = ResultCode.NO_ERROR,
                    Channel = new ChannelDto { ChannelId = 1, ChannelUsername = "lobao", ChannelStatus = ChannelStatus.Approved }
                }
            }));

        await CreateRouter().RouteAsync(TextUpdate("@lobao"), CancellationToken.None);

        Assert.Contains("@lobao — na fila", Assert.Single(_sentMessages));
    }

    [Fact]
    public async Task Route_DoesNotKnowTheCommandOfChannels_ForTheUsers() {
        await CreateRouter().RouteAsync(TextUpdate("/canais"), CancellationToken.None);

        Assert.Equal(BotTexts.UnknownCommand, Assert.Single(_sentMessages));
    }

    [Fact]
    public async Task Route_ExplainsAText_OutsideAFlow() {
        await CreateRouter().RouteAsync(TextUpdate("rtx 4060"), CancellationToken.None);

        Assert.Contains("/adicionar rtx 4060", Assert.Single(_sentMessages));
    }

    [Fact]
    public async Task Route_AnswersEveryButton_EvenAnUnknownOne() {
        await CreateRouter().RouteAsync(ButtonUpdate("zz:qualquer"), CancellationToken.None);

        _mockMessenger.Verify(m => m.AnswerCallbackAsync(CallbackQueryId, null, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Route_SendsTheButtonToItsHandler() {
        _mockAlertService.Setup(s => s.GetAlertsAsync(TestData.ChatId)).ReturnsAsync(new ServiceResult<List<AlertDto>>(ResultCode.NO_ERROR, []));

        await CreateRouter().RouteAsync(ButtonUpdate("m:alerts"), CancellationToken.None);

        _mockConversationService.Verify(s => s.ClearStateAsync(TestData.ChatId), Times.Once);
        Assert.Single(_editedMessages);
    }
}