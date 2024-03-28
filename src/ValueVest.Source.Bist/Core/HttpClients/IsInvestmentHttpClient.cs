using ValueVest.Domain;
using ValueVest.Source.Bist.Models;

namespace ValueVest.Source.Bist.Core.HttpClients;

public class IsInvestmentHttpClient : IIsInvestmentHttpClient
{
	private readonly HttpClient _httpClient;

	public IsInvestmentHttpClient(HttpClient httpClient)
	{
		_httpClient = httpClient;
	}

	/// <summary>
	/// Given the company symbol, return the last financials report
	/// </summary>
	/// <param name="symbol">Company symbol</param>
	/// <returns>Financials</returns>
	public async Task<Models.FinancialsDto?> GetFinancials(string symbol, Currency currency)
	{
		var currentYear = DateTime.Now.Year;
		var currentYearResult = await _httpClient.GetFromJsonAsync<Models.FinancialsDto?>(GetFinancialsUrl(symbol, currentYear, currency));
		if (currentYearResult?.Financials is null)
			return await _httpClient.GetFromJsonAsync<Models.FinancialsDto?>(GetFinancialsUrl(symbol, currentYear - 1, currency));
		return currentYearResult;
	}

	private string GetFinancialsUrl(string symbol, int year, Currency currency)
	{
		return (_httpClient.BaseAddress ?? throw new ArgumentNullException(nameof(_httpClient.BaseAddress)))
		.ToString().Replace("{Symbol}", symbol).Replace("{Year}", year.ToString().Replace("{Currency}", currency.ToString()));
	}
}

public interface IIsInvestmentHttpClient
{
	Task<Models.FinancialsDto?> GetFinancials(string symbol, Currency currency);
}