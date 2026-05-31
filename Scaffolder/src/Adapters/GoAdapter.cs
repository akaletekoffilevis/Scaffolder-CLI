using Scaffolder.Services;

namespace Scaffolder.Adapters;

public class GoAdapter : IAdapter
{
    public string Name => "go";
    public string Description => "Projets Go (module)";
    public string[] Languages => ["go"];
    public string[] SubTemplates => ["go", "golang"];
    public bool IsAvailable => ProcessService.CommandExists("go");

    public async Task<(int ExitCode, string Message)> ScaffoldAsync(
        string name, string outputDir, string subTemplate, string? language)
    {
        ConsoleService.Info($"  go mod init {name}");
        Console.WriteLine();

        Directory.CreateDirectory(outputDir);

        var result = await ProcessService.RunAsync(
            "go",
            $"mod init {name}",
            workingDirectory: outputDir,
            streamOutput: true
        );

        if (result.ExitCode == 0)
        {
            var mainGo = "package main\n\nimport \"fmt\"\n\nfunc main() {\n\tfmt.Println(\"Hello, World!\")\n}\n";
            File.WriteAllText(Path.Combine(outputDir, "main.go"), mainGo);
            return (0, $"Projet Go créé avec go mod init");
        }

        return (result.ExitCode, result.Output);
    }
}
