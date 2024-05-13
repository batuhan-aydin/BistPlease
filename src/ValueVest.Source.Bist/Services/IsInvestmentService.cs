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

    public async Task<ErrorOr<Company>> GetCompany(string symbol, Currency currency)
    {
        // I/O
        var companyTag = await GetCompanyTag(symbol);
        var dailyPriceDetailsList = await GetCurrentStockPriceDetails(symbol, false);
        var financials = await GetCurrentFinancials(symbol, companyTag.Value.LastBalanceTerm, currency);

        // Validations
        if (companyTag.IsError || !float.TryParse(companyTag.Value.PublicOwnershipRatio.Replace(',', '.'), out float poRatio))
            return Error.Failure();

        if (financials is null || !financials.Financials.Any())
            return Error.Failure();

        var companyNameResult = NameModule.Create(companyTag.Value.Name);
        var companySymbolResult = SymbolModule.Create(symbol);
        var publicOwnershipRatioResult = PublicOwnershipRatioModule.Create(poRatio);
        if (companyNameResult.IsError || companySymbolResult.IsError || publicOwnershipRatioResult.IsError)
            return Error.Failure();

        var recentPriceDetails = dailyPriceDetailsList?.GetLastOrDefault();
        if (recentPriceDetails is null) 
            return Error.Failure();

        var valuationsResult = currency.Tag switch
        {
            0 => CompanyValuationsModule.Create(recentPriceDetails.PriceTry, recentPriceDetails.MarketValueTRY, recentPriceDetails.Capital, currency),
            _ => CompanyValuationsModule.Create(recentPriceDetails.Price, recentPriceDetails.MarketValue, recentPriceDetails.Capital, currency)
        };
        if (valuationsResult.IsError)
            return Error.Failure();

        // Calculations
        var marketValueWorth = valuationsResult.ResultValue.MarketValue;
        var marketValue = PriceModule.Value(marketValueWorth.Price);
        var priceEarnings = GetPriceEarnings(financials, marketValue);
        var priceToBook = GetPriceToBook(financials, marketValue);
        var evEbitda = GetEvEbitda(financials, marketValue);

        if (priceEarnings.IsError || priceToBook.IsError || evEbitda.IsError)
            return Error.Failure("Fecth data failure");

        var ratios = CompanyFinancialRatiosModule.Create((float?)priceEarnings.Value, (float?)priceToBook.Value, (float?)evEbitda.Value, companyTag.Value.LastBalanceTerm);
        if (ratios.IsError)
            return Error.Failure();

        var company = CompanyModule.Create(companySymbolResult.ResultValue,
        companyNameResult.ResultValue,
        publicOwnershipRatioResult.ResultValue,
        ratios.ResultValue,
        valuationsResult.ResultValue);

        return company;
    }

    public async Task<Models.FinancialsDto?> GetCurrentFinancials(string companySymbol, string lastFinancialTerms,
    Currency currency)
    {
        var url = _settings.GetFinancialsUrl(companySymbol, lastFinancialTerms, currency);
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

	public async Task<StockPriceDetailsDto?> GetCurrentStockPriceDetails(string companySymbol, bool oneWeekBefore)
	{
        var url = _settings.GetFetchStockUrl(companySymbol, oneWeekBefore ? DateTime.Now.AddDays(-7) : null);
		using var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Accept", "application/json");
        try
        {
            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<StockPriceDetailsDto?>();
                if ((result is null || result.Prices.Count == 0) && oneWeekBefore == false)
                    result = await GetCurrentStockPriceDetails(companySymbol, true);
                return result;
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

    private async Task<ErrorOr<CompanyTag>> GetCompanyTag(string symbol)
    {
        var document = await _parser.GetDocumentAsync(_settings.GetCompanyPageUrl(symbol));
        var table = document.QuerySelectorAll("[data-csvname=\"finansaloranlargerceklesen\"]").FirstOrDefault();
        if (table is null)
            return Error.Failure();
        var rows = table.QuerySelectorAll("td");
        var lastRow = rows.LastOrDefault();
        var lastBalanceTerm = lastRow == null ? string.Empty : lastRow.InnerHtml;

        var companyTag = document.QuerySelectorAll(".companyTag").FirstOrDefault();
        if (companyTag is null)
            return Error.Failure();
        rows = companyTag.QuerySelectorAll("td");
        var firstRow = rows.FirstOrDefault();
        var name = firstRow == null ? string.Empty : firstRow.InnerHtml;

        var otherTables = document.QuerySelectorAll(".table.vertical");
        if (otherTables is null || otherTables.Length < 2)
            return Error.Failure();
        rows = otherTables[otherTables.Length - 2].QuerySelectorAll("td");
        lastRow = rows.LastOrDefault();
        var publicOwnershipRatio = lastRow == null ? string.Empty : lastRow.InnerHtml;

        return new CompanyTag
        {
            Name = name,
            LastBalanceTerm = lastBalanceTerm,
            PublicOwnershipRatio = publicOwnershipRatio
        };
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

        var priceDetails = await GetCurrentStockPriceDetails(summary.Children[0].TextContent, false);
        if (priceDetails?.Prices != null)
        {
            var priceValues = priceDetails.Prices.FirstOrDefault();
            if (priceValues != null)
            {
                var valuationsResult = CompanyValuationsModule.Create(priceValues.Price, priceValues.MarketValue, priceValues.Capital, Currency.USD);
                if (valuationsResult.IsError) return Error.Validation("Valuations");
                var valuationsValue = valuationsResult.ResultValue;
                var financials = await GetCurrentFinancials(summary.Children[0].TextContent, lastBalanceTerm, Currency.USD);
                if (financials != null)
                {
                    var marketValueWorth = valuationsValue.MarketValue;
					var marketValue = PriceModule.Value(marketValueWorth.Price);
					if (marketValue == default) return Error.Failure("Market valuecannot be zero");
					var priceEarnings =  GetPriceEarnings(financials, marketValue);
                    var priceToBook = GetPriceToBook(financials, marketValue);
                    var evEbitda = GetEvEbitda(financials, marketValue);
                    if (priceEarnings.IsError || priceToBook.IsError || evEbitda.IsError)
                        return Error.Failure("Fecth data failure");

                    var ratios = CompanyFinancialRatiosModule.Create((float?)priceEarnings.Value, (float?)priceToBook.Value, (float?)evEbitda.Value, lastBalanceTerm);

				}
            }
        }

        return Error.Failure();
    }

    // pe = market value / profits
    private static ErrorOr<decimal?> GetPriceEarnings(Models.FinancialsDto financials, decimal marketValue)
    {
        var profits = financials.GetLastTermValue(FinancialValueType.NetProfit).GetWorthOrDefault() ??
        financials.GetLastTermValue(FinancialValueType.ParentShares).GetWorthOrDefault();
        if (!profits.HasValue || profits.Value == default)
            return Error.Failure("No profits");
        return (marketValue / profits.Value);
    }

    // market value / (total assets - total debt)
    private static ErrorOr<decimal?> GetPriceToBook(Models.FinancialsDto financials, decimal marketValue)
    {
        var totalAssets = financials.GetLastTermValue(FinancialValueType.TotalAssets).GetWorthOrDefault();
        if (totalAssets is null) return Error.Failure("Assets needed");
        var longTermLiabilities = financials.GetLastTermValue(FinancialValueType.LongTermLiabilities).GetWorthOrDefault() ?? 0;
        var shortTermLiabilities = financials.GetLastTermValue(FinancialValueType.ShortTermLiabilities).GetWorthOrDefault() ?? 0;
        if (longTermLiabilities + shortTermLiabilities == 0) return Error.Failure("Debt calculation error");
        var parentShareholdersCapital = financials.GetLastTermValue(FinancialValueType.ParentShareholdersCapital).GetWorthOrDefault() ?? 0;
        return marketValue / parentShareholdersCapital;
        //var result = marketValue / (totalAssets - (longTermLiabilities + shortTermLiabilities));
    }

    
    // Ev / Ebitdat (fd / favök)
    private static ErrorOr<decimal?> GetEvEbitda(Models.FinancialsDto financials, decimal marketValue)
    {
        var a = GetEnterpriseMultiple(financials, marketValue);
        var b = GetEbitda(financials);
        return a / b;
       
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
	private static decimal? GetEnterpriseMultiple(Models.FinancialsDto financials, decimal marketValue)
    {
		var shortTermLoan = financials.GetLastTermValue(FinancialValueType.ShortTermFinancialLoans).GetWorthOrDefault();
		var longTermLoan = financials.GetLastTermValue(FinancialValueType.LongTermFinancialLoans).GetWorthOrDefault();
		var cash = financials.GetLastTermValue(FinancialValueType.CashAndCashEquivalents).GetWorthOrDefault();
		var shortTermInvestment = financials.GetLastTermValue(FinancialValueType.ShortTermFinancialInvestments).GetWorthOrDefault();
		var investments = financials.GetLastTermValue(FinancialValueType.FinancialInvestments).GetWorthOrDefault();
        //return marketValue + ((shortTermLoan + longTermLoan) - (cash + shortTermInvestment + investments));
        return marketValue + ((shortTermLoan + longTermLoan) - (cash));

    }
}

public interface IIsInvestmentService
{
    Task<IEnumerable<Company>> GetCompanies();
    Task<ErrorOr<Company>> GetCompany(string symbol, Currency currency);
}