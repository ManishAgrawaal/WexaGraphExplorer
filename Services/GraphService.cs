using GraphExplorer.Models;
using Neo4j.Driver;

namespace GraphExplorer.Services;

public sealed class GraphService : IGraphService
{
    private readonly IDriver _driver;
    private readonly ILogger<GraphService> _logger;

    public GraphService(IDriver driver, ILogger<GraphService> logger)
    {
        _driver = driver;
        _logger = logger;
    }

    public async Task<GraphStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        const string cypher = """
            MATCH (n)
            WITH labels(n)[0] AS label, count(n) AS count
            RETURN label, count
            """;

        const string relationshipCypher = """
            MATCH ()-[r]->()
            RETURN count(r) AS count
            """;

        try
        {
            await using var session = _driver.AsyncSession();

            var nodeCursor = await session.RunAsync(cypher);
            var nodeCounts = await nodeCursor.ToListAsync(r =>
                (Label: r["label"].As<string>(), Count: r["count"].As<int>()));

            var relationshipCursor = await session.RunAsync(relationshipCypher);
            var relationshipCount = (await relationshipCursor.SingleAsync())["count"].As<int>();

            return new GraphStats(
                nodeCounts.FirstOrDefault(x => x.Label == "Person").Count,
                nodeCounts.FirstOrDefault(x => x.Label == "Skill").Count,
                nodeCounts.FirstOrDefault(x => x.Label == "Company").Count,
                nodeCounts.FirstOrDefault(x => x.Label == "Project").Count,
                relationshipCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not read graph statistics.");
            throw;
        }
    }

    public async Task<IReadOnlyList<PersonCard>> SearchPeopleAsync(
        string term,
        CancellationToken cancellationToken = default)
    {
        const string cypher = """
            MATCH (p:Person)-[:HAS_SKILL]->(s:Skill)
            OPTIONAL MATCH (p)-[:WORKED_AT]->(c:Company)
            WHERE toLower(p.name) CONTAINS toLower($term)
               OR toLower(p.role) CONTAINS toLower($term)
               OR toLower(s.name) CONTAINS toLower($term)
               OR toLower(c.name) CONTAINS toLower($term)
            RETURN p.id AS id,
                   p.name AS name,
                   p.role AS role,
                   p.location AS location,
                   coalesce(c.name, 'Independent') AS company,
                   collect(DISTINCT s.name) AS skills
            ORDER BY p.name
            LIMIT 30
            """;

        try
        {
            await using var session = _driver.AsyncSession();
            var cursor = await session.RunAsync(cypher, new { term = term.Trim() });

            return await cursor.ToListAsync(r => new PersonCard(
                r["id"].As<string>(),
                r["name"].As<string>(),
                r["role"].As<string>(),
                r["location"].As<string>(),
                r["company"].As<string>(),
                r["skills"].As<List<string>>()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Person search failed for term {Term}.", term);
            throw;
        }
    }

    public async Task<PersonCard?> GetPersonAsync(
        string personId,
        CancellationToken cancellationToken = default)
    {
        const string cypher = """
            MATCH (p:Person {id: $personId})-[:HAS_SKILL]->(s:Skill)
            OPTIONAL MATCH (p)-[:WORKED_AT]->(c:Company)
            RETURN p.id AS id,
                   p.name AS name,
                   p.role AS role,
                   p.location AS location,
                   coalesce(c.name, 'Independent') AS company,
                   collect(DISTINCT s.name) AS skills
            """;

        try
        {
            await using var session = _driver.AsyncSession();
            var cursor = await session.RunAsync(cypher, new { personId });
            var records = await cursor.ToListAsync();

            if (records.Count == 0)
                return null;

            var r = records[0];
            return new PersonCard(
                r["id"].As<string>(),
                r["name"].As<string>(),
                r["role"].As<string>(),
                r["location"].As<string>(),
                r["company"].As<string>(),
                r["skills"].As<List<string>>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Person lookup failed for {PersonId}.", personId);
            throw;
        }
    }

    public async Task<IReadOnlyList<Recommendation>> GetRecommendationsAsync(
        string personId,
        CancellationToken cancellationToken = default)
    {
        const string cypher = """
            MATCH (me:Person {id: $personId})-[:HAS_SKILL]->(shared:Skill)<-[:HAS_SKILL]-(other:Person)
            WHERE me <> other
            WITH me, other, collect(DISTINCT shared.name) AS sharedSkills

            OPTIONAL MATCH (me)-[:CONTRIBUTED_TO]->(project:Project)<-[:CONTRIBUTED_TO]-(other)
            WITH other, sharedSkills, collect(DISTINCT project.name) AS sharedProjects

            OPTIONAL MATCH (other)-[:WORKED_AT]->(company:Company)
            RETURN other.id AS id,
                   other.name AS name,
                   other.role AS role,
                   coalesce(company.name, 'Independent') AS company,
                   sharedSkills,
                   [x IN sharedProjects WHERE x IS NOT NULL] AS sharedProjects,
                   size(sharedSkills) * 2 + size([x IN sharedProjects WHERE x IS NOT NULL]) AS connectionScore
            ORDER BY connectionScore DESC, other.name
            LIMIT 12
            """;

        try
        {
            await using var session = _driver.AsyncSession();
            var cursor = await session.RunAsync(cypher, new { personId });

            return await cursor.ToListAsync(r => new Recommendation(
                r["id"].As<string>(),
                r["name"].As<string>(),
                r["role"].As<string>(),
                r["company"].As<string>(),
                r["sharedSkills"].As<List<string>>(),
                r["sharedProjects"].As<List<string>>(),
                r["connectionScore"].As<int>()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Recommendation query failed for person {PersonId}.", personId);
            throw;
        }
    }
}
