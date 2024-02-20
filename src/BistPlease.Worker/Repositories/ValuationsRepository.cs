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
        var query = @"INSERT INTO sector (id, name, average_pe, average_pb)
                    VALUES (@Id, @Name, @Average_PE, @Average_PB)
                    ON CONFLICT(id) DO UPDATE SET
                        name = @Name,
                        average_pe = @Average_PE,
                        average_pb = @Average_PB,
                        last_modified_date = CURRENT_TIMESTAMP;";
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