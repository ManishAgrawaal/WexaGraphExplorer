using GraphExplorer.Data;

var uri = Environment.GetEnvironmentVariable("COGNODB_URI");
var password = Environment.GetEnvironmentVariable("COGNODB_PASSWORD");
var username = Environment.GetEnvironmentVariable("COGNODB_USERNAME") ?? "cognodb";

if (string.IsNullOrWhiteSpace(uri) || string.IsNullOrWhiteSpace(password))
{
    Console.Error.WriteLine("Set COGNODB_URI and COGNODB_PASSWORD before running the seed script.");
    return 1;
}

await SeedData.RunAsync(uri, username, password);
Console.WriteLine("Seed completed.");
return 0;
