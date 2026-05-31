using Scaffolder.Services;

namespace Scaffolder.Adapters;

public class ZigAdapter : IAdapter
{
    public string Name => "zig";
    public string Description => "Projets Zig";
    public string[] Languages => ["zig"];
    public string[] SubTemplates => ["zig"];
    public bool IsAvailable => ProcessService.CommandExists("zig");

    public async Task<(int ExitCode, string Message)> ScaffoldAsync(
        string name, string outputDir, string subTemplate, string? language)
    {
        ConsoleService.Info($"  zig init");
        Console.WriteLine();

        Directory.CreateDirectory(outputDir);

        var result = await ProcessService.RunAsync(
            "zig",
            "init",
            workingDirectory: outputDir,
            streamOutput: true
        );

        if (result.ExitCode == 0)
            return (0, $"Projet Zig créé avec zig init");

        return (result.ExitCode, result.Output);
    }
}
