/*
 * File: ReaderLogin.cs
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

using System.Diagnostics.CodeAnalysis;
using System.Text;

using PromoFilterBot.Reader.Common;
using PromoFilterBot.Reader.Options;

using TL;

using WTelegram;

namespace PromoFilterBot.Reader.Services;

/// <summary>
/// First login of the reader account (<c>docker compose run --rm app --login</c>): asks the code Telegram sends and,
/// when there is one, the 2FA password, and saves the session the app uses from then on.
/// </summary>
[ExcludeFromCodeCoverage]
public static class ReaderLogin {
    public static async Task<int> RunInteractiveAsync(ReaderOptions options) {
        if (!options.IsConfigured) {
            Console.WriteLine("Configure Reader__ApiId, Reader__ApiHash e Reader__PhoneNumber antes do login.");
            return 1;
        }

        ReaderClientConfig.EnsureDirectories(options);
        Helpers.Log = (level, message) => {
            if (level >= 3) Console.WriteLine(message);
        };

        await using Client client = new Client(ReaderClientConfig.Create(options, Ask));

        User user = await client.LoginUserIfNeeded();

        Console.WriteLine($"Conta leitora logada como {user.first_name} {user.last_name} (id {user.id}).");
        Console.WriteLine($"Sessão salva em {Path.GetFullPath(options.SessionPath)}. Agora suba o app normalmente.");

        return 0;
    }

    private static string? Ask(string what) {
        switch (what) {
            case "verification_code":
                return Prompt("Código recebido no Telegram: ");
            case "password":
                return PromptSecret("Senha da verificação em duas etapas: ");
            case "email":
                return Prompt("E-mail pedido pelo Telegram: ");
            case "email_verification_code":
                return Prompt("Código recebido no e-mail: ");
            default:
                // first_name / last_name: the number has no account yet, and the reader account must already exist
                throw new InvalidOperationException("Crie e use a conta no aplicativo do Telegram antes de logar o leitor.");
        }
    }

    private static string? Prompt(string question) {
        Console.Write(question);
        return Console.ReadLine()?.Trim();
    }

    private static string? PromptSecret(string question) {
        Console.Write(question);

        if (Console.IsInputRedirected) return Console.ReadLine();

        StringBuilder secret = new StringBuilder();

        while (true) {
            ConsoleKeyInfo key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Enter) break;

            if (key.Key == ConsoleKey.Backspace) {
                if (secret.Length > 0) secret.Length--;
            }
            else {
                secret.Append(key.KeyChar);
            }
        }

        Console.WriteLine();
        return secret.ToString();
    }
}