using System.CommandLine;
using System.Diagnostics;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class InfoCommand : Command
{
    public InfoCommand() : base("info", "Affiche les informations du projet courant")
    {
        SetAction(HandleInfo);
    }

    private static int HandleInfo(ParseResult pr)
    {
        var cwd = Directory.GetCurrentDirectory();
        var name = new DirectoryInfo(cwd).Name;

        ConsoleService.ShowLogo();
        Console.WriteLine();

        ConsoleService.Info($"Projet : {name}");
        ConsoleService.Info($"Dossier : {cwd}");

        DetectProjectType(cwd);

        Console.WriteLine();
        ConsoleService.Info("Outils disponibles :");

        CheckTool("dotnet", "dotnet --version");
        CheckTool("node", "node --version");
        CheckTool("npm", "npm --version");
        CheckTool("cargo", "cargo --version");
        CheckTool("go", "go version");
        CheckTool("flutter", "flutter --version");
        CheckTool("python3", "python3 --version");
        CheckTool("git", "git --version");

        return 0;
    }

    private static void DetectProjectType(string dir)
    {
        if (File.Exists(Path.Combine(dir, "package.json")))
        {
            var pkg = File.ReadAllText(Path.Combine(dir, "package.json"));
            var hasReact = pkg.Contains("\"react\"");
            var hasVue = pkg.Contains("\"vue\"");
            var hasNext = pkg.Contains("\"next\"");
            var hasVite = pkg.Contains("\"vite\"");

            var framework = hasNext ? "Next.js" : hasVue ? "Vue" : hasReact ? "React" : hasVite ? "Vite" : "Node.js";
            ConsoleService.Info($"Type : {framework}");
        }
        else if (Directory.GetFiles(dir, "*.csproj").Length > 0)
        {
            ConsoleService.Info("Type : .NET (C#)");
        }
        else if (File.Exists(Path.Combine(dir, "Cargo.toml")))
        {
            ConsoleService.Info("Type : Rust");
        }
        else if (File.Exists(Path.Combine(dir, "go.mod")))
        {
            ConsoleService.Info("Type : Go");
        }
        else
        {
            ConsoleService.Info("Type : inconnu");
        }
    }

    private static void CheckTool(string name, string versionCmd)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "which",
                Arguments = name,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = new Process { StartInfo = psi };
            proc.Start();
            proc.WaitForExit(2000);

            if (proc.ExitCode == 0)
                ConsoleService.Success($"  {name} disponible");
            else
                ConsoleService.Warning($"  {name} non installe");
        }
        catch
        {
            ConsoleService.Warning($"  {name} : erreur de detection");
        }
    }
}
