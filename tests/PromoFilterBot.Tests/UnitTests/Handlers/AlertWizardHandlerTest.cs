/*
 * File: AlertWizardHandlerTest.cs
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

namespace PromoFilterBot.Tests.Handlers;

public class AlertWizardHandlerTest : HandlerTestBase {
    private void SetState(ConversationStep step, AlertDraftDto draft) {
        _mockConversationService.Setup(s => s.GetStateAsync(TestData.ChatId)).ReturnsAsync(new ConversationStateDto { Step = step, Draft = draft });
    }

    [Fact]
    public async Task Start_AsksWhatToLookFor() {
        await CreateWizardHandler().StartAsync(MessageContext(), null, CancellationToken.None);

        _mockConversationService.Verify(s => s.SaveStateAsync(TestData.ChatId, ConversationStep.AwaitingAlertText, It.IsAny<AlertDraftDto>()), Times.Once);
        Assert.Contains("O que você quer acompanhar?", Assert.Single(_sentMessages));
    }

    [Fact]
    public async Task Start_FillsTheAnswers_WhenEditing() {
        AlertDto alert = AlertDto(maxPrice: 1500);
        _mockAlertService.Setup(s => s.GetAlertAsync(TestData.ChatId, 10)).ReturnsAsync(Success(alert));

        await CreateWizardHandler().StartAsync(MessageContext(), 10, CancellationToken.None);

        _mockConversationService.Verify(s => s.SaveStateAsync(TestData.ChatId, ConversationStep.AwaitingAlertText, It.Is<AlertDraftDto>(d =>
            d.EditingAlertId == 10 && d.AlertText == "rtx 4060" && d.AlertMaxPrice == 1500 && d.PriceChosen)), Times.Once);
        Assert.Contains("Editando o alerta", Assert.Single(_sentMessages));
    }

    [Fact]
    public async Task HandleText_AsksThePrice_AfterTheSearch() {
        ConversationStateDto state = new ConversationStateDto { Step = ConversationStep.AwaitingAlertText };

        await CreateWizardHandler().HandleTextAsync(MessageContext(), state, "mouse logitech g305", CancellationToken.None);

        _mockConversationService.Verify(s => s.SaveStateAsync(TestData.ChatId, ConversationStep.AwaitingMaxPrice,
            It.Is<AlertDraftDto>(d => d.AlertText == "mouse logitech g305")), Times.Once);
        Assert.Contains("Até quanto você quer pagar?", Assert.Single(_sentMessages));
    }

    [Fact]
    public async Task HandleText_GoesToTheSummary_WhenTheSearchHasTheLimit() {
        ConversationStateDto state = new ConversationStateDto { Step = ConversationStep.AwaitingAlertText };

        await CreateWizardHandler().HandleTextAsync(MessageContext(), state, "rtx 4060 max 2000", CancellationToken.None);

        _mockConversationService.Verify(s => s.SaveStateAsync(TestData.ChatId, ConversationStep.ConfirmingAlert,
            It.Is<AlertDraftDto>(d => d.AlertText == "rtx 4060" && d.AlertMaxPrice == 2000)), Times.Once);
        Assert.Contains("Resumo do alerta", Assert.Single(_sentMessages));
    }

    [Fact]
    public async Task HandleText_KeepsTheStep_WhenTheSearchHasNoTerm() {
        ConversationStateDto state = new ConversationStateDto { Step = ConversationStep.AwaitingAlertText };

        await CreateWizardHandler().HandleTextAsync(MessageContext(), state, "de a", CancellationToken.None);

        _mockConversationService.Verify(s => s.SaveStateAsync(It.IsAny<long>(), It.IsAny<ConversationStep>(), It.IsAny<AlertDraftDto>()), Times.Never);
        Assert.Contains("Não entendi o que procurar", Assert.Single(_sentMessages));
    }

    [Fact]
    public async Task HandleText_RefusesACustomPriceThatIsNotAnAmount() {
        ConversationStateDto state = new ConversationStateDto { Step = ConversationStep.AwaitingCustomPrice, Draft = new AlertDraftDto { AlertText = "rtx" } };

        await CreateWizardHandler().HandleTextAsync(MessageContext(), state, "barato", CancellationToken.None);

        Assert.Contains("preço máximo não é válido", Assert.Single(_sentMessages));
    }

    [Fact]
    public async Task HandleText_AsksToUseTheButtons_OnAStepOfButtons() {
        ConversationStateDto state = new ConversationStateDto { Step = ConversationStep.ConfirmingAlert };

        await CreateWizardHandler().HandleTextAsync(MessageContext(), state, "sim", CancellationToken.None);

        Assert.Equal(BotTexts.UseButtons, Assert.Single(_sentMessages));
    }

    [Fact]
    public async Task HandleCallback_GoesToTheSummary_AfterThePrice() {
        SetState(ConversationStep.AwaitingMaxPrice, new AlertDraftDto { AlertText = "rtx 4060" });

        await CreateWizardHandler().HandleCallbackAsync(CallbackContext(), new CallbackData(Callbacks.Wizard, Callbacks.WizardPrice, "300"), CancellationToken.None);

        _mockConversationService.Verify(s => s.SaveStateAsync(TestData.ChatId, ConversationStep.ConfirmingAlert,
            It.Is<AlertDraftDto>(d => d.AlertMaxPrice == 300 && d.PriceChosen)), Times.Once);
        Assert.Contains("Resumo do alerta", Assert.Single(_editedMessages));
    }

    [Fact]
    public async Task HandleCallback_SavesTheAlert() {
        SetState(ConversationStep.ConfirmingAlert, new AlertDraftDto { AlertText = "rtx 4060", PriceChosen = true, AlertMaxPrice = 2000 });
        _mockAlertService.Setup(s => s.CreateAlertAsync(TestData.ChatId, It.Is<CreateAlertRequestDto>(r => r.AlertText == "rtx 4060" && r.AlertMaxPrice == 2000)))
            .ReturnsAsync(Success(AlertDto()));

        await CreateWizardHandler().HandleCallbackAsync(CallbackContext(), new CallbackData(Callbacks.Wizard, Callbacks.WizardSave), CancellationToken.None);

        _mockConversationService.Verify(s => s.ClearStateAsync(TestData.ChatId), Times.Once);
        Assert.Contains("Alerta nº 1 criado", Assert.Single(_editedMessages));
    }

    [Fact]
    public async Task HandleCallback_EditsTheAlertBeingEdited() {
        SetState(ConversationStep.ConfirmingAlert, new AlertDraftDto { EditingAlertId = 10, AlertText = "rtx 4070", PriceChosen = true });
        _mockAlertService.Setup(s => s.EditAlertAsync(TestData.ChatId, 10, It.Is<EditAlertRequestDto>(r => r.AlertText == "rtx 4070")))
            .ReturnsAsync(Success(AlertDto(text: "rtx 4070")));

        await CreateWizardHandler().HandleCallbackAsync(CallbackContext(), new CallbackData(Callbacks.Wizard, Callbacks.WizardSave), CancellationToken.None);

        Assert.Contains("atualizado", Assert.Single(_editedMessages));
    }

    [Fact]
    public async Task HandleCallback_KeepsTheFlow_WhenTheServiceRefusesTheAlert() {
        SetState(ConversationStep.ConfirmingAlert, new AlertDraftDto { AlertText = "rtx 4060", PriceChosen = true });
        _mockAlertService.Setup(s => s.CreateAlertAsync(TestData.ChatId, It.IsAny<CreateAlertRequestDto>()))
            .ReturnsAsync(Failure<AlertDto>(ResultCode.ACTIVE_ALERT_LIMIT_REACHED));

        await CreateWizardHandler().HandleCallbackAsync(CallbackContext(), new CallbackData(Callbacks.Wizard, Callbacks.WizardSave), CancellationToken.None);

        _mockConversationService.Verify(s => s.ClearStateAsync(It.IsAny<long>()), Times.Never);
        _mockMessenger.Verify(m => m.AnswerCallbackAsync(CallbackQueryId, ResultMessages.Get(ResultCode.ACTIVE_ALERT_LIMIT_REACHED), true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleCallback_SaysTheStepExpired_WithoutAFlow() {
        _mockConversationService.Setup(s => s.GetStateAsync(TestData.ChatId)).ReturnsAsync((ConversationStateDto?)null);

        await CreateWizardHandler().HandleCallbackAsync(CallbackContext(), new CallbackData(Callbacks.Wizard, Callbacks.WizardSave), CancellationToken.None);

        _mockMessenger.Verify(m => m.AnswerCallbackAsync(CallbackQueryId, BotTexts.StepExpired, true, It.IsAny<CancellationToken>()), Times.Once);
        _mockAlertService.Verify(s => s.CreateAlertAsync(It.IsAny<long>(), It.IsAny<CreateAlertRequestDto>()), Times.Never);
    }

    [Fact]
    public async Task HandleCallback_Cancels() {
        await CreateWizardHandler().HandleCallbackAsync(CallbackContext(), new CallbackData(Callbacks.Wizard, Callbacks.WizardCancel), CancellationToken.None);

        _mockConversationService.Verify(s => s.ClearStateAsync(TestData.ChatId), Times.Once);
        Assert.Equal(BotTexts.WizardCancelled, Assert.Single(_editedMessages));
    }
}