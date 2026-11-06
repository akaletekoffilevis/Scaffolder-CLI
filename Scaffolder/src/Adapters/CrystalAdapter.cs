using Scaffolder.Services;

namespace Scaffolder.Adapters;

public class CrystalAdapter : IAdapter
{
    public string Name => "crystal";
    public string Description => "Projets Crystal";
    public string[] Languages => ["crystal"];
    public string[] SubTemplates => ["crystal", "crystal-init"];
    public bool IsAvailable => ProcessService.CommandExists("crystal");

    public async Task<(int ExitCode, string Message)> ScaffoldAsync(
        string name, string outputDir, string subTemplate, string? language)
    {
        name = name ?? throw new ArgumentNullException(nameof(name));
        ConsoleService.Info($"  crystal init app {name}...");
        Console.WriteLine();

        Directory.CreateDirectory(outputDir);

        // Try crystal init first
        var result = await ProcessService.RunAsync(
            "crystal",
            $"init app {name}",
            workingDirectory: Path.GetDirectoryName(outputDir) ?? Directory.GetCurrentDirectory(),
            streamOutput: true
        );

        if (result.ExitCode == 0)
        {
            return (0, $"Projet Crystal '{name}' cree avec crystal init");
        }

        // Fallback: create manually
        var srcDir = Path.Combine(outputDir, "src");
        Directory.CreateDirectory(srcDir);

        File.WriteAllText(Path.Combine(outputDir, "shard.yml"),
            $"""
name: {name}
version: 0.1.0

authors:
  - {Environment.UserName}

targets:
  {name}:
    main: src/{name.Replace("-", "_")}.cr
""");

        File.WriteAllText(Path.Combine(srcDir, $"{name.Replace("-", "_")}.cr"),
            $"""
# TODO: Write documentation for `/{name}`
module {ToPascalCase(name)}
  VERSION = "0.1.0"

  def self.main
    puts "Hello, {name}!"
  end
end

{ToPascalCase(name)}.main
""");

        File.WriteAllText(Path.Combine(outputDir, ".gitignore"),
            "# Crystal\n/bin/\n/lib/\n.shards/\n*.dwarf\n");
        File.WriteAllText(Path.Combine(outputDir, "README.md"),
            $"# {name}\n\nProject Crystal.\n\n## Usage\n\n```bash\ncrystal run src/{name.Replace("-", "_")}.cr\n```\n");

        return (0, $"Projet Crystal cree avec succes");
    }

    private static string ToPascalCase(string input)
    {
        return string.Join("", input.Split('-', '_')
            .Select(part => char.ToUpper(part[0]) + part[1..]));
    }
}
