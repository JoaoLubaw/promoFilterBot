/*
 * File: Keyboards.cs
 * Project: Common
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

using PromoFilterBot.Infrastructure.Dto;

using Telegram.Bot.Types.ReplyMarkups;

namespace PromoFilterBot.Bot.Common;

/// <summary>
/// Inline keyboards of the bot.
/// </summary>
public static class Keyboards {
    private static readonly decimal[] SuggestedPrices = [100m, 300m, 1000m];

    public static InlineKeyboardMarkup MainMenu() {
        return Markup(
            [Button("➕ Novo alerta", Callbacks.Menu, Callbacks.MenuNewAlert), Button("🔔 Meus alertas", Callbacks.Menu, Callbacks.MenuAlerts)],
            [Button("⏸ Pausar", Callbacks.Menu, Callbacks.MenuPause), Button("❓ Ajuda", Callbacks.Menu, Callbacks.MenuHelp)]);
    }

    public static InlineKeyboardMarkup NewAlert() {
        return Markup([Button("➕ Novo alerta", Callbacks.Menu, Callbacks.MenuNewAlert)]);
    }

    public static InlineKeyboardMarkup AfterAlertSaved() {
        return Markup([Button("🔔 Meus alertas", Callbacks.Menu, Callbacks.MenuAlerts), Button("➕ Outro alerta", Callbacks.Menu, Callbacks.MenuNewAlert)]);
    }

    public static InlineKeyboardMarkup AlertList(IEnumerable<AlertDto> alerts) {
        List<InlineKeyboardButton[]> rows = alerts.Select(alert => new[] {
            Button(alert.AlertIsActive ? $"🔕 {alert.AlertPosition}" : $"🔔 {alert.AlertPosition}", Callbacks.Alert, Callbacks.AlertToggle, alert.AlertId),
            Button($"✏️ {alert.AlertPosition}", Callbacks.Alert, Callbacks.AlertEdit, alert.AlertId),
            Button($"🗑 {alert.AlertPosition}", Callbacks.Alert, Callbacks.AlertDelete, alert.AlertId)
        }).ToList();

        rows.Add([Button("➕ Novo alerta", Callbacks.Menu, Callbacks.MenuNewAlert)]);

        return new InlineKeyboardMarkup(rows);
    }

    public static InlineKeyboardMarkup AlertRemoval(IEnumerable<AlertDto> alerts) {
        List<InlineKeyboardButton[]> rows = alerts
            .Select(alert => new[] { Button($"🗑 {alert.AlertPosition}. {Html.Truncate(alert.AlertOriginalText, 40)}", Callbacks.Alert, Callbacks.AlertDelete, alert.AlertId) })
            .ToList();

        return new InlineKeyboardMarkup(rows);
    }

    /* Guided flow */

    public static InlineKeyboardMarkup WizardText(bool canKeepText) {
        return canKeepText
            ? Markup([Button("↩️ Manter", Callbacks.Wizard, Callbacks.WizardKeepText), Cancel()])
            : Markup([Cancel()]);
    }

    public static InlineKeyboardMarkup WizardPrice(AlertDraftDto draft) {
        List<InlineKeyboardButton[]> rows = [
            [Button("Sem limite", Callbacks.Wizard, Callbacks.WizardPrice, Callbacks.PriceNone), PriceButton(SuggestedPrices[0])],
            [PriceButton(SuggestedPrices[1]), PriceButton(SuggestedPrices[2])],
            [Button("✍️ Digitar valor", Callbacks.Wizard, Callbacks.WizardPrice, Callbacks.PriceCustom)]
        ];

        if (draft.EditingAlertId.HasValue && draft.PriceChosen) {
            rows.Add([Button($"↩️ Manter ({Formatters.MaxPrice(draft.AlertMaxPrice)})", Callbacks.Wizard, Callbacks.WizardPrice, Callbacks.PriceKeep)]);
        }

        rows.Add([Cancel()]);

        return new InlineKeyboardMarkup(rows);
    }

    public static InlineKeyboardMarkup WizardConfirm() {
        return Markup(
            [Button("💾 Salvar", Callbacks.Wizard, Callbacks.WizardSave), Button("✏️ Corrigir", Callbacks.Wizard, Callbacks.WizardFix)],
            [Cancel()]);
    }

    public static InlineKeyboardMarkup CancelOnly() => Markup([Cancel()]);

    /* Pause */

    public static InlineKeyboardMarkup PauseMenu(bool isPaused) {
        List<InlineKeyboardButton[]> rows = [
            [Button("1h", Callbacks.Pause, "1h"), Button("8h", Callbacks.Pause, "8h"), Button("Até amanhã", Callbacks.Pause, "amanha")]
        ];

        if (isPaused) rows.Add([Button("▶️ Retomar", Callbacks.Pause, "retomar")]);

        return new InlineKeyboardMarkup(rows);
    }

    /* Account */

    public static InlineKeyboardMarkup DeleteAccountConfirm() {
        return Markup([
            Button("🗑 Apagar tudo", Callbacks.Account, Callbacks.AccountDeleteConfirm),
            Button("Cancelar", Callbacks.Account, Callbacks.AccountDeleteCancel)
        ]);
    }

    /* Admin */

    public static InlineKeyboardMarkup AdminMenu() {
        return Markup(
            [Button("📢 Canais", Callbacks.Admin, Callbacks.AdminChannels), Button("➕ Adicionar canais", Callbacks.Admin, Callbacks.AdminAddChannels)],
            [Button("🔄 Atualizar", Callbacks.Admin, Callbacks.AdminStats)]);
    }

    public static InlineKeyboardMarkup AdminChannels() {
        return Markup([Button("➕ Adicionar canais", Callbacks.Admin, Callbacks.AdminAddChannels), Button("⬅️ Painel", Callbacks.Admin, Callbacks.AdminStats)]);
    }

    /* Notifications */

    public static InlineKeyboardMarkup Notification(NotificationDispatchDto notification) {
        List<InlineKeyboardButton[]> rows = [];
        List<InlineKeyboardButton> links = [];

        if (notification.OfferLink != null) links.Add(InlineKeyboardButton.WithUrl("🛒 Abrir oferta", notification.OfferLink));
        if (notification.PostLink != null) links.Add(InlineKeyboardButton.WithUrl("📢 Ver no canal", notification.PostLink));
        if (links.Count > 0) rows.Add([.. links]);

        rows.Add([
            Button("🔕 Desligar alerta", Callbacks.Notification, Callbacks.NotificationTurnOffAlert, notification.AlertId),
            Button("🙅 Não era isso", Callbacks.Notification, Callbacks.NotificationNotRelevant, notification.NotificationId)
        ]);

        return new InlineKeyboardMarkup(rows);
    }

    public static InlineKeyboardMarkup EditAlert(int alertId) {
        return Markup([Button("✏️ Editar alerta", Callbacks.Alert, Callbacks.AlertEdit, alertId)]);
    }

    private static InlineKeyboardButton Button(string text, string prefix, string action, object? argument = null) {
        return InlineKeyboardButton.WithCallbackData(text, Callbacks.Build(prefix, action, argument));
    }

    private static InlineKeyboardButton PriceButton(decimal price) {
        return Button(Formatters.Price(price).Replace(",00", string.Empty), Callbacks.Wizard, Callbacks.WizardPrice, price);
    }

    private static InlineKeyboardButton Cancel() => Button("✖️ Cancelar", Callbacks.Wizard, Callbacks.WizardCancel);

    private static InlineKeyboardMarkup Markup(params InlineKeyboardButton[][] rows) => new InlineKeyboardMarkup(rows);
}