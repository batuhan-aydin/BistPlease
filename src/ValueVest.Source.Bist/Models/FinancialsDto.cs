using ErrorOr;
using System.Collections.Frozen;
using System.Text.Json.Serialization;
using ValueVest.Domain;
using Error = ErrorOr.Error;

namespace ValueVest.Source.Bist.Models;

public sealed record FinancialsDto
{
	[JsonPropertyName("value")]
	public IReadOnlyCollection<FinancialsValue> Financials { get; init; } = [];

	public ErrorOr<Worth> GetLastTermValue(FinancialValueType type)
	{
		var data = GetFinancialsValue(type);
		if (data.IsError) return data.FirstError;

		if (data.Value.FourthTerm != null)
		{
			return data.Value.FourthTerm.Value;
		} else if (data.Value.ThirdTerm != null)
		{
			return data.Value.ThirdTerm.Value;
		}
        else if (data.Value.SecondTerm != null)
        {
            return data.Value.SecondTerm.Value;
        }
        else if (data.Value.FirstTerm != null)
        {
            return data.Value.FirstTerm.Value;
        }
		return Error.Failure("All of them are null");
    }

	public ErrorOr<FinancialsByTerm> GetFinancialsValue(FinancialValueType type)
	{
		if (FinancialsExtensions.FinancialValueCodes.TryGetValue(type, out string? code) && 
			!string.IsNullOrWhiteSpace(code))
		{
            var data = Financials.FirstOrDefault(p => p.ItemCode == code);
			if (data == default)
				return Error.NotFound(type.ToString());
            var termsData = FinancialsByTermModule.Create(data.Value1 ?? string.Empty, data.Value2 ?? string.Empty,
            data.Value3 ?? string.Empty, data.Value4 ?? string.Empty, Currency.USD);
            if (termsData.IsOk)
                return termsData.ResultValue;
        }
		return Error.NotFound(type.ToString());
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

public static class FinancialsExtensions 
{
	public readonly static FrozenDictionary<FinancialValueType, string> FinancialValueCodes = new Dictionary<FinancialValueType, string>
    {
		{ FinancialValueType.NetProfit, "20CF" },
		{ FinancialValueType.OperationProfit, "3H" },
		{ FinancialValueType.TotalAssets,  "1BL" },
		{ FinancialValueType.ShortTermLiabilities, "2A" },
		{ FinancialValueType.LongTermLiabilities, "2B" },
		{ FinancialValueType.CashAndCashEquivalents, "1AA" },
		{ FinancialValueType.ShortTermFinancialInvestments, "1AB" },
		{ FinancialValueType.FinancialInvestments, "1BC" },
		{ FinancialValueType.ShortTermFinancialLoans, "2AA" },
		{ FinancialValueType.LongTermFinancialLoans, "2BA" },
		{ FinancialValueType.GrossProfit, "3D" },
		{ FinancialValueType.Amortization, "4B" },
		{ FinancialValueType.AdministrativeCosts, "3DB" },
		{ FinancialValueType.MarketingCosts, "3DA" },
		{ FinancialValueType.ResearchAndDevelopmentCosts, "3DC" },
		{ FinancialValueType.ParentShares, "3Z" },
		{ FinancialValueType.ParentShareholdersCapital, "2O" }
	}.ToFrozenDictionary();

    public static decimal? GetWorthOrDefault(this ErrorOr<Worth> worth)
    {
		if (worth.IsError) return null;
		return PriceModule.Value(worth.Value.Price);
    }
}

public enum FinancialValueType
{
	NetProfit, OperationProfit,
	TotalAssets, ShortTermLiabilities,
	LongTermLiabilities, CashAndCashEquivalents,
	ShortTermFinancialInvestments, FinancialInvestments,
	ShortTermFinancialLoans, LongTermFinancialLoans,
	GrossProfit, Amortization, AdministrativeCosts,
	MarketingCosts, ResearchAndDevelopmentCosts,
	ParentShares, ParentShareholdersCapital
}