namespace ValueVest.Source.Bist.Models;

public sealed record ExhangesApiSettings
{
    public string Url { get; init; } = null!;
}

public sealed record TryExhangeRates
{
    public string? UsdTry { get; init; }

    public decimal? UsdTryValue
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(UsdTry))
            {
                if (decimal.TryParse(UsdTry, out decimal value))
                    return value;
            }
            return null;
        }
    }
}
