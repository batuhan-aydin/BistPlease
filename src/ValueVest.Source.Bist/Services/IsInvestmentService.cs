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
	private readonly ExhangesApiSettings _exchangeSettings;
	private readonly ILogger _logger;
	private readonly IHttpClientFactory _httpClientFactory;

	public IsInvestmentService(IWebParser parser,
	IOptions<IsInvestmentSettings> settings,
	ILogger logger,
	IHttpClientFactory httpClientFactory,
	ExhangesApiSettings exchangeSettings)
	{
		_parser = parser;
		_settings = settings.Value;
		_logger = logger;
		_httpClientFactory = httpClientFactory;
		_exchangeSettings = exchangeSettings;
	}

	public async Task<IEnumerable<Company>> GetCompanyValuations()
	{
		var exchangeRates = await GetTryExchangeRates();
		if (exchangeRates is null || !exchangeRates.UsdTryValue.HasValue)
			return Enumerable.Empty<Company>();
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
		var document = await _parser.GetDocumentAsync(sectorId.ToString());
		var summary = document.QuerySelectorAll("#temelTBody_Ozet").FirstOrDefault();
		var financials = document.QuerySelectorAll("#temelTBody_Finansal").FirstOrDefault();
		if (summary is null || financials is null) { return result.ToArray(); }
		for (var i = 0; i < summary.Children.Length; i++)
		{
			var company = ExtractCompany(summary.Children[i], financials.Children[i]);
			if (company.IsError)
			{
				_logger.LogWarning("Company parsing error {0}", company.FirstError);
				continue;
			}
			result.Add(company.Value);
		}
		return result.ToArray();
	}

	private ErrorOr<Company> ExtractCompany(IElement summary, IElement financialsElement)
	{
		if (!decimal.TryParse(summary.Children[3].TextContent, out decimal lastPriceRaw)) { return Error.Validation(IsInvestmentErrors.LastPriceParseError); }
		if (!decimal.TryParse(summary.Children[4].TextContent, out decimal marketWorthRaw)) { return Error.Validation(IsInvestmentErrors.MarketWorthParseError); }
		if (!float.TryParse(summary.Children[6].TextContent, out float publicRatioRaw)) { return Error.Validation(IsInvestmentErrors.PublicRatioParseError); }
		if (!decimal.TryParse(summary.Children[7].TextContent, out decimal capitalRaw)) { return Error.Validation(IsInvestmentErrors.CapitalParseError); }
		if (!float.TryParse(financialsElement.Children[2].TextContent, out float peRaw)) { return Error.Validation(IsInvestmentErrors.PEParseError); }
		if (!float.TryParse(financialsElement.Children[5].TextContent, out float pbRaw)) { return Error.Validation(IsInvestmentErrors.PBParseError); }

		// var financeData = await _isInvestmentHttpClient.GetFinancials(SymbolModule.Value(companySymbol.ResultValue));
		//if (financeData?.Financials is null || financeData.Financials.Count == 0)
		//     return Error.NotFound("Company financials not found");

		//var profits = financeData.GetProfits();
		//var operationProfits = financeData.GetOperationProfits();
		//if (profits.IsError || operationProfits.IsError)
		//    return Error.NotFound("Company financials not found");

		var request = new CompanyCreateRequest(summary.Children[0].TextContent, summary.Children[1].TextContent,
		publicRatioRaw, peRaw, pbRaw, 3.5f, 3.5f, "", 3.5M, 3.5M, 3.5M, 3.5M);
		var company = CompanyModule.Create(request);

		if (company.IsError) return Error.Validation(company.ErrorValue.ToString());

		return company.ResultValue;
	}

	private async Task<TryExhangeRates?> GetTryExchangeRates()
	{
		using HttpClient client = _httpClientFactory.CreateClient();

		try
		{
			var exchanges = await client.GetFromJsonAsync<TryExhangeRates>(_exchangeSettings.Url.Replace("{currency_code}", "try"));
			return exchanges;
		}
		catch (Exception ex)
		{
			_logger.LogError("Error while getting exhange rate: {Error}", ex);
		}
		return null;
	}
}

public interface IIsInvestmentService
{
	Task<IEnumerable<Company>> GetCompanyValuations();
}