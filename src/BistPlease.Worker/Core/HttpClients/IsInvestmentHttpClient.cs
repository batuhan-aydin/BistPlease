using BistPlease.Worker.Models;
using System.Net.Http.Json;

namespace BistPlease.Worker.Core.HttpClients;

public class IsInvestmentHttpClient : IIsInvestmentHttpClient
{
    private readonly HttpClient _httpClient;

    public IsInvestmentHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<IsFinancialsDto?> GetFinancials(string symbol)
    {
        var url = (_httpClient.BaseAddress ?? throw new ArgumentNullException(nameof(_httpClient.BaseAddress)))
        .ToString().Replace("{Symbol}", symbol).Replace("{Year}", DateTime.Now.Year.ToString());
        return _httpClient.GetFromJsonAsync<IsFinancialsDto?>(url);
    }
}

public interface IIsInvestmentHttpClient
{
    Task<IsFinancialsDto?> GetFinancials(string symbol);
}
