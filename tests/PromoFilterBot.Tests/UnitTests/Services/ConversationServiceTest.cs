/*
 * File: ConversationServiceTest.cs
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
using PromoFilterBot.Infrastructure.Dto;
using PromoFilterBot.Infrastructure.Interfaces;
using PromoFilterBot.Infrastructure.Options;
using PromoFilterBot.Infrastructure.Services;
using PromoFilterBot.Tests.Helpers;

using MsOptions = Microsoft.Extensions.Options.Options;

namespace PromoFilterBot.Tests.Services;

public class ConversationServiceTest {
    private readonly Mock<IConversationStateRepository> _mockConversationStateRepository = new();
    private readonly Mock<IUserRepository> _mockUserRepository = new();
    private readonly Mock<ILogger<ConversationService>> _mockLogger = new();
    private readonly ConversationService _service;

    public ConversationServiceTest() {
        _service = new ConversationService(
            _mockConversationStateRepository.Object,
            _mockUserRepository.Object,
            new FixedTimeProvider(TestData.Now),
            MsOptions.Create(new LimitsOptions { ConversationTimeoutHours = 24 }),
            _mockLogger.Object);

        _mockUserRepository.Setup(r => r.GetUserByChatIdAsync(TestData.ChatId)).ReturnsAsync(TestData.User());
    }

    private static ConversationState State(string? json, DateTime updatedAt, ConversationStep step = ConversationStep.AwaitingMaxPrice) {
        return new ConversationState { UserId = 1, ConversationStateStep = step, ConversationStateDataJson = json, UpdateTime = updatedAt };
    }

    [Fact]
    public async Task GetState_ReadsTheAnswersSoFar() {
        _mockConversationStateRepository.Setup(r => r.GetConversationStateAsync(1))
            .ReturnsAsync(State("{\"AlertText\":\"rtx 4060\",\"AlertMaxPrice\":2000}", TestData.Now.AddMinutes(-5)));

        ConversationStateDto? state = await _service.GetStateAsync(TestData.ChatId);

        Assert.Equal(ConversationStep.AwaitingMaxPrice, state!.Step);
        Assert.Equal("rtx 4060", state.Draft.AlertText);
        Assert.Equal(2000m, state.Draft.AlertMaxPrice);
    }

    [Fact]
    public async Task GetState_StartsOver_WhenTheFlowExpired() {
        _mockConversationStateRepository.Setup(r => r.GetConversationStateAsync(1)).ReturnsAsync(State("{}", TestData.Now.AddHours(-25)));

        Assert.Null(await _service.GetStateAsync(TestData.ChatId));
        _mockConversationStateRepository.Verify(r => r.DeleteConversationStateAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetState_StartsTheDraftOver_WhenTheDataCannotBeRead() {
        _mockConversationStateRepository.Setup(r => r.GetConversationStateAsync(1)).ReturnsAsync(State("{not json", TestData.Now));

        ConversationStateDto? state = await _service.GetStateAsync(TestData.ChatId);

        Assert.Null(state!.Draft.AlertText);
    }

    [Fact]
    public async Task GetState_IsNull_WithoutAFlow() {
        _mockConversationStateRepository.Setup(r => r.GetConversationStateAsync(1)).ReturnsAsync(State(null, TestData.Now, ConversationStep.None));

        Assert.Null(await _service.GetStateAsync(TestData.ChatId));
    }

    [Fact]
    public async Task SaveState_SavesTheDraftAsJson() {
        await _service.SaveStateAsync(TestData.ChatId, ConversationStep.ConfirmingAlert, new AlertDraftDto { AlertText = "mouse" });

        _mockConversationStateRepository.Verify(r => r.SaveConversationStateAsync(
            1, ConversationStep.ConfirmingAlert, It.Is<string>(json => json.Contains("\"AlertText\":\"mouse\"")), "chat:1001"), Times.Once);
    }
}