namespace ValueVest.Source.Bist.Models;

public sealed record CompanyTag
{
    public required string Name { get; init; }
    public required string LastBalanceTerm { get; init; }
    public required string PublicOwnershipRatio { get; init; }
}
