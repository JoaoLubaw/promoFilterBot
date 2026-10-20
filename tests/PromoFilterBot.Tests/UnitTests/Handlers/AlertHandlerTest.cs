/*
 * File: AlertHandlerTest.cs
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
using PromoFilterBot.Infrastructure.Common;
using PromoFilterBot.Infrastructure.Dto;
using PromoFilterBot.Tests.Helpers;

namespace PromoFilterBot.Tests.Handlers;

public class AlertHandlerTest : HandlerTestBase {

    [Fact]
    public async Task HandleAdd_CreatesTheAlertTypedInOneLine() {
        _mockAlertService
            .Setup(s => s.CreateAlertAsync(TestData.ChatId, It.Is<CreateAlertRequestDto>(r => r.AlertText == "rtx 4060" && r.AlertMaxPrice == 2000)))
            .ReturnsAsync(Success(AlertDto()));

        await CreateAlertHandler().HandleAddAsync(MessageContext(), "rtx 4060 max 2000", CancellationToken.None);

        Assert.Contains("Alerta nº 1 criado", Assert.Single(_sentMessages));
    }

    [Fact]
    public async Task HandleAdd_ExplainsTheError_OfTheService() {
        _mockAlertService.Setup(s => s.CreateAlertAsync(TestData.ChatId, It.IsAny<CreateAlertRequestDto>()))
            .ReturnsAsync(Failure<AlertDto>(ResultCode.ACTIVE_ALERT_LIMIT_REACHED));

        await CreateAlertHandler().HandleAddAsync(MessageContext(), "rtx 4060", CancellationToken.None);

        Assert.Contains("máximo de alertas ativos", Assert.Single(_sentMessages));
    }

    [Fact]
    public async Task HandleAdd_RefusesALimitThatIsNotAnAmount_WithoutCallingTheService() {
        await CreateAlertHandler().HandleAddAsync(MessageContext(), "ssd max 1,2,3", CancellationToken.None);

        Assert.Contains(Html.Escape(ResultMessages.Get(ResultCode.INVALID_MAX_PRICE)), Assert.Single(_sentMessages));
        _mockAlertService.Verify(s => s.CreateAlertAsync(It.IsAny<long>(), It.IsAny<CreateAlertRequestDto>()), Times.Never);
    }

    [Fact]
    public async Task HandleAdd_TurnsAnUnexpectedFailureIntoAHandledError() {
        _mockAlertService.Setup(s => s.CreateAlertAsync(TestData.ChatId, It.IsAny<CreateAlertRequestDto>())).ThrowsAsync(new InvalidOperationException("db down"));

        await CreateAlertHandler().HandleAddAsync(MessageContext(), "rtx 4060", CancellationToken.None);

        Assert.Contains("Algo deu errado", Assert.Single(_sentMessages));
    }

    [Fact]
    public async Task HandleList_ShowsTheAlertsAndThePause() {
        _mockAlertService.Setup(s => s.GetAlertsAsync(TestData.ChatId))
            .ReturnsAsync(Success(new List<AlertDto> { AlertDto(), AlertDto(11, 2, "mouse g305", isActive: false, maxPrice: null) }));

        await CreateAlertHandler().HandleListAsync(MessageContext(TestData.UserDto(pausedUntil: TestData.Now.AddHours(5))), CancellationToken.None);

        string message = Assert.Single(_sentMessages);
        Assert.Contains("1. 🟢 <b>rtx 4060</b>", message);
        Assert.Contains("2. 🔕 <b>mouse g305</b>", message);
        Assert.Contains("pausados até 21/10 às 08:00", message);
    }

    [Fact]
    public async Task HandleList_InvitesToCreate_WithoutAlerts() {
        _mockAlertService.Setup(s => s.GetAlertsAsync(TestData.ChatId)).ReturnsAsync(Success(new List<AlertDto>()));

        await CreateAlertHandler().HandleListAsync(MessageContext(), CancellationToken.None);

        Assert.Equal(BotTexts.NoAlerts, Assert.Single(_sentMessages));
    }

    [Fact]
    public async Task HandleRemove_RefusesANumberThatCannotBeRead() {
        await CreateAlertHandler().HandleRemoveAsync(MessageContext(), "três", CancellationToken.None);

        Assert.Contains("Não existe alerta com esse número", Assert.Single(_sentMessages));
    }

    [Fact]
    public async Task HandleRemove_DeletesTheAlertOfTheNumber() {
        _mockAlertService.Setup(s => s.DeleteAlertByPositionAsync(TestData.ChatId, 2)).ReturnsAsync(Success(AlertDto(11, 2, "mouse")));

        await CreateAlertHandler().HandleRemoveAsync(MessageContext(), "#2", CancellationToken.None);

        Assert.Contains("Alerta nº 2 excluído: mouse", Assert.Single(_sentMessages));
    }

    [Fact]
    public async Task HandleCallback_PausesTheAlertAndShowsTheListInPlace() {
        _mockAlertService.Setup(s => s.ToggleAlertAsync(TestData.ChatId, 10)).ReturnsAsync(Success(AlertDto(isActive: false)));
        _mockAlertService.Setup(s => s.GetAlertsAsync(TestData.ChatId)).ReturnsAsync(Success(new List<AlertDto> { AlertDto(isActive: false) }));
        BotContext context = CallbackContext();

        await CreateAlertHandler().HandleCallbackAsync(context, new CallbackData(Callbacks.Alert, Callbacks.AlertToggle, "10"), CancellationToken.None);

        _mockMessenger.Verify(m => m.AnswerCallbackAsync(CallbackQueryId, "⏸ Alerta pausado", false, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Contains("Seus alertas", Assert.Single(_editedMessages));
        Assert.True(context.CallbackAnswered);
    }

    [Fact]
    public async Task HandleCallback_ShowsTheErrorInAPopup() {
        _mockAlertService.Setup(s => s.DeleteAlertAsync(TestData.ChatId, 10)).ReturnsAsync(Failure<AlertDto>(ResultCode.ALERT_NOT_FOUND));

        await CreateAlertHandler().HandleCallbackAsync(CallbackContext(), new CallbackData(Callbacks.Alert, Callbacks.AlertDelete, "10"), CancellationToken.None);

        _mockMessenger.Verify(m => m.AnswerCallbackAsync(CallbackQueryId, ResultMessages.Get(ResultCode.ALERT_NOT_FOUND), true, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Empty(_editedMessages);
    }
}