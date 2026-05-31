using Scaffolder.Services;

namespace Scaffolder.Adapters;

public class RailsAdapter : IAdapter
{
    public string Name => "rails";
    public string Description => "Projets Ruby on Rails";
    public string[] Languages => ["ruby"];
    public string[] SubTemplates => ["rails", "ruby"];
    public bool IsAvailable => ProcessService.CommandExists("rails");

    public async Task<(int ExitCode, string Message)> ScaffoldAsync(
        string name, string outputDir, string subTemplate, string? language)
    {
        ConsoleService.Info($"  rails new {name}");
        Console.WriteLine();

        var result = await ProcessService.RunAsync(
            "rails",
            $"new {name}",
            workingDirectory: outputDir,
            streamOutput: true
        );

        if (result.ExitCode == 0)
            return (0, $"Projet Rails créé avec rails new");

        return (result.ExitCode, result.Output);
    }
}
