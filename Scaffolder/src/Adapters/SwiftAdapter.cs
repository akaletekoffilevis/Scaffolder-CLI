using Scaffolder.Services;

namespace Scaffolder.Adapters;

public class SwiftAdapter : IAdapter
{
    public string Name => "swift";
    public string Description => "Projets Swift avec Swift Package Manager";
    public string[] Languages => ["swift"];
    public string[] SubTemplates => ["swift", "swift-executable", "swift-library"];
    public bool IsAvailable => ProcessService.CommandExists("swift");

    public async Task<(int ExitCode, string Message)> ScaffoldAsync(
        string name, string outputDir, string subTemplate, string? language)
    {
        var type = subTemplate switch
        {
            "swift-library" or "library" => "--type library",
            _ => "--type executable"
        };

        ConsoleService.Info($"  swift package init {type}");
        Console.WriteLine();

        Directory.CreateDirectory(outputDir);

        var result = await ProcessService.RunAsync(
            "swift",
            $"package init {type} --name {name}",
            workingDirectory: outputDir,
            streamOutput: true
        );

        if (result.ExitCode == 0)
            return (0, $"Projet Swift créé avec swift package init");

        return (result.ExitCode, result.Output);
    }
}
