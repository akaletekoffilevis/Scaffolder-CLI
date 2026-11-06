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
        var parentDir = Directory.GetParent(outputDir)?.FullName ?? Environment.CurrentDirectory;
        var dirName = new DirectoryInfo(outputDir).Name;

        ConsoleService.Info($"  rails new {dirName}");
        Console.WriteLine();

        // rails new cree lui-meme le dossier cible.
        var result = await ProcessService.RunResultAsync(
            "rails",
            ["new", dirName],
            workingDirectory: parentDir,
            streamOutput: true,
            timeout: TimeSpan.FromMinutes(20));

        return result.Success
            ? (0, "Projet Rails créé avec rails new")
            : (result.ExitCode, result.Output);
    }
}
