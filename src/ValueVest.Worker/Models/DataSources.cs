namespace ValueVest.Worker.Models;

public sealed record DataSources
{
    public BistSource Bist { get; init; } = null!;
}

public sealed record BistSource
{
	public string Base { get; init; } = null!;

	public string GetCompanies { get; init; } = null!;
}
