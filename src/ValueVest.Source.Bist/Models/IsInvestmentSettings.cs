using ValueVest.Domain;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ValueVest.Source.Bist.Models;

public sealed record IsInvestmentSettings
{
	public required string BaseSectorUrl { get; init; }
    public required string BaseCompanyPageUrl { get; init; }
	public required string BaseFinancialsUrl { get; init; }
	public required string BaseFetchStockUrl { get; init; }

    public string GetUrl(string sectorId) => BaseSectorUrl.Replace("{SectorId}", sectorId);
    public string GetCompanyPageUrl(string symbol) => BaseCompanyPageUrl.Replace("{Symbol}", symbol);

	public string GetFinancialsUrl(string symbol, string lastBalanceTerm,
    Currency currency)
	{
        var lastFourYears = GetLastFourTerms(lastBalanceTerm);
        if (lastFourYears is null || lastFourYears.Count < 4) 
            return string.Empty;
        return BaseFinancialsUrl.Replace("{Symbol}", symbol)
        .Replace("{Exchange}", currency.ToString())
        .Replace("{Year1}", lastFourYears[0].Item2.ToString())
        .Replace("{Year2}", lastFourYears[1].Item2.ToString())
        .Replace("{Year3}", lastFourYears[2].Item2.ToString())
        .Replace("{Year4}", lastFourYears[3].Item2.ToString())
        .Replace("{Period1}", lastFourYears[0].Item1.ToString())
        .Replace("{Period2}", lastFourYears[1].Item1.ToString())
        .Replace("{Period3}", lastFourYears[2].Item1.ToString())
        .Replace("{Period4}", lastFourYears[3].Item1.ToString());

    }

	public string GetFetchStockUrl(string symbol, DateTime? startDate)
    {
        startDate = startDate.HasValue ? startDate.Value : DateTime.Now;
        var endDate = DateTime.Now.AddDays(1);
        if (startDate.Value.DayOfWeek == DayOfWeek.Monday || startDate.Value.DayOfWeek == DayOfWeek.Saturday || startDate.Value.DayOfWeek == DayOfWeek.Sunday)
        {
            while (startDate.Value.DayOfWeek == DayOfWeek.Monday || startDate.Value.DayOfWeek == DayOfWeek.Saturday || startDate.Value.DayOfWeek == DayOfWeek.Sunday)
            {
                startDate = startDate.Value.AddDays(-1);
            }
        }
        return BaseFetchStockUrl.Replace("{Symbol}", symbol).Replace("{StartDate}", startDate.Value.ToString("dd-MM-yyyy")).Replace("{EndDate}", endDate.ToString("dd-MM-yyyy"));
    }

    public static List<(int, int)> GetLastFourTerms(string term)
    {
        int month, year;
        try
        {
            month = int.Parse(term.Split("/")[0]);
            year = int.Parse(term.Split("/")[1]);
        }
        catch (FormatException)
        {
            throw new ArgumentException("Invalid term format. Expected M/YYYY");
        }

        var list = new (int, int)[4];
        list[3] = (month, year);
        list[2] = month == 12 || month == 9 || month == 6 ? (month - 3, year) : (12, year - 1);
        list[1] = month == 12 || month == 9 ? (month - 6, year) : month == 6 ? (12, year - 1) : (9, year - 1);
        list[0] = month == 12 ? (month - 9, year) : month == 9 ? (12, year - 1) : month == 6 ? (9, year - 1) : (6, year - 1);
        return list.ToList();
    }
}
