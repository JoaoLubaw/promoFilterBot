/*
 * File: AlertServiceTest.cs
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

using PromoFilterBot.Domain.Dto;
using PromoFilterBot.Domain.Models;
using PromoFilterBot.Infrastructure.Common;
using PromoFilterBot.Infrastructure.Dto;
using PromoFilterBot.Infrastructure.Interfaces;
using PromoFilterBot.Infrastructure.Options;
using PromoFilterBot.Infrastructure.Services;
using PromoFilterBot.Tests.Helpers;

using MsOptions = Microsoft.Extensions.Options.Options;

namespace PromoFilterBot.Tests.Services;

public class AlertServiceTest {
    private readonly Mock<IAlertRepository> _mockAlertRepository = new();
    private readonly Mock<IUserRepository> _mockUserRepository = new();
    private readonly Mock<IAlertIndexProvider> _mockAlertIndexProvider = new();
    private readonly Mock<ILogger<AlertService>> _mockLogger = new();
    private readonly LimitsOptions _limits = new LimitsOptions { MaxActiveAlertsPerUser = 2, MaxAlertsPerUser = 3, MaxAlertTextLength = 50 };
    private readonly AlertService _service;
    private readonly User _user = TestData.User();

    public AlertServiceTest() {
        _service = new AlertService(
            _mockAlertRepository.Object,
            _mockUserRepository.Object,
            _mockAlertIndexProvider.Object,
            MsOptions.Create(_limits),
            _mockLogger.Object);

        _mockUserRepository.Setup(r => r.GetUserByChatIdAsync(TestData.ChatId)).ReturnsAsync(_user);
    }

    private static CreateAlertRequestDto Request(string text, decimal? maxPrice = null) {
        return new CreateAlertRequestDto { AlertText = text, AlertMaxPrice = maxPrice };
    }

    [Fact]
    public async Task CreateAlert_ReturnsUnregistered_WhenUserNotFound() {
        _mockUserRepository.Setup(r => r.GetUserByChatIdAsync(TestData.ChatId)).ReturnsAsync((User?)null);

        ServiceResult<AlertDto> result = await _service.CreateAlertAsync(TestData.ChatId, Request("rtx 4060"));

        Assert.Equal(ResultCode.UNREGISTERED_USER, result.Code);
    }

    [Fact]
    public async Task CreateAlert_ReturnsEmptyRequest_WhenRequestNull() {
        ServiceResult<AlertDto> result = await _service.CreateAlertAsync(TestData.ChatId, null!);

        Assert.Equal(ResultCode.EMPTY_REQUEST, result.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("de a")]
    [InlineData("🔥🔥")]
    public async Task CreateAlert_ReturnsInvalidText_WithoutATermToLookFor(string text) {
        ServiceResult<AlertDto> result = await _service.CreateAlertAsync(TestData.ChatId, Request(text));

        Assert.Equal(ResultCode.INVALID_ALERT_TEXT, result.Code);
    }

    [Fact]
    public async Task CreateAlert_ReturnsTooLong_AboveTheLimitOfCharacters() {
        ServiceResult<AlertDto> result = await _service.CreateAlertAsync(TestData.ChatId, Request(new string('a', 51)));

        Assert.Equal(ResultCode.ALERT_TEXT_TOO_LONG, result.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(2_000_000)]
    public async Task CreateAlert_ReturnsInvalidMaxPrice_OutOfRange(double maxPrice) {
        ServiceResult<AlertDto> result = await _service.CreateAlertAsync(TestData.ChatId, Request("rtx 4060", (decimal)maxPrice));

        Assert.Equal(ResultCode.INVALID_MAX_PRICE, result.Code);
    }

    [Fact]
    public async Task CreateAlert_ReturnsAlertLimit_WhenTheUserHasTooManyAlerts() {
        _mockAlertRepository.Setup(r => r.CountAlertsAsync(_user.UserId)).ReturnsAsync(3);

        ServiceResult<AlertDto> result = await _service.CreateAlertAsync(TestData.ChatId, Request("rtx 4060"));

        Assert.Equal(ResultCode.ALERT_LIMIT_REACHED, result.Code);
    }

    [Fact]
    public async Task CreateAlert_ReturnsActiveAlertLimit_WhenTheUserHasTooManyActiveAlerts() {
        _mockAlertRepository.Setup(r => r.CountAlertsAsync(_user.UserId)).ReturnsAsync(2);
        _mockAlertRepository.Setup(r => r.CountActiveAlertsAsync(_user.UserId)).ReturnsAsync(2);

        ServiceResult<AlertDto> result = await _service.CreateAlertAsync(TestData.ChatId, Request("rtx 4060"));

        Assert.Equal(ResultCode.ACTIVE_ALERT_LIMIT_REACHED, result.Code);
    }

    [Fact]
    public async Task CreateAlert_SavesTheTermsAndRebuildsTheIndex() {
        Alert created = TestData.Alert(10, _user.UserId, "ssd 1tb -sata", 400, true);
        AlertQuery? savedQuery = null;

        _mockAlertRepository
            .Setup(r => r.CreateAlertAsync(_user.UserId, "ssd 1tb -sata", It.IsAny<AlertQuery>(), 400, "chat:1001"))
            .Callback((int _, string _, AlertQuery query, decimal? _, string _) => savedQuery = query)
            .ReturnsAsync(created);
        _mockAlertRepository.Setup(r => r.GetUserAlertsAsync(_user.UserId)).ReturnsAsync([TestData.Alert(9, _user.UserId, "mouse"), created]);

        ServiceResult<AlertDto> result = await _service.CreateAlertAsync(TestData.ChatId, Request(" ssd 1tb -sata ", 400));

        Assert.Equal(ResultCode.NO_ERROR, result.Code);
        Assert.Equal(2, result.Data!.AlertPosition);
        Assert.Equal("ssd · \"1 tb\" · sem: sata", result.Data.AlertDescription);
        Assert.Equal("ssd|1 tb", savedQuery!.StoredRequiredTerms);
        _mockAlertIndexProvider.Verify(p => p.Invalidate(), Times.Once);
    }

    [Fact]
    public async Task GetAlerts_NumbersTheAlertsInOrder() {
        _mockAlertRepository.Setup(r => r.GetUserAlertsAsync(_user.UserId))
            .ReturnsAsync([TestData.Alert(5, _user.UserId, "mouse"), TestData.Alert(8, _user.UserId, "teclado")]);

        ServiceResult<List<AlertDto>> result = await _service.GetAlertsAsync(TestData.ChatId);

        Assert.Equal([1, 2], result.Data!.Select(a => a.AlertPosition));
        Assert.Equal([5, 8], result.Data!.Select(a => a.AlertId));
    }

    [Fact]
    public async Task ToggleAlert_ReturnsNotFound_ForTheAlertOfSomeoneElse() {
        _mockAlertRepository.Setup(r => r.GetUserAlertsAsync(_user.UserId)).ReturnsAsync([TestData.Alert(5, _user.UserId, "mouse")]);

        ServiceResult<AlertDto> result = await _service.ToggleAlertAsync(TestData.ChatId, 99);

        Assert.Equal(ResultCode.ALERT_NOT_FOUND, result.Code);
    }

    [Fact]
    public async Task ToggleAlert_ReturnsActiveLimit_WhenResumingAboveTheLimit() {
        _mockAlertRepository.Setup(r => r.GetUserAlertsAsync(_user.UserId)).ReturnsAsync([TestData.Alert(5, _user.UserId, "mouse", isActive: false)]);
        _mockAlertRepository.Setup(r => r.CountActiveAlertsAsync(_user.UserId)).ReturnsAsync(2);

        ServiceResult<AlertDto> result = await _service.ToggleAlertAsync(TestData.ChatId, 5);

        Assert.Equal(ResultCode.ACTIVE_ALERT_LIMIT_REACHED, result.Code);
    }

    [Fact]
    public async Task ToggleAlert_PausesAnActiveAlert() {
        Alert alert = TestData.Alert(5, _user.UserId, "mouse");
        _mockAlertRepository.Setup(r => r.GetUserAlertsAsync(_user.UserId)).ReturnsAsync([alert]);
        _mockAlertRepository.Setup(r => r.SetAlertActiveAsync(alert, false, It.IsAny<string>()))
            .ReturnsAsync(() => { alert.AlertIsActive = false; return alert; });

        ServiceResult<AlertDto> result = await _service.ToggleAlertAsync(TestData.ChatId, 5);

        Assert.False(result.Data!.AlertIsActive);
        _mockAlertIndexProvider.Verify(p => p.Invalidate(), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public async Task DeleteAlertByPosition_ReturnsInvalidPosition_OutOfTheList(int position) {
        _mockAlertRepository.Setup(r => r.GetUserAlertsAsync(_user.UserId))
            .ReturnsAsync([TestData.Alert(5, _user.UserId, "mouse"), TestData.Alert(8, _user.UserId, "teclado")]);

        ServiceResult<AlertDto> result = await _service.DeleteAlertByPositionAsync(TestData.ChatId, position);

        Assert.Equal(ResultCode.INVALID_ALERT_POSITION, result.Code);
    }

    [Fact]
    public async Task DeleteAlertByPosition_DeletesTheAlertOfThatNumber() {
        Alert second = TestData.Alert(8, _user.UserId, "teclado");
        _mockAlertRepository.Setup(r => r.GetUserAlertsAsync(_user.UserId)).ReturnsAsync([TestData.Alert(5, _user.UserId, "mouse"), second]);

        ServiceResult<AlertDto> result = await _service.DeleteAlertByPositionAsync(TestData.ChatId, 2);

        Assert.Equal("teclado", result.Data!.AlertOriginalText);
        _mockAlertRepository.Verify(r => r.DeleteAlertAsync(second, "chat:1001"), Times.Once);
    }

    [Fact]
    public async Task EditAlert_ReplacesTheAlert() {
        Alert alert = TestData.Alert(5, _user.UserId, "mouse");
        _mockAlertRepository.Setup(r => r.GetUserAlertsAsync(_user.UserId)).ReturnsAsync([alert]);
        _mockAlertRepository
            .Setup(r => r.EditAlertAsync(alert, "mouse g305", It.IsAny<AlertQuery>(), 150, It.IsAny<string>()))
            .ReturnsAsync(TestData.Alert(5, _user.UserId, "mouse g305", 150));

        EditAlertRequestDto request = new EditAlertRequestDto { AlertText = "mouse g305", AlertMaxPrice = 150 };
        ServiceResult<AlertDto> result = await _service.EditAlertAsync(TestData.ChatId, 5, request);

        Assert.Equal("mouse g305", result.Data!.AlertOriginalText);
        Assert.Equal(1, result.Data.AlertPosition);
        _mockAlertIndexProvider.Verify(p => p.Invalidate(), Times.Once);
    }
}