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
        var parentDir = Directory.GetParent(outputDir)?.FullName ?? Environment.CurrentDirectory;
        var dirName = new DirectoryInfo(outputDir).Name;
        var isPhoenix = subTemplate == "phoenix";

        string[] args = isPhoenix ? ["phx.new", dirName] : ["new", dirName];

        ConsoleService.Info($"  mix {string.Join(' ', args)}");
        Console.WriteLine();

        // mix nouveau projet cree lui-meme le dossier cible.
        var result = await ProcessService.RunResultAsync(
            "mix", args, workingDirectory: parentDir, streamOutput: true,
            timeout: TimeSpan.FromMinutes(20));

        return result.Success
            ? (0, "Projet Elixir créé avec mix")
            : (result.ExitCode, result.Output);
    }
}
