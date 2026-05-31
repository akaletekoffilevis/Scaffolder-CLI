using Scaffolder.Services;

namespace Scaffolder.Adapters;

public class CabalAdapter : IAdapter
{
    public string Name => "cabal";
    public string Description => "Projets Haskell avec Cabal";
    public string[] Languages => ["haskell"];
    public string[] SubTemplates => ["haskell", "cabal"];
    public bool IsAvailable => ProcessService.CommandExists("cabal");

    public async Task<(int ExitCode, string Message)> ScaffoldAsync(
        string name, string outputDir, string subTemplate, string? language)
    {
        ConsoleService.Info($"  cabal init {name}");
        Console.WriteLine();

        Directory.CreateDirectory(outputDir);

        var result = await ProcessService.RunAsync(
            "cabal",
            $"init {name} --non-interactive",
            workingDirectory: outputDir,
            streamOutput: true
        );

        if (result.ExitCode == 0)
            return (0, $"Projet Haskell créé avec cabal init");

        return (result.ExitCode, result.Output);
    }
}
