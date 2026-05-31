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
            return (0, $"Projet .NET {tpl} créé avec dotnet new");
        }

        return (result.ExitCode, result.Output);
    }
}
