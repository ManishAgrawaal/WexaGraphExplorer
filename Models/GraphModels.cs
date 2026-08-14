namespace GraphExplorer.Models;

public sealed record PersonCard(
    string Id,
    string Name,
    string Role,
    string Location,
    string Company,
    IReadOnlyList<string> Skills);

public sealed record Recommendation(
    string Id,
    string Name,
    string Role,
    string Company,
    IReadOnlyList<string> SharedSkills,
    IReadOnlyList<string> SharedProjects,
    int ConnectionScore);

public sealed record GraphStats(
    int People,
    int Skills,
    int Companies,
    int Projects,
    int Relationships);
