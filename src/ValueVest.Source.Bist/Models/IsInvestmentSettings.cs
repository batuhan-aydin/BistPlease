namespace ValueVest.Source.Bist.Models;

public sealed record IsInvestmentSettings
{
	public required string BaseSectorUrl { get; init; }
	public required string BaseFinancialsUrl { get; init; }
	public string GetUrl(string sectorId) => BaseSectorUrl.Replace("{SectorId}", sectorId);
}
