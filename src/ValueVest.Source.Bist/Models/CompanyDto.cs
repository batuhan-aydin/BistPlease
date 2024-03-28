namespace ValueVest.Source.Bist.Models;

public class CompanyDto
{
    public required string Symbol { get; init; }
    public required string LastFinancialsTerm { get; init; }
    public float PublicOwnershipRatio { get; init; }

    public StockPriceDetailsValueDto? PriceDetails { get; init; }
}
