using Scaffolder.Services;

namespace Scaffolder.Adapters;

public class GradleAdapter : IAdapter
{
    public string Name => "gradle";
    public string Description => "Projets Kotlin/Java/Groovy avec Gradle";
    public string[] Languages => ["kotlin", "java", "groovy"];
    public string[] SubTemplates => ["gradle", "kotlin", "java"];
    public bool IsAvailable => ProcessService.CommandExists("gradle");

    public async Task<(int ExitCode, string Message)> ScaffoldAsync(
        string name, string outputDir, string subTemplate, string? language)
    {
        var dsL = language?.ToLowerInvariant() switch
        {
            "kotlin" => "--dsl kotlin",
            "groovy" => "--dsl groovy",
            _ => "--dsl kotlin"
        };

        ConsoleService.Info($"  gradle init {dsL}");
        Console.WriteLine();

        Directory.CreateDirectory(outputDir);

        var result = await ProcessService.RunAsync(
            "gradle",
            $"init {dsL} --project-name {name}",
            workingDirectory: outputDir,
            streamOutput: true
        );

        if (result.ExitCode == 0)
            return (0, $"Projet Gradle créé avec gradle init");

        return (result.ExitCode, result.Output);
    }
}
