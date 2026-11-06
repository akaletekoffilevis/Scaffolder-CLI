using Scaffolder.Services;

namespace Scaffolder.Adapters;

public class NpmAdapter : IAdapter
{
    private static readonly (string Tpl, string Desc)[] Templates =
    [
        ("vite", "Vite + React/TypeScript"),
        ("next", "Next.js (App Router)"),
        ("react", "React (via Vite)"),
        ("vue", "Vue 3 + Vite"),
        ("nuxt", "Nuxt 3"),
        ("svelte", "SvelteKit"),
        ("solid", "SolidStart"),
    ];

    public string Name => "npm";
    public string Description => "Projets JavaScript/TypeScript (Vite, Next, Vue, Nuxt, Svelte, Solid...)";
    public string[] Languages => ["javascript", "typescript"];
    public string[] SubTemplates => Templates.Select(t => t.Tpl).ToArray();
    public bool IsAvailable => ProcessService.CommandExists("npm");

    public async Task<(int ExitCode, string Message)> ScaffoldAsync(
        string name, string outputDir, string subTemplate, string? language)
    {
        var tpl = string.IsNullOrWhiteSpace(subTemplate) ? "vite" : subTemplate.Trim().ToLowerInvariant();
        var isTypeScript = !string.Equals(language, "javascript", StringComparison.OrdinalIgnoreCase);

        var parentDir = Directory.GetParent(outputDir)?.FullName ?? Environment.CurrentDirectory;
        var dirName = new DirectoryInfo(outputDir).Name;

        var (fileName, args) = BuildCommand(tpl, dirName, isTypeScript);
        if (fileName == null)
            return (2, $"Template npm inconnu : {tpl}. Valeurs possibles : {string.Join(", ", SubTemplates)}");

        if (!ProcessService.CommandExists("npm"))
            return (127, "npm n'est pas installe (https://nodejs.org).");

        ConsoleService.Info($"  {fileName} {string.Join(' ', args)}");
        Console.WriteLine();

        var result = await ProcessService.RunResultAsync(
            fileName, args, workingDirectory: parentDir, streamOutput: true,
            timeout: TimeSpan.FromMinutes(15));

        if (!result.Success)
            return (result.ExitCode, result.Output);

        // Les generateurs npm n'ecrivent pas toujours de .gitignore : on en ajoute un minimal.
        if (Directory.Exists(outputDir))
            EnsureNodeGitignore(outputDir);

        return (0, $"Projet {tpl} créé avec succes");
    }

    private static (string? FileName, List<string> Args) BuildCommand(
        string tpl, string dirName, bool ts)
    {
        var npx = OperatingSystem.IsWindows() ? "npx.cmd" : "npx";

        return tpl switch
        {
            "vite" => ("npm", ["create", "vite@latest", dirName, "--", "--template", ts ? "react-ts" : "react"]),
            "react" => ("npm", ["create", "vite@latest", dirName, "--", "--template", ts ? "react-ts" : "react"]),

            "next" => (npx, new List<string>
            {
                "--yes", "create-next-app@latest", dirName,
                ts ? "--ts" : "--js",
                "--eslint", "--app", "--no-tailwind", "--no-src-dir",
                "--import-alias", "@/*", "--use-npm", "--yes"
            }),

            "vue" => ("npm", new List<string>
            {
                "create", "vue@latest", dirName, "--",
                ts ? "--ts" : "--js",
                "--router", "--pinia", "--eslint", "--prettier", "--force"
            }),

            "nuxt" => (npx, new List<string>
            {
                "--yes", "nuxi@latest", "init", dirName,
                "--package-manager", "npm", "--no-install", "--git-init", "--force"
            }),

            "svelte" => (npx, new List<string>
            {
                "--yes", "sv@latest", "create", dirName,
                "--template", "minimal", "--types", ts ? "ts" : "js",
                "--no-add-ons", "--install", "npm"
            }),

            "solid" => (npx, new List<string>
            {
                "--yes", "create-solid@latest", dirName,
                "--template", ts ? "ts" : "js", "--force"
            }),

            _ => (null, [])
        };
    }

    private static void EnsureNodeGitignore(string outputDir)
    {
        var path = Path.Combine(outputDir, ".gitignore");
        if (File.Exists(path)) return;

        File.WriteAllText(path,
            "node_modules/\ndist/\nbuild/\n.output/\n.nuxt/\n.svelte-kit/\n.env\n.env.local\ncoverage/\n.DS_Store\n");
    }
}
