using BistPlease.Domain;
using BistPlease.Worker.Core.Data;

namespace BistPlease.Worker.Repositories;

public class ValuationsRepository : BaseRepository, IValuationsRepository
{
    public ValuationsRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public Task<int> UpsertSectors(IEnumerable<Sector> sectors)
    {
        var query = "";
        return ExecuteMultipleAsync(query, sectors);
    }
}

public interface IValuationsRepository
{
}