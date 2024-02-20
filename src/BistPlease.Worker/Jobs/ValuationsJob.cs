using AngleSharp.Dom;
using ValueVest.Domain;
using ValueVest.Worker.Core;
using ValueVest.Worker.Core.HttpClients;
using ValueVest.Worker.Repositories;
using ErrorOr;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace ValueVest.Worker.Jobs;

public class ValuationsJob : IJob
{
    private readonly ILogger<ValuationsJob> _logger;
    private readonly IAngleSharpWrapper _angleSharpWrapper;
    private readonly IsYatirimSettings _settings;
    private readonly IIsInvestmentHttpClient _isInvestmentHttpClient;
    private readonly IValuationsRepository _valuationsRepository;
    public static readonly JobKey Key = new(nameof(ValuationsJob), "IsYatirim");

    public ValuationsJob(ILogger<ValuationsJob> logger,
    IAngleSharpWrapper angleSharpWrapper,
    IOptions<IsYatirimSettings> settings,
    IIsInvestmentHttpClient isInvestmentHttpClient,
    IValuationsRepository valuationsRepository)
    {
        _logger = logger;
        _angleSharpWrapper = angleSharpWrapper;
        _settings = settings.Value;
        _isInvestmentHttpClient = isInvestmentHttpClient;
        _valuationsRepository = valuationsRepository;

	}

    public async Task Execute(IJobExecutionContext context)
    {
        var sectorList = await GetSectorList();
        if (sectorList is null)
        {
            _logger.Log(LogLevel.Error, BistPleaseErrors.SectorListParseError);
            return;
        }
        var sectors = SectorListModule.GetInnerSectorArray(sectorList);
        foreach(var sector in sectors)
        {
            await _valuationsRepository.UpsertSector(sector);
            await _valuationsRepository.UpsertCompanies(sector.Companies);
        }
    }

    private async Task<SectorList> GetSectorList()
    {
        var document = await _angleSharpWrapper.GetDocumentAsync(_settings.GetUrl(SectorIdModule.Create(1).ResultValue));
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
                var sectorDetailsDocument = await _angleSharpWrapper.GetDocumentAsync(_settings.GetUrl(sectorId.ResultValue));
                var sectorFinancialsHtml = sectorDetailsDocument.QuerySelector("#sectorAreaBigData");
                if (sectorFinancialsHtml is null)
                {
                    _logger.LogWarning("Couldn't parse {id} is sector's financials", sectorId);
                    continue;
                }
                if (!decimal.TryParse(sectorFinancialsHtml.Children[1].TextContent, out decimal peRaw)) { continue; }
                if (!decimal.TryParse(sectorFinancialsHtml.Children[4].TextContent, out decimal pbRaw)) { continue; }
                var priceEarnings = PriceEarningsModule.Create(peRaw);
                var priceToBook = PriceToBookModule.Create(pbRaw);
                if (sectorName.IsError || priceEarnings.IsError || priceToBook.IsError) {
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
        var document = await _angleSharpWrapper.GetDocumentAsync(_settings.GetUrl(sectorId));
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
        if (!decimal.TryParse(summary.Children[3].TextContent, out decimal lastPriceRaw)) { return Error.Validation(BistPleaseErrors.LastPriceParseError); }
        if (!decimal.TryParse(summary.Children[4].TextContent, out decimal marketWorthRaw)) { return Error.Validation(BistPleaseErrors.MarketWorthParseError); }
        if (!decimal.TryParse(summary.Children[6].TextContent, out decimal publicRatioRaw)) { return Error.Validation(BistPleaseErrors.PublicRatioParseError); }
        if (!decimal.TryParse(summary.Children[7].TextContent, out decimal capitalRaw)) { return Error.Validation(BistPleaseErrors.CapitalParseError); }
        if (!decimal.TryParse(financialsElement.Children[2].TextContent, out decimal peRaw)) { return Error.Validation(BistPleaseErrors.PEParseError); }
        if (!decimal.TryParse(financialsElement.Children[5].TextContent, out decimal pbRaw)) { return Error.Validation(BistPleaseErrors.PBParseError); }

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
