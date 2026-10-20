/*
 * File: CouponExtractor.cs
 * Project: Common
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

using System.Text;
using System.Text.RegularExpressions;

namespace PromoFilterBot.Domain.Common;

/// <summary>
/// Finds the coupon codes of an offer ("Cupom: TECH10", "use o cupom PRIMEIRA10"), so the same product with another
/// coupon counts as another offer. A code is on the line of the word "cupom" (or on the next one), and is a word in
/// capitals or a mix of letters and numbers.
/// </summary>
public static class CouponExtractor {
    public const int MaxCoupons = 3;

    private const char Separator = ',';
    private const int MinCodeLength = 4;

    private static readonly Regex KeywordRegex = new(
        @"\b(cupom|cupons|cupon|cupao|coupon|codigo|cod)\b",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    // Links are taken out before, so the end of a link is not read as a code
    private static readonly Regex LinkRegex = new(
        @"\S*(?:://|www\.|\.[a-z]{2,}/|\.(?:com|br|to|ly|me|co|io|net|org|link|shop)\b)\S*",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex CodeRegex = new(
        @"(?<![\p{L}\p{N}_@#-])[A-Za-z0-9][A-Za-z0-9_-]{3,29}(?![\p{L}\p{N}_-])",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    // Words in capitals that show up on the line of a coupon without being one
    private static readonly HashSet<string> NotCodes = new HashSet<string>(StringComparer.Ordinal) {
        "CUPOM", "CUPONS", "CUPON", "COUPON", "CODIGO", "DESCONTO", "DESCONTOS", "OFF", "FRETE", "GRATIS", "PRIME",
        "LINK", "AQUI", "SITE", "LOJA", "ATIVE", "ATIVAR", "ATIVADO", "ATIVO", "APLICADO", "APLIQUE", "APLICAR",
        "AUTOMATICO", "PAGINA", "PRODUTO", "CARRINHO", "CHECKOUT", "RESGATE", "RESGATAR", "CLIQUE", "COMPRE", "COMPRA",
        "COMPRAS", "ACIMA", "ABAIXO", "MINIMO", "VALOR", "VALIDO", "LIMITADO", "LIMITADA", "EXCLUSIVO", "DISPONIVEL",
        "NOVOS", "USUARIOS", "CONTA", "PROMO", "PROMOCAO", "OFERTA", "OFERTAS", "HOJE", "AGORA", "CORRE", "URGENTE",
        "VOLTOU", "BAIXOU", "REAIS", "CASHBACK", "MOEDAS", "BLACK", "FRIDAY", "CYBER", "MONDAY", "NATAL", "SEMANA",
        "ESQUENTA", "AMAZON", "SHOPEE", "MAGALU", "KABUM", "ALIEXPRESS", "MERCADO", "LIVRE", "AMERICANAS", "SHEIN",
        "TEMU", "BOLETO", "CARTAO", "PARA", "COM", "USE", "USAR"
    };

    /// <summary>
    /// The codes in capitals, without repetition and in alphabetical order (the order of the post does not matter).
    /// </summary>
    public static IReadOnlyList<string> Extract(string? text) {
        if (string.IsNullOrWhiteSpace(text)) return [];

        // The compatibility form turns the styled letters (𝗧𝗘𝗖𝗛𝟭𝟬) into plain ones, keeping the capitals
        string[] lines = text.Normalize(NormalizationForm.FormKC).Split('\n');
        SortedSet<string> coupons = new SortedSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < lines.Length && coupons.Count < MaxCoupons; i++) {
            if (!KeywordRegex.IsMatch(TextNormalizer.Fold(lines[i]))) continue;

            List<string> codes = FindCodes(lines[i]);

            // "Cupom:" alone, with the code on the line below
            if (codes.Count == 0 && i + 1 < lines.Length) codes = FindCodes(lines[i + 1]);

            foreach (string code in codes.Take(MaxCoupons - coupons.Count)) coupons.Add(code);
        }

        return coupons.ToList();
    }

    /// <summary>
    /// The codes as one value, to store and to compare offers.
    /// </summary>
    public static string ToKey(IEnumerable<string> coupons) => string.Join(Separator, coupons);

    private static List<string> FindCodes(string line) {
        string withoutLinks = LinkRegex.Replace(line, " ");

        return CodeRegex.Matches(withoutLinks)
            .Select(match => match.Value)
            .Where(IsCode)
            .Select(code => code.ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static bool IsCode(string token) {
        if (token.Length < MinCodeLength) return false;

        bool hasLetter = token.Any(char.IsLetter);
        bool hasDigit = token.Any(char.IsDigit);

        if (!hasLetter) return false;
        if (NotCodes.Contains(token.ToUpperInvariant())) return false;

        // A word in lower case is part of the sentence ("cupom na página"); a code is in capitals or has numbers
        return hasDigit || token.Where(char.IsLetter).All(char.IsUpper);
    }
}