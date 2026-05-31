using Scaffolder.Services;

namespace Scaffolder.Adapters;

public class NpmAdapter : IAdapter
{
    private static readonly (string Tpl, string Desc)[] Templates =
    [
        ("vite", "Vite + React/TypeScript"),
        ("next", "Next.js"),
        ("react", "Create React App"),
        ("vue", "Vue 3 + Vite"),
        ("nuxt", "Nuxt 3"),
        ("svelte", "SvelteKit"),
        ("solid", "SolidStart"),
    ];

    public string Name => "npm";
    public string Description => "Projets JavaScript/TypeScript (Vite, Next, Vue, Svelte...)";
    public string[] Languages => ["javascript", "typescript"];
    public string[] SubTemplates => Templates.Select(t => t.Tpl).ToArray();
    public bool IsAvailable => ProcessService.CommandExists("npm");

    public async Task<(int ExitCode, string Message)> ScaffoldAsync(
        string name, string outputDir, string subTemplate, string? language)
    {
        var tpl = string.IsNullOrWhiteSpace(subTemplate) ? "vite" : subTemplate;
        var parentDir = Directory.GetParent(outputDir)?.FullName ?? ".";
        var dirName = new DirectoryInfo(outputDir).Name;

        ConsoleService.Info($"  npm create {tpl} {dirName} --cwd {parentDir}");
        Console.WriteLine();

        var result = await ProcessService.RunAsync(
            "npm",
            $"create {tpl}@latest {dirName}",
            streamOutput: true,
            workingDirectory: parentDir
        );

        if (result.ExitCode == 0)
        {
            return (0, $"Projet {tpl} créé avec npm create");
        }

        return (result.ExitCode, result.Output);
    }
}
