using Scaffolder.Services;

namespace Scaffolder.Adapters;

public class CargoAdapter : IAdapter
{
    public string Name => "cargo";
    public string Description => "Projets Rust (binary, library)";
    public string[] Languages => ["rust"];
    public string[] SubTemplates => ["rust", "cargo-bin", "cargo-lib"];
    public bool IsAvailable => ProcessService.CommandExists("cargo");

    public async Task<(int ExitCode, string Message)> ScaffoldAsync(
        string name, string outputDir, string subTemplate, string? language)
    {
        var tpl = subTemplate switch
        {
            "cargo-lib" or "lib" => "--lib",
            _ => "--bin"
        };

        ConsoleService.Info($"  cargo init {name} {tpl}");
        Console.WriteLine();

        Directory.CreateDirectory(outputDir);

        var result = await ProcessService.RunAsync(
            "cargo",
            $"init --name {name} {tpl}",
            workingDirectory: outputDir,
            streamOutput: true
        );

        if (result.ExitCode == 0)
            return (0, $"Projet Rust créé avec cargo init");

        return (result.ExitCode, result.Output);
    }
}
