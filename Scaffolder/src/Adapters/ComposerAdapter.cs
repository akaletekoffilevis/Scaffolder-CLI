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
            "symfony" => "symfony/skeleton",
            _ => "laravel/laravel"
        };

        var parentDir = Directory.GetParent(outputDir)?.FullName ?? Environment.CurrentDirectory;
        var dirName = new DirectoryInfo(outputDir).Name;

        ConsoleService.Info($"  composer create-project {package} {dirName}");
        Console.WriteLine();

        // composer cree lui-meme le dossier cible.
        var result = await ProcessService.RunResultAsync(
            "composer",
            ["create-project", package, dirName],
            workingDirectory: parentDir,
            streamOutput: true,
            timeout: TimeSpan.FromMinutes(20));

        return result.Success
            ? (0, "Projet PHP créé avec composer create-project")
            : (result.ExitCode, result.Output);
    }
}
