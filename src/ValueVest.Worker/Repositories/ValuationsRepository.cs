using ValueVest.Domain;
using ValueVest.Worker.Core.Data;

namespace ValueVest.Worker.Repositories;

public class ValuationsRepository : BaseRepository, IValuationsRepository
{
    public ValuationsRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public Task<int> UpsertSector(Sector sector)
    {
        var query = @"INSERT INTO sector (id, name, price_earnings, price_to_book)
                    VALUES (@Id, @Name, @PriceEarnings, @PriceToBook)
                    ON CONFLICT(id) DO UPDATE SET
                    name = @Name,
                    price_earnings = @PriceEarnings,
                    price_to_book = @PriceToBook";
        return ExecuteAsync(query, sector);
    }

    public Task<int> UpsertCompanies(IEnumerable<Company> companies)
    {
		var query = @"";
		return ExecuteMultipleAsync(query, companies);
	}
}

public interface IValuationsRepository
{
    Task<int> UpsertSector(Sector sector);
    Task<int> UpsertCompanies(IEnumerable<Company> companies);
}