using Scaffolder.Services;

namespace Scaffolder.Adapters;

public class ComposerAdapter : IAdapter
{
    public string Name => "composer";
    public string Description => "Projets PHP (Laravel, Symfony)";
    public string[] Languages => ["php"];
    public string[] SubTemplates => ["php", "laravel", "symfony", "composer"];
    public bool IsAvailable => ProcessService.CommandExists("composer");

    public async Task<(int ExitCode, string Message)> ScaffoldAsync(
        string name, string outputDir, string subTemplate, string? language)
    {
        var package = subTemplate switch
        {
            "laravel" => "laravel/laravel",
            "symfony" => "symfony/skeleton",
            _ => "laravel/laravel"
        };

        ConsoleService.Info($"  composer create-project {package} {name}");
        Console.WriteLine();

        var result = await ProcessService.RunAsync(
            "composer",
            $"create-project {package} {name}",
            workingDirectory: outputDir,
            streamOutput: true
        );

        if (result.ExitCode == 0)
            return (0, $"Projet PHP créé avec composer create-project");

        return (result.ExitCode, result.Output);
    }
}
