# Quick setup

```bash
export COGNODB_URI="bolt+s://<instance-id>.databases.cognodb.cloud"
export COGNODB_USERNAME="cognodb"
export COGNODB_PASSWORD="<password>"

dotnet restore
dotnet run --project Seed.csproj

dotnet run --project GraphExplorer.csproj
```

Do not commit secrets.
