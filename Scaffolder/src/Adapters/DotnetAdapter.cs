using Scaffolder.Services;

namespace Scaffolder.Adapters;

public class DotnetAdapter : IAdapter
{
    public string Name => "dotnet";
    public string Description => "Projets .NET (console, webapi, blazor, maui, classlib)";
    public string[] Languages => ["c#", "f#"];
    public string[] SubTemplates => ["console", "webapi", "blazor", "maui", "classlib"];
    public bool IsAvailable => ProcessService.CommandExists("dotnet");

    public async Task<(int ExitCode, string Message)> ScaffoldAsync(
        string name, string outputDir, string subTemplate, string? language)
    {
        var tpl = string.IsNullOrWhiteSpace(subTemplate) ? "console" : subTemplate;

        ConsoleService.Info($"  dotnet new {tpl} --name {name} --output {name}");
        Console.WriteLine();

        var result = await ProcessService.RunAsync(
            "dotnet",
            $"new {tpl} --name {name} --output {outputDir}",
            streamOutput: true
        );

        if (result.ExitCode == 0)
        {
            PostGenerateEnhancements(outputDir, tpl, name);
            return (0, $"Projet .NET {tpl} créé avec dotnet new");
        }

        return (result.ExitCode, result.Output);
    }

    private static void PostGenerateEnhancements(string outputDir, string template, string name)
    {
        var author = ConfigService.Get(ConfigService.Keys.Author) ?? "";
        var company = ConfigService.Get(ConfigService.Keys.Company) ?? "";

        if (!string.IsNullOrWhiteSpace(author) || !string.IsNullOrWhiteSpace(company))
            PatchProjectFile(outputDir, author, company);

        if (template == "webapi" || template == "blazor")
        {
            AddLogging(outputDir, name);
            AddValidation(outputDir, name);
        }

        AddDockerSupport(outputDir, name, template);
        AddTestProject(outputDir, name, template);
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

        if (insertions.Count > 0 && content.Contains("<PropertyGroup>"))
        {
            content = content.Replace(
                "<PropertyGroup>",
                "<PropertyGroup>\n" + string.Join("\n", insertions));
            File.WriteAllText(csproj, content);
            ConsoleService.Debug("Fichier .csproj enrichi avec auteur/entreprise.");
        }
    }

    private static void AddLogging(string outputDir, string name)
    {
        var programCs = Path.Combine(outputDir, "Program.cs");
        if (!File.Exists(programCs)) return;

        var content = File.ReadAllText(programCs);
        if (content.Contains("ILogger") || content.Contains("AddLogging")) return;

        if (content.Contains("var builder = WebApplication.CreateBuilder"))
        {
            content = content.Replace(
                "var builder = WebApplication.CreateBuilder(args);",
                "var builder = WebApplication.CreateBuilder(args);\n\n// Logging\nbuilder.Logging.ClearProviders();\nbuilder.Logging.AddConsole();");
            File.WriteAllText(programCs, content);
            ConsoleService.Debug("Logging configure dans Program.cs.");
        }
    }

    private static void AddValidation(string outputDir, string name)
    {
        var csproj = Directory.GetFiles(outputDir, "*.csproj", SearchOption.TopDirectoryOnly).FirstOrDefault();
        if (csproj == null) return;

        var content = File.ReadAllText(csproj);
        if (content.Contains("FluentValidation") || content.Contains("DataAnnotations")) return;

        ConsoleService.Debug("Pour la validation, ajoute FluentValidation ou utilise [Required] du package System.ComponentModel.Annotations.");
    }

