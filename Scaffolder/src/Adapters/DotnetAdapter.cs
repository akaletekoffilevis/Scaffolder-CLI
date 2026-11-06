using System.Text;
using Scaffolder.Services;

namespace Scaffolder.Adapters;

public class DotnetAdapter : IAdapter
{
    private static readonly string[] TemplateNames = ["console", "webapi", "blazor", "maui", "classlib"];

    public string Name => "dotnet";
    public string Description => "Projets .NET (console, webapi, blazor, maui, classlib)";
    public string[] Languages => ["c#", "f#"];
    public string[] SubTemplates => TemplateNames;
    public bool IsAvailable => ProcessService.CommandExists("dotnet");

    public async Task<(int ExitCode, string Message)> ScaffoldAsync(
        string name, string outputDir, string subTemplate, string? language)
    {
        var tpl = string.IsNullOrWhiteSpace(subTemplate) ? "console" : subTemplate.Trim().ToLowerInvariant();

        if (!TemplateNames.Contains(tpl))
            return (2, $"Template .NET inconnu : {tpl}. Valeurs possibles : {string.Join(", ", TemplateNames)}");

        if (!ProcessService.CommandExists("dotnet"))
            return (127, "Le SDK .NET n'est pas installe (https://dotnet.microsoft.com/download).");

        var args = new List<string> { "new", tpl, "--name", name, "--output", outputDir };

        // F# n'est disponible que pour certains templates.
        var lang = (language ?? "").Trim().ToLowerInvariant();
        if (lang is "f#" or "fsharp" && tpl is "console" or "classlib")
        {
            args.Add("--language");
            args.Add("F#");
        }

        // Framework cible optionnel (ex: net10.0).
        var framework = ConfigService.Get(ConfigService.Keys.Framework);
        if (!string.IsNullOrWhiteSpace(framework) && framework.StartsWith("net", StringComparison.OrdinalIgnoreCase))
        {
            args.Add("--framework");
            args.Add(framework);
        }

        ConsoleService.Info($"  dotnet {string.Join(' ', args)}");
        Console.WriteLine();

        var result = await ProcessService.RunResultAsync("dotnet", args, streamOutput: true);

        if (!result.Success)
            return (result.ExitCode, result.Output);

        PostGenerateEnhancements(outputDir, tpl, name);
        return (0, $"Projet .NET {tpl} créé avec dotnet new");
    }

    private static void PostGenerateEnhancements(string outputDir, string template, string name)
    {
        var author = ConfigService.Get(ConfigService.Keys.Author) ?? "";
        var company = ConfigService.Get(ConfigService.Keys.Company) ?? "";

        if (!string.IsNullOrWhiteSpace(author) || !string.IsNullOrWhiteSpace(company))
            PatchProjectFile(outputDir, author, company);

        if (template is "webapi" or "blazor")
            AddLogging(outputDir);

        AddGitignore(outputDir);
        AddDockerSupport(outputDir, name, template);
        AddTestProject(outputDir, name, template);

        if (template is "webapi" or "blazor")
            AddSolutionIfMissing(outputDir, name);
    }

    private static void PatchProjectFile(string outputDir, string author, string company)
    {
        var csproj = Directory.GetFiles(outputDir, "*.csproj", SearchOption.TopDirectoryOnly).FirstOrDefault();
        if (csproj == null) return;

        var content = File.ReadAllText(csproj);
        var insertions = new List<string>();

        if (!string.IsNullOrWhiteSpace(author) && !content.Contains("<Authors>"))
            insertions.Add($"    <Authors>{author}</Authors>");
        if (!string.IsNullOrWhiteSpace(company) && !content.Contains("<Company>"))
            insertions.Add($"    <Company>{company}</Company>");

        if (insertions.Count == 0) return;

        var index = content.IndexOf("<PropertyGroup>", StringComparison.Ordinal);
        if (index < 0) return;

        var insertAt = index + "<PropertyGroup>".Length;
        content = content.Insert(insertAt, "\n" + string.Join("\n", insertions));
        File.WriteAllText(csproj, content);
        ConsoleService.Debug("Fichier .csproj enrichi avec auteur/entreprise.");
    }

    private static void AddLogging(string outputDir)
    {
        var programCs = Path.Combine(outputDir, "Program.cs");
        if (!File.Exists(programCs)) return;

        var content = File.ReadAllText(programCs);
        if (content.Contains("AddConsole", StringComparison.Ordinal)) return;

        const string marker = "var builder = WebApplication.CreateBuilder(args);";
        if (!content.Contains(marker, StringComparison.Ordinal)) return;

        content = content.Replace(marker, marker + "\n\nbuilder.Logging.ClearProviders();\nbuilder.Logging.AddConsole();");
        File.WriteAllText(programCs, content);
        ConsoleService.Debug("Logging configure dans Program.cs.");
    }

    private static void AddGitignore(string outputDir)
    {
        var path = Path.Combine(outputDir, ".gitignore");
        if (File.Exists(path)) return;

        File.WriteAllText(path,
            "bin/\nobj/\n.vs/\n*.user\n*.suo\n*.userosscache\nTestResults/\n[Dd]ebug/\n[Rr]elease/\n");
    }

