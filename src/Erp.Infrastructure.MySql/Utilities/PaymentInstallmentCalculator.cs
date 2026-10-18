using System.Globalization;
using System.Text.RegularExpressions;

namespace Erp.Infrastructure.MySql.Utilities;

internal static class PaymentInstallmentCalculator
{
    public static IReadOnlyList<int> ParseOffsets(string? paymentTerm)
    {
        if (string.IsNullOrWhiteSpace(paymentTerm))
        {
            return [0];
        }

        var normalized = paymentTerm.Trim().ToUpperInvariant();
        if (normalized is "A_VISTA" or "AVISTA" or "A VISTA" or "VISTA")
        {
            return [0];
        }

        var tokens = Regex.Matches(normalized, @"\d+")
            .Select(m => int.TryParse(m.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var days) ? days : 0)
            .Where(v => v >= 0)
            .ToList();

        return tokens.Count == 0 ? [0] : tokens;
    }

    public static IReadOnlyList<Installment> BuildInstallments(decimal totalAmount, DateTime baseDate, string? paymentTerm)
    {
        var offsets = ParseOffsets(paymentTerm);
        var count = offsets.Count;
        var each = decimal.Round(totalAmount / count, 2);
        var list = new List<Installment>(count);
        decimal accumulated = 0;

        for (var i = 0; i < count; i++)
        {
            var amount = i == count - 1 ? decimal.Round(totalAmount - accumulated, 2) : each;
            accumulated += amount;
            list.Add(new Installment(i + 1, baseDate.Date.AddDays(offsets[i]), amount));
        }

        return list;
    }

    internal readonly record struct Installment(int Number, DateTime DueDate, decimal Amount);
}