    private static void AddDockerSupport(string outputDir, string name, string template)
    {
        var dockerFile = Path.Combine(outputDir, "Dockerfile");
        if (File.Exists(dockerFile)) return;

        var isWeb = template == "webapi" || template == "blazor";
        var sdkVersion = GetSdkVersion();

        var dockerfileContent = isWeb
            ? $"FROM mcr.microsoft.com/dotnet/aspnet:{sdkVersion} AS base\n" +
              $"WORKDIR /app\n" +
              $"EXPOSE 80\n" +
              $"EXPOSE 443\n\n" +
              $"FROM mcr.microsoft.com/dotnet/sdk:{sdkVersion} AS build\n" +
              $"WORKDIR /src\n" +
              $"COPY [\"*.csproj\", \"./\"]\n" +
              $"RUN dotnet restore\n\n" +
              $"COPY . .\n" +
              $"RUN dotnet build -c Release -o /app/build\n\n" +
              $"FROM build AS publish\n" +
              $"RUN dotnet publish -c Release -o /app/publish\n\n" +
              $"FROM base AS final\n" +
              $"WORKDIR /app\n" +
              $"COPY --from=publish /app/publish .\n" +
              $"ENTRYPOINT [\"dotnet\", \"{name}.dll\"]\n"
            : $"FROM mcr.microsoft.com/dotnet/sdk:{sdkVersion} AS build\n" +
              $"WORKDIR /src\n" +
              $"COPY [\"*.csproj\", \"./\"]\n" +
              $"RUN dotnet restore\n\n" +
              $"COPY . .\n" +
              $"RUN dotnet publish -c Release -o /app/publish\n\n" +
              $"FROM mcr.microsoft.com/dotnet/aspnet:{sdkVersion}\n" +
              $"WORKDIR /app\n" +
              $"COPY --from=build /app/publish .\n" +
              $"ENTRYPOINT [\"dotnet\", \"{name}.dll\"]\n";

        File.WriteAllText(dockerFile, dockerfileContent);

        var dockerignore = Path.Combine(outputDir, ".dockerignore");
        if (!File.Exists(dockerignore))
        {
            File.WriteAllText(dockerignore,
                "**/bin/\n**/obj/\n**/node_modules/\n**/.vs/\n**/*.user\n**/*.suo\ngit/\n.github/\n");
        }

        ConsoleService.Debug("Dockerfile et .dockerignore generes.");
    }

    private static void AddTestProject(string outputDir, string name, string template)
    {
        if (template == "console" || template == "maui") return;

        var testDir = Path.Combine(outputDir, $"{name}.Tests");
        if (Directory.Exists(testDir)) return;

        Directory.CreateDirectory(testDir);

        var csprojContent = $"""
            <Project Sdk="Microsoft.NET.Sdk">

              <PropertyGroup>
                <TargetFramework>net9.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <IsPackable>false</IsPackable>
                <IsTestProject>true</IsTestProject>
              </PropertyGroup>

              <ItemGroup>
                <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
                <PackageReference Include="xunit" Version="2.9.2" />
                <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
              </ItemGroup>

              <ItemGroup>
                <ProjectReference Include="..\\{name}\\{name}.csproj" />
              </ItemGroup>

            </Project>
            """;

        File.WriteAllText(Path.Combine(testDir, $"{name}.Tests.csproj"), csprojContent);

        var testContent = template == "webapi"
            ? $"using Xunit;\n\nnamespace {name}.Tests;\n\npublic class ApiTests\n{{\n    [Fact]\n    public void Endpoint_Should_Be_Available()\n    {{\n        // TODO: Ajouter les tests de l'API\n        Assert.True(true);\n    }}\n}}\n"
            : $"using Xunit;\n\nnamespace {name}.Tests;\n\npublic class BasicTests\n{{\n    [Fact]\n    public void Should_Work()\n    {{\n        // TODO: Ajouter les tests\n        Assert.True(true);\n    }}\n}}\n";

        File.WriteAllText(Path.Combine(testDir, $"BasicTests.cs"), testContent);
        ConsoleService.Debug($"Projet de tests {name}.Tests cree.");
    }

    private static string GetSdkVersion()
    {
        try
        {
            var result = ProcessService.RunAsync("dotnet", "--version", streamOutput: false).GetAwaiter().GetResult();
            var version = result.Output.Trim();
            var parts = version.Split('.');
            return parts.Length >= 2 ? $"{parts[0]}.{parts[1]}" : "9.0";
        }
        catch
        {
            return "9.0";
        }
    }
}
