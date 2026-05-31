using System.CommandLine;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class ProjectCommand : Command
{
    public ProjectCommand() : base("project", "Diagnostique et analyse un projet genere")
    {
        var doctorCmd = new Command("doctor", "Diagnostique un projet : dependances, SDK, conflits");
        doctorCmd.SetAction(_ => HandleDoctor());

        var upgradeCmd = new Command("upgrade", "Met a jour un projet vers la derniere version du template");
        upgradeCmd.SetAction(_ => HandleUpgrade());

        var analyzeCmd = new Command("analyze", "Analyse un dossier et detecte le template/language");
        analyzeCmd.SetAction(_ => HandleAnalyze());

        Add(doctorCmd);
        Add(upgradeCmd);
        Add(analyzeCmd);

        SetAction(_ =>
        {
            ConsoleService.Info("Sous-commandes : doctor, upgrade, analyze");
            return 0;
        });
    }

    private static int HandleDoctor()
    {
        var cwd = Directory.GetCurrentDirectory();
        ConsoleService.Info($"Diagnostic de : {cwd}");
        Console.WriteLine();

        var issues = new List<string>();

        // Check project files
        var files = Directory.GetFiles(cwd);
        var hasCsproj = files.Any(f => f.EndsWith(".csproj"));
        var hasPackageJson = files.Any(f => Path.GetFileName(f) == "package.json");
        var hasCargoToml = files.Any(f => Path.GetFileName(f) == "Cargo.toml");
        var hasGoMod = files.Any(f => Path.GetFileName(f) == "go.mod");
        var hasDockerfile = files.Any(f => Path.GetFileName(f) == "Dockerfile") ||
                           Directory.GetFiles(cwd, "Dockerfile*", SearchOption.AllDirectories).Any();
        var hasReadme = files.Any(f => Path.GetFileName(f).StartsWith("README", StringComparison.OrdinalIgnoreCase));

        // Check Git
        var isGitRepo = Directory.Exists(Path.Combine(cwd, ".git"));
        Console.WriteLine($"  Git init          : {(isGitRepo ? "✅" : "❌")}");

        // Check README
        Console.WriteLine($"  README            : {(hasReadme ? "✅" : "❌")}");

        // Check Docker
        Console.WriteLine($"  Dockerfile        : {(hasDockerfile ? "✅" : "❌")}");

        // Check .env
        var hasEnv = files.Any(f => Path.GetFileName(f) == ".env");
        Console.WriteLine($"  .env              : {(hasEnv ? "✅" : hasPackageJson || hasCsproj ? "⚠️  recommande" : "➖")}");

        // Check .gitignore
        var hasGitignore = files.Any(f => Path.GetFileName(f) == ".gitignore");
        Console.WriteLine($"  .gitignore        : {(hasGitignore ? "✅" : "❌")}");

        Console.WriteLine();

        // Tools check
        ConsoleService.Info("Outils installes :");
        CheckTool("dotnet", hasCsproj);
        CheckTool("node", hasPackageJson);
        CheckTool("npm", hasPackageJson);
        CheckTool("cargo", hasCargoToml);
        CheckTool("go", hasGoMod);

        if (!isGitRepo) issues.Add("Git non initialise. Lance : git init");
        if (!hasReadme && (hasCsproj || hasPackageJson)) issues.Add("README manquant. Cree un README.md");
        if (!hasGitignore) issues.Add(".gitignore manquant. Lance : scaffold github gitignore");

        Console.WriteLine();
        if (issues.Count == 0)
        {
            ConsoleService.Success("Aucun probleme detecte.");
        }
        else
        {
            ConsoleService.Warning($"{issues.Count} probleme(s) trouve(s) :");
            foreach (var issue in issues)
                ConsoleService.Info($"  - {issue}");
        }

        return 0;
    }

    private static void CheckTool(string tool, bool needed)
    {
        var exists = ProcessService.CommandExists(tool);
        var status = exists ? "✅" : "❌";
        var note = needed && !exists ? " (requis pour ce projet)" : "";
        Console.WriteLine($"  {tool,-12} {status}{note}");
    }

    private static int HandleUpgrade()
    {
        var cwd = Directory.GetCurrentDirectory();
        ConsoleService.Info($"Mise a jour du projet : {cwd}");
        Console.WriteLine();

        // Detect project type
        var hasCsproj = Directory.GetFiles(cwd, "*.csproj").Any();
        var hasPackageJson = File.Exists(Path.Combine(cwd, "package.json"));

        if (hasCsproj)
        {
            ConsoleService.Info("Projet .NET detecte. Mise a jour des packages...");
            foreach (var csproj in Directory.GetFiles(cwd, "*.csproj"))
            {
                var content = File.ReadAllText(csproj);
                // Update target framework
                if (content.Contains("net8.0"))
                {
                    content = content.Replace("net8.0", "net9.0");
                    File.WriteAllText(csproj, content);
                    ConsoleService.Success($"  {Path.GetFileName(csproj)} : net8.0 -> net9.0");
                }
            }
            ConsoleService.Info("  dotnet restore...");
            ProcessService.RunAsync("dotnet", "restore", cwd).Wait();
            ConsoleService.Success("Packages mis a jour.");
        }
        else if (hasPackageJson)
        {
            ConsoleService.Info("Projet Node.js detecte. Mise a jour des dependances...");
            ProcessService.RunAsync("npm", "update", cwd).Wait();
            ConsoleService.Success("Dependances mises a jour.");
        }
        else
        {
            ConsoleService.Warning("Type de projet non detecte.");
            ConsoleService.Info("Mise a jour manuelle recommandee : verifie la documentation du template.");
        }

        // Check for newer template version
        ConsoleService.Info("Verification de la version du template...");
        ConsoleService.Info("  (simule) Derniere version disponible : 2.0.0");
        ConsoleService.Info("  (simule) Version actuelle : 1.0.0");

        return 0;
    }

    private static int HandleAnalyze()
    {
        var cwd = Directory.GetCurrentDirectory();
        ConsoleService.Info($"Analyse de : {cwd}");
        Console.WriteLine();

        var files = Directory.GetFiles(cwd);

        if (files.Any(f => f.EndsWith(".csproj")))
        {
            var csproj = files.First(f => f.EndsWith(".csproj"));
            var content = File.ReadAllText(csproj);
            Console.WriteLine($"  Type      : Projet .NET");
            Console.WriteLine($"  Fichier   : {Path.GetFileName(csproj)}");
            Console.WriteLine($"  Framework : {DetectFramework(content)}");
            Console.WriteLine($"  Template  : console, webapi, blazor, maui, classlib");
        }
        else if (files.Any(f => Path.GetFileName(f) == "package.json"))
        {
            var json = File.ReadAllText(Path.Combine(cwd, "package.json"));
            Console.WriteLine($"  Type      : Projet Node.js");
            Console.WriteLine($"  Template  : {DetectNodeTemplate(json)}");
            Console.WriteLine($"  Manager   : {DetectPackageManager(cwd)}");
        }
        else if (files.Any(f => Path.GetFileName(f) == "Cargo.toml"))
        {
            Console.WriteLine($"  Type      : Projet Rust");
            Console.WriteLine($"  Template  : cargo");
        }
        else if (files.Any(f => Path.GetFileName(f) == "go.mod"))
        {
            Console.WriteLine($"  Type      : Projet Go");
            Console.WriteLine($"  Template  : go");
        }
        else if (files.Any(f => f.EndsWith(".py")))
        {
            Console.WriteLine($"  Type      : Projet Python");
            Console.WriteLine($"  Template  : python");
        }
        else
        {
            Console.WriteLine($"  Type      : Inconnu");
            Console.WriteLine($"  Fichiers  : {files.Length}");
        }

        Console.WriteLine($"  Git       : {Directory.Exists(Path.Combine(cwd, ".git"))}");
        Console.WriteLine($"  Docker    : {Directory.GetFiles(cwd, "Dockerfile*", SearchOption.AllDirectories).Any()}");

        return 0;
    }

    private static string DetectFramework(string csprojContent)
    {
        if (csprojContent.Contains("Microsoft.NET.Sdk.Web")) return "ASP.NET Core";
        if (csprojContent.Contains("Microsoft.NET.Sdk.BlazorWebAssembly")) return "Blazor WASM";
        if (csprojContent.Contains("Microsoft.NET.Sdk.Maui")) return "MAUI";
        if (csprojContent.Contains("UseWindowsForms")) return "Windows Forms";
        if (csprojContent.Contains("UseWPF")) return "WPF";
        return "Console";
    }

    private static string DetectNodeTemplate(string packageJson)
    {
        if (packageJson.Contains("\"next\"")) return "next";
        if (packageJson.Contains("\"nuxt\"")) return "nuxt";
        if (packageJson.Contains("\"vue\"")) return "vue";
        if (packageJson.Contains("\"react\"")) return "react";
        if (packageJson.Contains("\"svelte\"")) return "svelte";
        if (packageJson.Contains("\"solid-js\"")) return "solid";
        if (packageJson.Contains("\"vite\"")) return "vite";
        return "npm";
    }

    private static string DetectPackageManager(string dir)
    {
        if (File.Exists(Path.Combine(dir, "pnpm-lock.yaml"))) return "pnpm";
        if (File.Exists(Path.Combine(dir, "yarn.lock"))) return "yarn";
        if (File.Exists(Path.Combine(dir, "bun.lockb"))) return "bun";
        return "npm";
    }
}
