/*
 * File: ResultMessages.cs
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

using PromoFilterBot.Infrastructure.Common;

namespace PromoFilterBot.Bot.Common;

/// <summary>
/// What the user reads for each result code of the services.
/// </summary>
public static class ResultMessages {
    private static readonly Dictionary<ResultCode, string> Messages = new() {
        [ResultCode.UNREGISTERED_USER] = "Não encontrei seu cadastro. Envie /start para começar.",
        [ResultCode.NOT_ADMIN] = "Esse comando é só para o administrador do bot.",
        [ResultCode.EMPTY_REQUEST] = "Não recebi nada para salvar.",
        [ResultCode.INVALID_ALERT_TEXT] = "Não entendi o que procurar. Escreva ao menos uma palavra do produto, por exemplo: rtx 4060",
        [ResultCode.ALERT_TEXT_TOO_LONG] = "Esse alerta ficou longo demais. Use só as palavras principais do produto.",
        [ResultCode.INVALID_MAX_PRICE] = "Esse preço máximo não é válido. Use um valor como 1500 ou 1.499,90.",
        [ResultCode.ACTIVE_ALERT_LIMIT_REACHED] = "Você já tem o máximo de alertas ativos. Pause ou exclua um em /alertas.",
        [ResultCode.ALERT_LIMIT_REACHED] = "Você chegou ao limite de alertas. Exclua algum em /alertas.",
        [ResultCode.INVALID_CHANNEL_REFERENCE] = "Não reconheci o canal. Envie o @canal ou o link t.me/canal de um canal público.",
        [ResultCode.PRIVATE_CHANNEL_LINK] = "Esse é o link de um canal privado. Só leio canais públicos (com @canal).",
        [ResultCode.INVALID_PAUSE_DURATION] = "Não entendi por quanto tempo. Exemplos: /pausar 1h, /pausar 8h, /pausar amanhã, /pausar retomar",
        [ResultCode.INVALID_ALERT_POSITION] = "Não existe alerta com esse número. Veja a lista em /alertas.",
        [ResultCode.ALERT_NOT_FOUND] = "Esse alerta não existe mais.",
        [ResultCode.CHANNEL_NOT_FOUND] = "Não encontrei esse canal.",
        [ResultCode.CHANNEL_ALREADY_MONITORED] = "Esse canal já está na lista (lendo ou entrando nele).",
        [ResultCode.NOTIFICATION_NOT_FOUND] = "Essa oferta não está mais disponível."
    };

    public static string Get(ResultCode code) {
        return Messages.TryGetValue(code, out string? message) ? message : "Algo deu errado aqui. Tente de novo em instantes.";
    }
}