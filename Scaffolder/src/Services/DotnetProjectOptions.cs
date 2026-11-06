using System.Text;

namespace Scaffolder.Services;

/// <summary>
/// Applique les choix --auth / --db / --arch a un projet .NET genere :
/// references NuGet, chaine de connexion et documentation de mise en route.
/// </summary>
public static class DotnetProjectOptions
{
    public static void Apply(
        string outputDir, string name, string? auth, string? db, string? arch, bool docker)
    {
        var csproj = Directory.GetFiles(outputDir, "*.csproj", SearchOption.TopDirectoryOnly).FirstOrDefault();
        if (csproj == null) return;

        var isWeb = IsWebProject(csproj);

        if (isWeb)
        {
            AddPackage(csproj, auth?.ToLowerInvariant() switch
            {
                "jwt" => "Microsoft.AspNetCore.Authentication.JwtBearer",
                _ => null
            }, "10.0.0");

            AddPackage(csproj, db?.ToLowerInvariant() switch
            {
                "sqlite" => "Microsoft.EntityFrameworkCore.Sqlite",
                "sqlserver" => "Microsoft.EntityFrameworkCore.SqlServer",
                "postgresql" or "postgres" => "Npgsql.EntityFrameworkCore.PostgreSQL",
                _ => null
            }, db?.ToLowerInvariant() is "postgresql" or "postgres" ? "10.0.0" : "10.0.0");

            if (db?.ToLowerInvariant() is not null and not "none")
                WriteAppSettings(outputDir, db);
        }

        WriteSetupDoc(outputDir, name, auth, db, arch);

        if (docker)
            EnsureDockerfile(outputDir, name, csproj);
    }

    private static bool IsWebProject(string csproj)
    {
        try
        {
            var content = File.ReadAllText(csproj);
            return content.Contains("Microsoft.NET.Sdk.Web", StringComparison.OrdinalIgnoreCase);
        }
        catch (IOException)
        {
            return false;
        }
    }

    private static void AddPackage(string csproj, string? package, string version)
    {
        if (string.IsNullOrWhiteSpace(package)) return;

        var content = File.ReadAllText(csproj);
        if (content.Contains($"\"{package}\"", StringComparison.OrdinalIgnoreCase)) return;

        const string marker = "</Project>";
        var index = content.LastIndexOf(marker, StringComparison.Ordinal);
        if (index < 0) return;

        var block = new StringBuilder();
        if (!content.Contains("<ItemGroup>", StringComparison.Ordinal))
            block.AppendLine("  <ItemGroup>");

        if (!content.Contains("<ItemGroup>", StringComparison.Ordinal))
        {
            block.AppendLine($"    <PackageReference Include=\"{package}\" Version=\"{version}\" />");
            block.AppendLine("  </ItemGroup>");
        }
        else
        {
            // Ajoute dans le premier ItemGroup existant.
            const string itemGroupEnd = "</ItemGroup>";
            var start = content.IndexOf("<ItemGroup>", StringComparison.Ordinal);
            var end = content.IndexOf(itemGroupEnd, start, StringComparison.Ordinal);
            if (end < 0) return;

            var reference = $"    <PackageReference Include=\"{package}\" Version=\"{version}\" />";
            content = content.Insert(end, reference + Environment.NewLine);
            File.WriteAllText(csproj, content);
            ConsoleService.Debug($"Package {package} ajoute.");
            return;
        }

        content = content.Insert(index, block.ToString());
        File.WriteAllText(csproj, content);
        ConsoleService.Debug($"Package {package} ajoute.");
    }

