using Scaffolder.Services;

namespace Scaffolder.Adapters;

public class FlutterAdapter : IAdapter
{
    public string Name => "flutter";
    public string Description => "Projets Flutter/Dart (app, package, plugin)";
    public string[] Languages => ["dart"];
    public string[] SubTemplates => ["flutter", "flutter-app", "flutter-package", "flutter-plugin"];
    public bool IsAvailable => ProcessService.CommandExists("flutter");

    public async Task<(int ExitCode, string Message)> ScaffoldAsync(
        string name, string outputDir, string subTemplate, string? language)
    {
        var template = subTemplate switch
        {
            "flutter-package" or "package" => "package",
            "flutter-plugin" or "plugin" => "plugin",
            _ => "app"
        };

        var parentDir = Directory.GetParent(outputDir)?.FullName ?? Environment.CurrentDirectory;
        var dirName = new DirectoryInfo(outputDir).Name;

        ConsoleService.Info($"  flutter create {dirName} --template={template}");
        Console.WriteLine();

        // flutter create cree lui-meme le dossier cible.
        var result = await ProcessService.RunResultAsync(
            "flutter",
            ["create", dirName, $"--template={template}"],
            workingDirectory: parentDir,
            streamOutput: true,
            timeout: TimeSpan.FromMinutes(20));

        return result.Success
            ? (0, "Projet Flutter créé avec flutter create")
            : (result.ExitCode, result.Output);
    }
}
