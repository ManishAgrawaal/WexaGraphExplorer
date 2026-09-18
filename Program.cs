using GraphExplorer.Services;
using Neo4j.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
var uri = Environment.GetEnvironmentVariable("COGNODB_URI")
    ?? builder.Configuration["CognoDb:Uri"]
    ?? throw new InvalidOperationException("COGNODB_URI is missing.");

var username = Environment.GetEnvironmentVariable("COGNODB_USERNAME")
    ?? builder.Configuration["CognoDb:Username"]
    ?? "cognodb";

var password = Environment.GetEnvironmentVariable("COGNODB_PASSWORD")
    ?? builder.Configuration["CognoDb:Password"]
    ?? throw new InvalidOperationException("COGNODB_PASSWORD is missing.");

builder.Services.AddSingleton<IDriver>(_ =>
    GraphDatabase.Driver(uri, AuthTokens.Basic(username, password)));

builder.Services.AddScoped<IGraphService, GraphService>();
builder.Services.AddSingleton<StartupHealth>();

var app = builder.Build();

app.UseExceptionHandler("/Error");
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

app.MapGet("/health", async (StartupHealth health) =>
{
    var ok = await health.CheckAsync();
    return ok ? Results.Ok(new { status = "healthy" }) : Results.StatusCode(503);
});

app.Run();

public sealed class StartupHealth
{
    private readonly IDriver _driver;
    public StartupHealth(IDriver driver) => _driver = driver;

    public async Task<bool> CheckAsync()
    {
        try
        {
            await _driver.VerifyConnectivityAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
