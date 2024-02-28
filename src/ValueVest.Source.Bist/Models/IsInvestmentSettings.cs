using ValueVest.Domain;

namespace ValueVest.Source.Bist.Models;

public sealed record IsInvestmentSettings
{
	public required string BaseSectorUrl { get; init; }
	public required string BaseFinancialsUrl { get; init; }
	public string GetUrl(SectorId sectorId) => BaseSectorUrl.Replace("{SectorId}", SectorIdModule.GetSectorIdUrlString(sectorId));
}