    private static void WriteAppSettings(string outputDir, string db)
    {
        var path = Path.Combine(outputDir, "appsettings.json");
        var connectionString = db.ToLowerInvariant() switch
        {
            "sqlite" => "Data Source=app.db",
            "sqlserver" => "Server=localhost;Database=app;User Id=sa;Password=ChangeMe!;TrustServerCertificate=True",
            "postgresql" or "postgres" => "Host=localhost;Database=app;Username=postgres;Password=postgres",
            _ => ""
        };

        var json = $$"""
            {
              "ConnectionStrings": {
                "Default": "{{connectionString}}"
              },
              "Logging": {
                "LogLevel": {
                  "Default": "Information",
                  "Microsoft.AspNetCore": "Warning"
                }
              },
              "AllowedHosts": "*"
            }
            """;

        try
        {
            if (!File.Exists(path))
                File.WriteAllText(path, json);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ConsoleService.Debug($"appsettings.json non ecrit : {ex.Message}");
        }
    }

    private static void WriteSetupDoc(
        string outputDir, string name, string? auth, string? db, string? arch)
    {
        var docsDir = Path.Combine(outputDir, "docs");
        Directory.CreateDirectory(docsDir);

        var sb = new StringBuilder();
        sb.AppendLine($"# {name} - mise en route");
        sb.AppendLine();
        sb.AppendLine("## Architecture");
        sb.AppendLine();
        sb.AppendLine($"Choix : **{arch ?? "mvc"}**");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(auth) && auth != "none")
        {
            sb.AppendLine("## Authentification");
            sb.AppendLine();
            sb.AppendLine($"Methode : **{auth}**");
            sb.AppendLine();
            if (auth.Equals("jwt", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine("Ajoute une cle secrete dans `appsettings.Development.json` :");
                sb.AppendLine();
                sb.AppendLine("```json");
                sb.AppendLine("{ \"Jwt\": { \"Key\": \"<cle-de-32-caracteres-minimum>\" } }");
                sb.AppendLine("```");
                sb.AppendLine();
            }
        }

        if (!string.IsNullOrWhiteSpace(db) && db != "none")
        {
            sb.AppendLine("## Base de donnees");
            sb.AppendLine();
            sb.AppendLine($"Fournisseur : **{db}**");
            sb.AppendLine();
            sb.AppendLine("La chaine de connexion se trouve dans `appsettings.json` (`ConnectionStrings:Default`).");
            sb.AppendLine();
            sb.AppendLine("```bash");
            sb.AppendLine("dotnet ef migrations add InitialCreate");
            sb.AppendLine("dotnet ef database update");
            sb.AppendLine("```");
            sb.AppendLine();
        }

        sb.AppendLine("## Commandes utiles");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine("dotnet build");
        sb.AppendLine("dotnet run");
        sb.AppendLine("dotnet test");
        sb.AppendLine("```");
        sb.AppendLine();

        try
        {
            File.WriteAllText(Path.Combine(docsDir, "SETUP.md"), sb.ToString());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ConsoleService.Debug($"Documentation non ecrite : {ex.Message}");
        }
    }

    private static void EnsureDockerfile(string outputDir, string name, string csproj)
    {
        var dockerfile = Path.Combine(outputDir, "Dockerfile");
        if (File.Exists(dockerfile)) return;

        var projectFile = Path.GetFileName(csproj);
        var assembly = Path.GetFileNameWithoutExtension(csproj);
        var isWeb = IsWebProject(csproj);

        var sb = new StringBuilder();
        sb.AppendLine("FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build");
        sb.AppendLine("WORKDIR /src");
        sb.AppendLine($"COPY [\"{projectFile}\", \"./\"]");
        sb.AppendLine("RUN dotnet restore");
        sb.AppendLine("COPY . .");
        sb.AppendLine("RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false");
        sb.AppendLine();
        sb.AppendLine(isWeb
            ? "FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final"
            : "FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final");
        sb.AppendLine("WORKDIR /app");
        if (isWeb)
        {
            sb.AppendLine("EXPOSE 8080");
            sb.AppendLine("ENV ASPNETCORE_URLS=http://+:8080");
        }
        sb.AppendLine("COPY --from=build /app/publish .");
        sb.AppendLine($"ENTRYPOINT [\"dotnet\", \"{assembly}.dll\"]");

        File.WriteAllText(dockerfile, sb.ToString());
        ConsoleService.Debug("Dockerfile genere (option --docker).");
    }
}
