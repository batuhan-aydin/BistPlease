using AngleSharp.Dom;
using ErrorOr;
using Microsoft.Extensions.Options;
using ValueVest.Domain;
using ValueVest.Source.Bist.Core;
using ValueVest.Source.Bist.Models;

namespace ValueVest.Source.Bist.Services;

public class IsInvestmentService : IIsInvestmentService
{
	private readonly IWebParser _parser;
	private readonly IsInvestmentSettings _settings;
	private readonly IHttpClientFactory _httpClientFactory;

	public IsInvestmentService(IWebParser parser,
	IOptions<IsInvestmentSettings> settings,
	IHttpClientFactory httpClientFactory)
	{
		_parser = parser;
		_settings = settings.Value;
		_httpClientFactory = httpClientFactory;
	}

    public async Task<Models.FinancialsDto?> GetCurrentFinancials(string companySymbol, string lastFinancialTerms)
    {
        var url = _settings.GetFinancialsUrl(companySymbol, lastFinancialTerms);
        using var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Accept", "application/json");
        try
        {
            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Models.FinancialsDto?>();
            }
        } catch (Exception ex)
        {
            return null;
        }
        return null;
    }

	public async Task<StockPriceDetailsDto?> GetCurrentStockPriceDetails(string companySymbol)
	{
        var startDate = DateTime.Now;
        var endDate = DateTime.Now.AddDays(1);
        if (startDate.DayOfWeek ==  DayOfWeek.Monday || startDate.DayOfWeek == DayOfWeek.Saturday || startDate.DayOfWeek == DayOfWeek.Sunday)
        {
            startDate = GetLastWeekday(startDate);
        }

        var url = _settings.GetFetchStockUrl(companySymbol, startDate.ToString("dd-MM-yyyy"), endDate.ToString("dd-MM-yyyy"));
		using var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Accept", "application/json");
        try
        {
            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<StockPriceDetailsDto?>();
            }
        } catch(Exception ex)
        {
            return default;
        }
        return default;
	}

    // 1. Collect companies and their last balance terms
    // 2. Collect their last day results and last financial results
    public async Task<IEnumerable<Company>> GetCompanies()
    {
        var document = await _parser.GetDocumentAsync(_settings.GetUrl("1"));
        var sectors = document.QuerySelectorAll("#ddlSektor").FirstOrDefault();
        var list = new List<Company>();
        if (sectors != null)
        {
            foreach (var sector in sectors.Children)
            {
                if (!int.TryParse(sector.GetAttribute("value"), out int sectorId))
                    continue;
                var companies = await GetCompanies(sectorId);
                list.AddRange(companies);
            }
        }
        return list;
    }

    private async Task<IReadOnlyCollection<Company>> GetCompanies(int sectorId)
    {
        var result = new List<Company>();
        var document = await _parser.GetDocumentAsync(_settings.GetUrl(sectorId.ToString()));
        if (document is null) return [];
        var summary = document.QuerySelectorAll("#temelTBody_Ozet").FirstOrDefault();
        var financials = document.QuerySelectorAll("#temelTBody_Finansal").FirstOrDefault();
        if (summary is null || financials is null) { return result.ToArray(); }
        for (var i = 0; i < summary.Children.Length; i++)
        {
            var company = await ExtractCompany(summary.Children[i], financials.Children[i]);
            if (company.IsError)
            {
                continue;
            }
            result.Add(company.Value);
        }
        return result.ToArray();
    }

    // I have decided to collect only company symbol, last financial terms and public ownership ratio
    private async Task<ErrorOr<Company>> ExtractCompany(IElement summary, IElement financialsElement)
    {
        if (!float.TryParse(summary.Children[6].TextContent.Replace(",", "."), out float publicRatioRaw)) { return Error.Validation(IsInvestmentErrors.PublicRatioParseError); }
        var lastBalanceTerm = financialsElement.Children[6].TextContent;


        // 1. symbol, name, public ownership ratio
        var symbolResult = SymbolModule.Create(summary.Children[0].TextContent);
        var nameResult = NameModule.Create(summary.Children[1].TextContent);
        var publicOwnershipRatioResult = PublicOwnershipRatioModule.Create(publicRatioRaw);
        if (symbolResult.IsError || nameResult.IsError || publicOwnershipRatioResult.IsError)
            return Error.Validation("Couldn't  read company summary");

        // 2. company valuation
        // lastClosingPrice, marketValue, capital

        var priceDetails = await GetCurrentStockPriceDetails(summary.Children[0].TextContent);
        if (priceDetails?.Prices != null)
        {
            var priceValues = priceDetails.Prices.FirstOrDefault();
            if (priceValues != null)
            {
                var valuationsResult = CompanyValuationsModule.Create(priceValues.Price, priceValues.MarketValue, priceValues.Capital, Currency.USD);
                if (valuationsResult.IsError) return Error.Validation("Valuations");
                var valuationsValue = valuationsResult.ResultValue;
                var financials = await GetCurrentFinancials(summary.Children[0].TextContent, lastBalanceTerm);
                if (financials != null)
                {
                    var marketValue = valuationsValue.MarketValue;
                    var priceEarnings =  GetPriceEarnings(financials, marketValue);
                    var priceToBook = GetPriceToBook(financials, marketValue);
                    var evEbitda = GetEvEbitda(financials, marketValue);
                }
            }
        }

        return Error.Failure();
    }

    // pe = market value / profits
    private static ErrorOr<decimal?> GetPriceEarnings(Models.FinancialsDto financials, Worth marketValueWorth)
    {
        var marketValue = PriceModule.Value(marketValueWorth.Price);
        if (marketValue == default) return Error.Failure("Market valuecannot be zero");
        var profits = financials.GetLastTermValue(FinancialValueType.NetProfit).GetWorthOrDefault();
        if (!profits.HasValue || profits.Value == default)
            return Error.Failure("No profits");
        return (marketValue / profits.Value);
    }

    // market value / (total assets - total debt)
    private static ErrorOr<decimal?> GetPriceToBook(Models.FinancialsDto financials, Worth marketValueWorth)
    {
        var marketValue = PriceModule.Value(marketValueWorth.Price);
        if (marketValue == default) return Error.Failure("Market valuecannot be zero");
        var totalAssets = financials.GetLastTermValue(FinancialValueType.TotalAssets).GetWorthOrDefault();
        if (totalAssets is null) return Error.Failure("Assets needed");
        var longTermLiabilities = financials.GetLastTermValue(FinancialValueType.LongTermLiabilities).GetWorthOrDefault() ?? 0;
        var shortTermLiabilities = financials.GetLastTermValue(FinancialValueType.ShortTermLiabilities).GetWorthOrDefault() ?? 0;
        if (longTermLiabilities + shortTermLiabilities == 0) return Error.Failure("Debt calculation error");

        var result = marketValue / (totalAssets - (longTermLiabilities + shortTermLiabilities));
        return result;
    }

    
    // Ev / Ebitdat (fd / favök)
    private static ErrorOr<decimal?> GetEvEbitda(Models.FinancialsDto financials, Worth marketValueWorth)
    {
        return GetEnterpriseMultiple(financials, marketValueWorth) / GetEbitda(financials);
       
    }

	// EBITDA = gross profit + amortization - management cost - marketing and sales cost - research and development cost

	private static decimal? GetEbitda(Models.FinancialsDto financials)
    {
        var grossProfit = financials.GetLastTermValue(FinancialValueType.GrossProfit).GetWorthOrDefault();
        var amortization = financials.GetLastTermValue(FinancialValueType.Amortization).GetWorthOrDefault();
        var managementCosts = financials.GetLastTermValue(FinancialValueType.AdministrativeCosts).GetWorthOrDefault();
        var marketingCosts = financials.GetLastTermValue(FinancialValueType.MarketingCosts).GetWorthOrDefault();
        var researchAndDevCosts = financials.GetLastTermValue(FinancialValueType.ResearchAndDevelopmentCosts).GetWorthOrDefault();
        return grossProfit + amortization + managementCosts + marketingCosts + researchAndDevCosts;

	}

	// EV = market value + (short term + long term financial loans) - (cash and cash equivilants + financial investments)
	private static decimal? GetEnterpriseMultiple(Models.FinancialsDto financials, Worth marketValueWorth)
    {
		var marketValue = PriceModule.Value(marketValueWorth.Price);
		if (marketValue == default) return null;
		var shortTermLoan = financials.GetLastTermValue(FinancialValueType.ShortTermFinancialLoans).GetWorthOrDefault();
		var longTermLoan = financials.GetLastTermValue(FinancialValueType.LongTermFinancialLoans).GetWorthOrDefault();
		var cash = financials.GetLastTermValue(FinancialValueType.CashAndCashEquivalents).GetWorthOrDefault();
		var shortTermInvestment = financials.GetLastTermValue(FinancialValueType.ShortTermFinancialInvestments).GetWorthOrDefault();
		var investments = financials.GetLastTermValue(FinancialValueType.FinancialInvestments).GetWorthOrDefault();
        return marketValue + ((shortTermLoan + longTermLoan) - (cash + shortTermInvestment + investments));
	}

    private static DateTime GetLastWeekday(DateTime date)
    {
        while (date.DayOfWeek == DayOfWeek.Monday || date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
        {
            date = date.AddDays(-1);
        }
        return date;
    }

}

public interface IIsInvestmentService
{
    Task<IEnumerable<Company>> GetCompanies();
}