/*
 * File: BotTexts.cs
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

using System.Text;

using PromoFilterBot.Domain.Models;
using PromoFilterBot.Infrastructure.Common;
using PromoFilterBot.Infrastructure.Dto;

using Telegram.Bot.Types;

namespace PromoFilterBot.Bot.Common;

/// <summary>
/// Texts of the bot, in HTML. Everything that comes from a user or a channel is escaped.
/// </summary>
public static class BotTexts {
    private const int MaxListedChannels = 100;

    public static readonly IReadOnlyList<BotCommand> Commands = [
        new BotCommand("adicionar", "Criar um alerta"),
        new BotCommand("alertas", "Ver, desligar, editar ou excluir alertas"),
        new BotCommand("remover", "Excluir um alerta pelo número"),
        new BotCommand("pausar", "Pausar os avisos por um tempo"),
        new BotCommand("ajuda", "Como escrever a busca"),
        new BotCommand("apagarconta", "Apagar todos os seus dados"),
        new BotCommand("cancelar", "Sair do passo a passo")
    ];

    public const string AskAlertText =
        "🔎 <b>O que você quer acompanhar?</b>\n\n" +
        "Escreva o produto, por exemplo: <code>mouse logitech g305</code>\n" +
        "Dicas: <code>\"air fryer\"</code> busca a frase exata e <code>-usado</code> exclui uma palavra.";

    public const string AskCustomPrice = "💰 Digite o preço máximo, por exemplo: <code>1500</code> ou <code>1.499,90</code>";

    public const string UseButtons = "👆 Use os botões da mensagem acima, ou /cancelar para sair do passo a passo.";

    public const string StepExpired = "Esse passo expirou. Use /adicionar para começar de novo.";

    public const string WizardCancelled = "Ok, cancelei. Nada foi salvo.";

    public const string CancelCommand = "Pronto, nada em andamento. Veja o menu abaixo 👇";

    public const string NoAlerts = "Você ainda não tem alertas. Crie o primeiro 👇";

    public const string UnknownCommand = "Não conheço esse comando. Veja /ajuda.";

    public const string NonTextMessage = "Por enquanto eu só entendo mensagens de texto 🙂";

    public const string ConfirmDeleteAccount =
        "🗑 <b>Apagar sua conta?</b>\n\nVou apagar seus alertas, o histórico de avisos e seu cadastro. Não dá para desfazer.";

    public const string AccountDeleted = "🗑 Pronto, apaguei seus alertas e seus dados. Se quiser voltar, é só mandar /start.";

    public const string AccountDeleteCancelled = "Ufa! Nada foi apagado 🙂";

    public const string Resumed = "▶️ Avisos retomados! Você volta a receber as ofertas dos seus alertas.";

    public const string AlertTurnedOffFromNotification = "🔕 Alerta desligado. Para religar, use /alertas.";

    public const string FeedbackThanks = "Obrigado pelo retorno!";

    public static string Welcome(string name) {
        return
            $"👋 Olá, {Html.Escape(name)}!\n\n" +
            "Eu acompanho canais de promoção do Telegram e, assim que sai uma oferta do que você procura, até o preço " +
            "que você definir, mando ela inteira aqui no privado, com o cupom e o link.\n\n" +
            "🔒 <b>Privacidade:</b> guardo só o seu chat do Telegram, seu nome de exibição e os alertas que você criar. " +
            "Para apagar tudo, use /apagarconta.\n\n" +
            "Comece criando um alerta 👇";
    }

    public static string Help(bool isAdmin) {
        StringBuilder help = new StringBuilder()
            .AppendLine("❓ <b>Como usar</b>")
            .AppendLine()
            .AppendLine("<b>Criar alerta</b>")
            .AppendLine("Toque em Novo alerta, ou digite tudo de uma vez:")
            .AppendLine("<code>/adicionar rtx 4060 max 2000</code>")
            .AppendLine()
            .AppendLine("<b>Como escrever a busca</b>")
            .AppendLine("• <code>rtx 4060</code> → a oferta tem \"rtx\" e \"4060\", em qualquer ordem")
            .AppendLine("• <code>\"air fryer\"</code> → a frase exata, entre aspas")
            .AppendLine("• <code>ssd 1tb -sata</code> → tem ssd e 1tb, mas não sata")
            .AppendLine("Maiúsculas, acentos e emojis não fazem diferença.")
            .AppendLine()
            .AppendLine("<b>O que chega</b>")
            .AppendLine("A oferta inteira, assim que sai no canal. Ela chega uma vez por dia; se o preço, o cupom ou a loja " +
                "mudar, chega de novo. Os avisos seguem até você desligar o alerta.")
            .AppendLine()
            .AppendLine("<b>Comandos</b>")
            .AppendLine("/adicionar — novo alerta")
            .AppendLine("/alertas — seus alertas (desligar, editar, excluir)")
            .AppendLine("/remover 3 — exclui o alerta nº 3")
            .AppendLine("/pausar 8h — pausa os avisos (30m, 1h, 8h, amanhã, retomar)")
            .AppendLine("/apagarconta — apaga todos os seus dados")
            .Append("/cancelar — sai do passo a passo");

        if (isAdmin) {
            help.AppendLine()
                .AppendLine()
                .AppendLine("<b>Administração</b>")
                .AppendLine("/admin — painel")
                .AppendLine("/admin adicionar @canal1 @canal2 — canais para ler (vários de uma vez)")
                .AppendLine("/admin remover @canal — para de ler um canal")
                .Append("/admin canais — todos os canais");
        }

        return help.ToString();
    }

    public static string UnknownText(string text) {
        return
            "Não entendi 🤔\n\n" +
            $"Para criar um alerta com esse texto, envie <code>/adicionar {Html.Escape(Html.Truncate(text, 60))}</code> " +
            "ou toque em Novo alerta.";
    }

    /* Alerts */

    public static string AlertSaved(AlertDto alert, bool edited) {
        string title = edited ? $"✅ <b>Alerta nº {alert.AlertPosition} atualizado!</b>" : $"✅ <b>Alerta nº {alert.AlertPosition} criado!</b>";

        return $"{title}\n\n{AlertDetails(alert)}\n\nAssim que sair uma oferta, mando aqui.";
    }

    public static string AlertDeleted(AlertDto alert) => $"🗑 Alerta nº {alert.AlertPosition} excluído: {Html.Escape(alert.AlertOriginalText)}";

    public static string AlertList(IReadOnlyList<AlertDto> alerts, string? pausedUntil) {
        StringBuilder list = new StringBuilder("🔔 <b>Seus alertas</b>\n");

        if (pausedUntil != null) list.Append($"⏸ <i>Seus avisos estão pausados até {pausedUntil}.</i>\n");

        foreach (AlertDto alert in alerts) {
            string status = alert.AlertIsActive ? "🟢" : "🔕";
            list.Append($"\n{alert.AlertPosition}. {status} <b>{Html.Escape(alert.AlertOriginalText)}</b>\n");
            list.Append($"    {Formatters.MaxPrice(alert.AlertMaxPrice)}\n");
        }

        list.Append("\nUse os botões: 🔕/🔔 desligar ou religar · ✏️ editar · 🗑 excluir.");

        return list.ToString();
    }

    public static string AlertRemovalList(IReadOnlyList<AlertDto> alerts) {
        StringBuilder list = new StringBuilder("🗑 <b>Qual alerta você quer excluir?</b>\n");

        foreach (AlertDto alert in alerts) list.Append($"\n{alert.AlertPosition}. {Html.Escape(alert.AlertOriginalText)}");

        return list.ToString();
    }

    private static string AlertDetails(AlertDto alert) {
        return
            $"🔎 <b>{Html.Escape(alert.AlertOriginalText)}</b>\n" +
            $"    <i>{Html.Escape(alert.AlertDescription)}</i>\n" +
            $"💰 {Formatters.MaxPrice(alert.AlertMaxPrice)}";
    }

    /* Guided flow */

    public static string EditAlertText(string currentText) {
        return $"✏️ <b>Editando o alerta</b>\n\nBusca atual: <b>{Html.Escape(currentText)}</b>\n\nEnvie a nova busca, ou toque em Manter.";
    }

    public static string AskMaxPrice(AlertDraftDto draft) {
        string current = draft.EditingAlertId.HasValue && draft.PriceChosen ? $"\nAtual: {Formatters.MaxPrice(draft.AlertMaxPrice)}." : string.Empty;

        return $"🔎 <b>{Html.Escape(draft.AlertText)}</b>\n\n💰 <b>Até quanto você quer pagar?</b>{current}";
    }

    public static string AlertSummary(AlertDraftDto draft) {
        string title = draft.EditingAlertId.HasValue ? "📝 <b>Resumo da alteração</b>" : "📝 <b>Resumo do alerta</b>";

        return
            $"{title}\n\n" +
            $"🔎 Busca: <b>{Html.Escape(draft.AlertText)}</b>\n" +
            $"💰 Preço máximo: {Formatters.MaxPrice(draft.AlertMaxPrice)}\n\n" +
            "Posso salvar?";
    }

    /* Pause */

    public static string PauseMenu(string? pausedUntil) {
        string status = pausedUntil == null ? "Seus avisos estão ligados." : $"Seus avisos estão pausados até {pausedUntil}.";

        return $"⏸ <b>Pausar avisos</b>\n\n{status}\nPor quanto tempo quer pausar?";
    }

    public static string Paused(string pausedUntil) => $"⏸ Avisos pausados até {pausedUntil}.\nPara voltar antes: /pausar retomar";

    /* Notifications */

    public static string FeedbackTip(string alertText) {
        return
            $"🙅 Anotado! Para o alerta <b>{Html.Escape(alertText)}</b> errar menos, edite a busca: " +
            "use <code>-palavra</code> para excluir o que não interessa ou <code>\"frase exata\"</code> entre aspas.";
    }

    /* Admin */

    public const string AskChannelsToAdd =
        "📢 <b>Quais canais devo ler?</b>\n\n" +
        "Envie um ou vários canais públicos de promoção, separados por espaço ou um por linha:\n" +
        "<code>@canal1 @canal2</code> ou <code>t.me/canal</code>";

    public static string AdminStats(AdminStatsDto stats, Func<DateTime, string> formatDate) {
        int Channels(ChannelStatus status) => stats.ChannelsByStatus.GetValueOrDefault(status);

        StringBuilder text = new StringBuilder("🛠 <b>Painel do administrador</b>\n\n")
            .AppendLine($"👥 Usuários: {stats.TotalUsers} (ativos {stats.ActiveUsers} · pausados {stats.PausedUsers})")
            .AppendLine($"🔔 Alertas ativos: {stats.ActiveAlerts}")
            .AppendLine($"📢 Canais: {Channels(ChannelStatus.Active)} lendo · {Channels(ChannelStatus.Approved)} na fila · " +
                $"{Channels(ChannelStatus.Blocked)} removidos")
            .AppendLine($"📨 Últimas 24h: {stats.MessagesLast24Hours} mensagens lidas · {stats.NotificationsSentLast24Hours} avisos enviados")
            .AppendLine($"⏳ Avisos na fila: {stats.PendingNotifications}")
            .AppendLine($"🙅 \"Não era isso\" (7 dias): {stats.NotRelevantFeedbackLast7Days}")
            .AppendLine();

        ReaderStatusDto reader = stats.Reader;

        if (!reader.IsEnabled) {
            text.AppendLine("📖 Conta leitora: ⚪ desligada (Reader:Enabled ou credenciais vazias)");
        }
        else if (reader.IsConnected) {
            string since = reader.ConnectedSince.HasValue ? $" desde {formatDate(reader.ConnectedSince.Value)}" : string.Empty;
            text.AppendLine($"📖 Conta leitora: 🟢 conectada como {Html.Escape(reader.AccountName)}{since}");
        }
        else {
            string since = reader.DisconnectedSince.HasValue ? $" desde {formatDate(reader.DisconnectedSince.Value)}" : string.Empty;
            string state = reader.LoginRequired ? "🔐 precisa de login (--login)" : "🔴 desconectada";
            text.AppendLine($"📖 Conta leitora: {state}{since}");

            if (!string.IsNullOrWhiteSpace(reader.LastError)) text.AppendLine($"    Erro: {Html.Escape(Html.Truncate(reader.LastError, 200))}");
        }

        DateTime? lastMessage = reader.LastMessageAt ?? stats.LastMessageAt;
        text.AppendLine($"    Última mensagem lida: {(lastMessage.HasValue ? formatDate(lastMessage.Value) : "nenhuma ainda")}");

        if (reader.LastFloodWaitSeconds.HasValue && reader.LastFloodWaitAt.HasValue) {
            text.AppendLine($"    Último FloodWait: {reader.LastFloodWaitSeconds}s em {formatDate(reader.LastFloodWaitAt.Value)}");
        }

        return text.ToString().TrimEnd();
    }

    public static string AdminChannelList(IReadOnlyList<ChannelDto> channels) {
        if (channels.Count == 0) return "📢 <b>Canais</b>\n\nNenhum canal ainda. Toque em Adicionar canais.";

        StringBuilder list = new StringBuilder($"📢 <b>Canais ({channels.Count})</b>\n");

        foreach (ChannelDto channel in channels.Take(MaxListedChannels)) {
            list.Append($"\n{StatusIcon(channel.ChannelStatus)} {ChannelName(channel)}");

            if (!string.IsNullOrWhiteSpace(channel.ChannelStatusReason)) list.Append($" — <i>{Html.Escape(channel.ChannelStatusReason)}</i>");
        }

        if (channels.Count > MaxListedChannels) list.Append($"\n\n… e mais {channels.Count - MaxListedChannels}.");

        list.Append("\n\n🟢 lendo · 🕓 na fila da conta leitora · ⛔ removido");

        return list.ToString();
    }

    public static string ChannelAdditions(IReadOnlyList<ChannelAdditionDto> additions) {
        StringBuilder text = new StringBuilder("📢 <b>Canais</b>\n");

        foreach (ChannelAdditionDto addition in additions) {
            string name = addition.Channel == null ? Html.Escape(Html.Truncate(addition.Reference, 60)) : $"@{Html.Escape(addition.Channel.ChannelUsername)}";

            string outcome = addition.Code switch {
                ResultCode.NO_ERROR => $"✅ {name} — na fila da conta leitora",
                ResultCode.CHANNEL_ALREADY_MONITORED => $"ℹ️ {name} — já estava na lista",
                ResultCode.PRIVATE_CHANNEL_LINK => $"🔒 {name} — link de canal privado; só leio canais públicos",
                _ => $"❌ {name} — não reconheci (use @canal ou t.me/canal)"
            };

            text.Append($"\n{outcome}");
        }

        if (additions.Any(a => a.IsAdded)) {
            text.Append(
                "\n\nNos canais que a sua conta já segue, a leitura começa em minutos. Nos outros, ela entra aos poucos " +
                "(um a cada ~20 min, até 10 por dia), silenciados e arquivados.");
        }

        return text.ToString();
    }

    public static string ChannelRemoved(ChannelDto channel) {
        return $"⛔ Parei de ler @{Html.Escape(channel.ChannelUsername)}. Sua conta continua no canal; se quiser, saia por lá.";
    }

    public static string ChannelName(ChannelDto channel) {
        string username = $"@{Html.Escape(channel.ChannelUsername)}";

        return string.IsNullOrWhiteSpace(channel.ChannelTitle) ? username : $"{Html.Escape(channel.ChannelTitle)} ({username})";
    }

    private static string StatusIcon(ChannelStatus status) => status switch {
        ChannelStatus.Active => "🟢",
        ChannelStatus.Approved => "🕓",
        _ => "⛔"
    };
}