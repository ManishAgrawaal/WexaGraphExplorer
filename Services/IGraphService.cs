using GraphExplorer.Models;

namespace GraphExplorer.Services;

public interface IGraphService
{
    Task<GraphStats> GetStatsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PersonCard>> SearchPeopleAsync(string term, CancellationToken cancellationToken = default);
    Task<PersonCard?> GetPersonAsync(string personId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Recommendation>> GetRecommendationsAsync(string personId, CancellationToken cancellationToken = default);
}
