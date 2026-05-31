using Scaffolder.Services;

namespace Scaffolder.Adapters;

public class MixAdapter : IAdapter
{
    public string Name => "mix";
    public string Description => "Projets Elixir avec Mix";
    public string[] Languages => ["elixir"];
    public string[] SubTemplates => ["elixir", "mix", "phoenix"];
    public bool IsAvailable => ProcessService.CommandExists("mix");

    public async Task<(int ExitCode, string Message)> ScaffoldAsync(
        string name, string outputDir, string subTemplate, string? language)
    {
        var isPhoenix = subTemplate == "phoenix";
        var cmd = isPhoenix ? $"phx.new {name}" : $"new {name}";

        ConsoleService.Info($"  mix {cmd}");
        Console.WriteLine();

        var result = await ProcessService.RunAsync(
            "mix",
            cmd,
            workingDirectory: outputDir,
            streamOutput: true
        );

        if (result.ExitCode == 0)
            return (0, $"Projet Elixir créé avec mix {cmd}");

        return (result.ExitCode, result.Output);
    }
}
