using BistPlease.Domain;
using ErrorOr;
using System.Text.Json.Serialization;

namespace BistPlease.Worker.Models;

public sealed record IsFinancialsDto
{
    [JsonPropertyName("value")]
    public IReadOnlyCollection<FinancialsValue>? Financials { get; set; }

    public ErrorOr<FinancialsByTerm> GetProfits()
    {
        if (Financials is null || Financials.Count == 0)
            return FinancialsByTermModule.Create("0", "0", "0", "0", Currency.TL).ResultValue;

        var profitData = Financials.FirstOrDefault(p => p.ItemCode == "2OCF");
        if (profitData == default)
            return FinancialsByTermModule.Create("0", "0", "0", "0", Currency.TL).ResultValue;

        var profits = FinancialsByTermModule.Create(profitData.Value1 ?? string.Empty, profitData.Value2 ?? string.Empty,
        profitData.Value3 ?? string.Empty, profitData.Value4 ?? string.Empty, Currency.TL);
        if (profits.IsOk)
            return profits.ResultValue;
        return Error.Validation(profits.ErrorValue.ToString());
    }

    public ErrorOr<FinancialsByTerm> GetOperationProfits()
    {
        if (Financials is null || Financials.Count == 0)
            return FinancialsByTermModule.Create("0", "0", "0", "0", Currency.TL).ResultValue;

        var profitData = Financials.FirstOrDefault(p => p.ItemCode == "3H");
        if (profitData == default)
            return FinancialsByTermModule.Create("0", "0", "0", "0", Currency.TL).ResultValue;

        var profits = FinancialsByTermModule.Create(profitData.Value1 ?? string.Empty, profitData.Value2 ?? string.Empty,
        profitData.Value3 ?? string.Empty, profitData.Value4 ?? string.Empty, Currency.TL);
        if (profits.IsOk)
            return profits.ResultValue;
        return Error.Validation(profits.ErrorValue.ToString());
    }
}

public readonly record struct FinancialsValue
{
    public string ItemCode { get; init; }
    public string? Value1 { get; init; }
    public string? Value2 { get; init; }
    public string? Value3 { get; init; }
    public string? Value4 { get; init; }
}