    private static void AddDockerSupport(string outputDir, string name, string template)
    {
        // Les bibliotheques et Maui n'ont pas de sens en conteneur.
        if (template is "classlib" or "maui") return;

        var dockerFile = Path.Combine(outputDir, "Dockerfile");
        if (File.Exists(dockerFile)) return;

        var sdkVersion = GetSdkVersion();
        var projectName = FindProjectFileName(outputDir) ?? $"{name}.csproj";
        var assemblyName = Path.GetFileNameWithoutExtension(projectName);
        var isWeb = template is "webapi" or "blazor";

        var sb = new StringBuilder();
        sb.AppendLine($"FROM mcr.microsoft.com/dotnet/sdk:{sdkVersion} AS build");
        sb.AppendLine("WORKDIR /src");
        sb.AppendLine($"COPY [\"{projectName}\", \"./\"]");
        sb.AppendLine("RUN dotnet restore");
        sb.AppendLine();
        sb.AppendLine("COPY . .");
        sb.AppendLine("RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false");
        sb.AppendLine();

        if (isWeb)
        {
            sb.AppendLine($"FROM mcr.microsoft.com/dotnet/aspnet:{sdkVersion} AS final");
            sb.AppendLine("WORKDIR /app");
            sb.AppendLine("EXPOSE 8080");
            sb.AppendLine("ENV ASPNETCORE_URLS=http://+:8080");
        }
        else
        {
            // Application console : l'image runtime suffit (pas besoin d'ASP.NET).
            sb.AppendLine($"FROM mcr.microsoft.com/dotnet/runtime:{sdkVersion} AS final");
            sb.AppendLine("WORKDIR /app");
        }

        sb.AppendLine("COPY --from=build /app/publish .");
        sb.AppendLine($"ENTRYPOINT [\"dotnet\", \"{assemblyName}.dll\"]");

        File.WriteAllText(dockerFile, sb.ToString());

        var dockerignore = Path.Combine(outputDir, ".dockerignore");
        if (!File.Exists(dockerignore))
        {
            File.WriteAllText(dockerignore,
                "**/bin/\n**/obj/\n**/node_modules/\n**/.vs/\n**/*.user\n**/*.suo\n.git/\n.github/\n");
        }

        ConsoleService.Debug("Dockerfile et .dockerignore generes.");
    }

    private static string? FindProjectFileName(string outputDir) =>
        Directory.GetFiles(outputDir, "*.csproj", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .FirstOrDefault();

    private static void AddTestProject(string outputDir, string name, string template)
    {
        if (template is "console" or "maui" or "classlib") return;

        var projectFile = FindProjectFileName(outputDir);
        if (projectFile == null) return;

        var testDir = Path.Combine(outputDir, $"{name}.Tests");
        if (Directory.Exists(testDir)) return;

        Directory.CreateDirectory(testDir);

        var csprojContent = $"""
            <Project Sdk="Microsoft.NET.Sdk">

              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <IsPackable>false</IsPackable>
                <IsTestProject>true</IsTestProject>
              </PropertyGroup>

              <ItemGroup>
                <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
                <PackageReference Include="xunit" Version="2.9.2" />
                <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
              </ItemGroup>

              <ItemGroup>
                <ProjectReference Include="..\{projectFile}" />
              </ItemGroup>

            </Project>
            """;

        File.WriteAllText(Path.Combine(testDir, $"{name}.Tests.csproj"), csprojContent);

        var testContent = $$"""
            using Xunit;

            namespace {{SanitizeNamespace(name)}}.Tests;

            public class BasicTests
            {
                [Fact]
                public void Should_Work()
                {
                    // TODO: remplacer par un vrai test
                    Assert.True(true);
                }
            }
            """;

        File.WriteAllText(Path.Combine(testDir, "BasicTests.cs"), testContent);
        ConsoleService.Debug($"Projet de tests {name}.Tests cree.");
    }

    private static void AddSolutionIfMissing(string outputDir, string name)
    {
        var sln = Path.Combine(outputDir, $"{name}.sln");
        if (File.Exists(sln)) return;

        var result = ProcessService
            .RunResultAsync("dotnet", ["new", "sln", "-n", name, "-o", outputDir])
            .GetAwaiter().GetResult();

        if (!result.Success) return;

        var projects = Directory.GetFiles(outputDir, "*.csproj", SearchOption.AllDirectories);
        foreach (var project in projects)
        {
            ProcessService.RunResultAsync("dotnet", ["sln", sln, "add", project]).GetAwaiter().GetResult();
        }

        ConsoleService.Debug("Solution .sln generee.");
    }

    private static string SanitizeNamespace(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (char.IsLetterOrDigit(c) || c == '_' || c == '.')
                builder.Append(c);
            else
                builder.Append('_');
        }

        var result = builder.ToString();
        return string.IsNullOrEmpty(result) || char.IsDigit(result[0]) ? "_" + result : result;
    }

    private static string GetSdkVersion()
    {
        try
        {
            var result = ProcessService.RunResultAsync("dotnet", ["--version"])
                .GetAwaiter().GetResult();

            var version = result.StandardOutput.Trim();
            var parts = version.Split('.');

            if (parts.Length >= 2 && int.TryParse(parts[0], out var major) && int.TryParse(parts[1], out var minor))
            {
                // Les images ne sont pas publiees pour chaque version de SDK : on utilise la version majeure/mineure.
                return $"{major}.{minor}";
            }

            return "10.0";
        }
        catch
        {
            return "10.0";
        }
    }
}
