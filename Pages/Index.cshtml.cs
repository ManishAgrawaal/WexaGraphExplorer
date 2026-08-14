using GraphExplorer.Models;
using GraphExplorer.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GraphExplorer.Pages;

public sealed class IndexModel : PageModel
{
    private readonly IGraphService _graph;

    public IndexModel(IGraphService graph) => _graph = graph;

    public GraphStats Stats { get; private set; } = new(0, 0, 0, 0, 0);
    public IReadOnlyList<PersonCard> SearchResults { get; private set; } = [];
    public IReadOnlyList<Recommendation> Recommendations { get; private set; } = [];
    public PersonCard? SelectedPerson { get; private set; }
    public string SearchTerm { get; private set; } = "";
    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync() => await LoadStatsAsync();

    public async Task OnPostSearchAsync(string searchTerm)
    {
        SearchTerm = searchTerm?.Trim() ?? "";
        await LoadStatsAsync();

        if (string.IsNullOrWhiteSpace(SearchTerm))
            return;

        try
        {
            SearchResults = await _graph.SearchPeopleAsync(SearchTerm);
        }
        catch
        {
            ErrorMessage = "The graph database is currently unavailable. Please verify the CognoDB connection and try again.";
        }
    }

    public async Task OnPostRecommendationsAsync(string personId)
    {
        await LoadStatsAsync();

        try
        {
            Recommendations = await _graph.GetRecommendationsAsync(personId);
            SelectedPerson = await _graph.GetPersonAsync(personId);

            if (SelectedPerson is null)
                ErrorMessage = "The selected person could not be found.";
        }
        catch
        {
            ErrorMessage = "The graph database is currently unavailable. Please verify the CognoDB connection and try again.";
        }
    }

    private async Task LoadStatsAsync()
    {
        try
        {
            Stats = await _graph.GetStatsAsync();
        }
        catch
        {
            ErrorMessage = "Unable to connect to CognoDB. Check COGNODB_URI/COGNODB_PASSWORD.";
        }
    }
}
