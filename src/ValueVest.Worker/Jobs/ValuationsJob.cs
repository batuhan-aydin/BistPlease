using Microsoft.Extensions.Logging;
using Quartz;
using System.Net.Http.Json;
using ValueVest.Domain;
using ValueVest.Worker.Models;
using ValueVest.Worker.Repositories;

namespace ValueVest.Worker.Jobs;

public class ValuationsJob : IJob
{
    private readonly ILogger<ValuationsJob> _logger;
    private readonly IValuationsRepository _valuationsRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DataSources _dataSources;
    public static readonly JobKey Key = new(nameof(ValuationsJob), "IsYatirim");

	public ValuationsJob(ILogger<ValuationsJob> logger,
	IValuationsRepository valuationsRepository,
	IHttpClientFactory httpClientFactory,
	DataSources dataSources)
	{
		_logger = logger;
		_valuationsRepository = valuationsRepository;
		_httpClientFactory = httpClientFactory;
		_dataSources = dataSources;
	}

	public async Task Execute(IJobExecutionContext context)
    {
		using HttpClient client = _httpClientFactory.CreateClient();
        var url = $"{_dataSources.Bist.Base}{_dataSources.Bist.GetValuations}";
        var sectorList = await client.GetFromJsonAsync<SectorList>("");
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
}
