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
	private readonly ILogger _logger;

	public IsInvestmentService(IWebParser parser, IOptions<IsInvestmentSettings> settings, ILogger logger)
	{
		_parser = parser;
		_settings = settings.Value;
		_logger = logger;
	}

	public async Task<SectorList> GetValuations()
	{
		var document = await _parser.GetDocumentAsync(_settings.GetUrl(SectorIdModule.Create(1).ResultValue));
		var sectors = document.QuerySelectorAll("#ddlSektor").FirstOrDefault();
		var sectorList = SectorListModule.InitSectorList();
		if (sectors != null)
		{
			foreach (var sector in sectors.Children)
			{
				if (!int.TryParse(sector.GetAttribute("value"), out int id))
					continue;
				var sectorId = SectorIdModule.Create(id);
				if (sectorId.IsError)
				{
					_logger.LogWarning("Error while parsing id {id}, error: {error}", id, sectorId.ErrorValue);
					continue;
				}
				var sectorName = NameModule.Create(sector.TextContent);
				var sectorDetailsDocument = await _parser.GetDocumentAsync(_settings.GetUrl(sectorId.ResultValue));
				var sectorFinancialsHtml = sectorDetailsDocument.QuerySelector("#sectorAreaBigData");
				if (sectorFinancialsHtml is null)
				{
					_logger.LogWarning("Couldn't parse {id} is sector's financials", sectorId);
					continue;
				}
				if (!float.TryParse(sectorFinancialsHtml.Children[1].TextContent, out float peRaw)) { continue; }
				if (!float.TryParse(sectorFinancialsHtml.Children[4].TextContent, out float pbRaw)) { continue; }
				var priceEarnings = PriceEarningsModule.Create(peRaw);
				var priceToBook = PriceToBookModule.Create(pbRaw);
				if (sectorName.IsError || priceEarnings.IsError || priceToBook.IsError)
				{
					_logger.LogWarning("Couldn't parse the sector id: {id}", sectorId);
					continue;
				}
				var companies = await GetCompanies(sectorId.ResultValue);
				var newSector = new Sector(sectorId.ResultValue,
				sectorName.ResultValue,
				priceEarnings.ResultValue,
				priceToBook.ResultValue,
				companies);

				sectorList = SectorListModule.AddSector(newSector, sectorList);
			}
		}
		return sectorList;
	}

	private async Task<Company[]> GetCompanies(SectorId sectorId)
	{
		var result = new List<Company>();
		var document = await _parser.GetDocumentAsync(_settings.GetUrl(sectorId));
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
		if (!decimal.TryParse(summary.Children[6].TextContent, out decimal publicRatioRaw)) { return Error.Validation(IsInvestmentErrors.PublicRatioParseError); }
		if (!decimal.TryParse(summary.Children[7].TextContent, out decimal capitalRaw)) { return Error.Validation(IsInvestmentErrors.CapitalParseError); }
		if (!float.TryParse(financialsElement.Children[2].TextContent, out float peRaw)) { return Error.Validation(IsInvestmentErrors.PEParseError); }
		if (!float.TryParse(financialsElement.Children[5].TextContent, out float pbRaw)) { return Error.Validation(IsInvestmentErrors.PBParseError); }

		var companySymbol = SymbolModule.Create(summary.Children[0].TextContent);
		if (companySymbol.IsError) return Error.Validation(companySymbol.ErrorValue.ToString());

		// var financeData = await _isInvestmentHttpClient.GetFinancials(SymbolModule.Value(companySymbol.ResultValue));
		//if (financeData?.Financials is null || financeData.Financials.Count == 0)
		//     return Error.NotFound("Company financials not found");

		//var profits = financeData.GetProfits();
		//var operationProfits = financeData.GetOperationProfits();
		//if (profits.IsError || operationProfits.IsError)
		//    return Error.NotFound("Company financials not found");

		var company = CompanyModule.Create(summary.Children[0].TextContent,
		summary.Children[1].TextContent,
		lastPriceRaw, marketWorthRaw, publicRatioRaw, capitalRaw, peRaw, pbRaw,
		Currency.TRY);

		if (company.IsError) return Error.Validation(company.ErrorValue.ToString());

		return company.ResultValue;
	}
}

public interface IIsInvestmentService
{
	Task<SectorList> GetValuations();
}