namespace ValueVest.Worker;

public sealed record IsYatirimSettings
{
    public required string BaseSectorUrl { get; init; }
    public required string BaseFinancialsUrl { get; init; }
    public string GetUrl(string sectorId) => BaseSectorUrl.Replace("{SectorId}", sectorId);
}
