using Microsoft.Extensions.Logging;
using Quartz;
using System.Net.Http.Json;
using ValueVest.Domain;
using ValueVest.Worker.Models;
using ValueVest.Worker.Repositories;

namespace ValueVest.Worker.Jobs;

public class CompaniesJob : IJob
{
    private readonly ILogger<CompaniesJob> _logger;
    private readonly IValuationsRepository _valuationsRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DataSources _dataSources;
    public static readonly JobKey Key = new(nameof(CompaniesJob), "IsYatirim");

	public CompaniesJob(ILogger<CompaniesJob> logger,
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
        var url = $"{_dataSources.Bist.Base}{_dataSources.Bist.GetCompanies}";
        var companies = await client.GetFromJsonAsync<IEnumerable<Company>>(url);
        if (companies is null)
        {
			_logger.Log(LogLevel.Error, BistPleaseErrors.CompanyListParseError);
			return;
		}
		await _valuationsRepository.UpsertCompanies(companies);
	}
}
