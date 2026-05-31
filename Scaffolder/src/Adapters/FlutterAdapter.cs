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
        var tpl = subTemplate switch
        {
            "flutter-package" or "package" => "--template=package",
            "flutter-plugin" or "plugin" => "--template=plugin",
            _ => "--template=app"
        };

        ConsoleService.Info($"  flutter create {name} {tpl}");
        Console.WriteLine();

        var result = await ProcessService.RunAsync(
            "flutter",
            $"create {name} {tpl}",
            workingDirectory: outputDir,
            streamOutput: true
        );

        if (result.ExitCode == 0)
            return (0, $"Projet Flutter créé avec flutter create");

        return (result.ExitCode, result.Output);
    }
}
