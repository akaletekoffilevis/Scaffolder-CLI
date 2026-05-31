using System.CommandLine;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class UpdateDepsCommand : Command
{
    public UpdateDepsCommand() : base("update-deps", "Met a jour les dependances du projet (npm, nuget, cargo, go, pip)")
    {
        SetAction(_ => Handle());
    }

    private static int Handle()
    {
        var cwd = Directory.GetCurrentDirectory();
        ConsoleService.Info("Analyse du projet...");
        Console.WriteLine();

        var hasPackageJson = File.Exists(Path.Combine(cwd, "package.json"));
        var hasCsproj = Directory.GetFiles(cwd, "*.csproj").Length > 0;
        var hasCargoToml = File.Exists(Path.Combine(cwd, "Cargo.toml"));
        var hasGoMod = File.Exists(Path.Combine(cwd, "go.mod"));
        var hasRequirements = File.Exists(Path.Combine(cwd, "requirements.txt")) ||
                              File.Exists(Path.Combine(cwd, "pyproject.toml"));

        var updated = 0;

        if (hasPackageJson)
        {
            ConsoleService.Info("Mise a jour des dependances npm...");
            var checkResult = ProcessService.RunAsync("npx", "npm-check-updates -u", cwd, streamOutput: true)
                .GetAwaiter().GetResult();
            if (checkResult.ExitCode != 0)
            {
                // Fallback: use npm update
                ConsoleService.Info("npm-check-updates non installe. Utilisation de npm update...");
                var npmResult = ProcessService.RunAsync("npm", "update", cwd, streamOutput: true)
                    .GetAwaiter().GetResult();
                if (npmResult.ExitCode == 0) updated++;
            }
            else
            {
                var installResult = ProcessService.RunAsync("npm", "install", cwd, streamOutput: true)
                    .GetAwaiter().GetResult();
                if (installResult.ExitCode == 0) updated++;
            }
            Console.WriteLine();
        }

        if (hasCsproj)
        {
            ConsoleService.Info("Mise a jour des packages NuGet...");
            var result = ProcessService.RunAsync("dotnet", "outdated --upgrade", cwd, streamOutput: true)
                .GetAwaiter().GetResult();
            if (result.ExitCode != 0)
            {
                // Fallback: update all packages
                var outdated = ProcessService.RunAsync("dotnet", "list package --outdated", cwd)
                    .GetAwaiter().GetResult();
                if (!string.IsNullOrWhiteSpace(outdated.Output))
                {
                    var lines = outdated.Output.Split('\n');
                    foreach (var line in lines)
                    {
                        var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2 && parts[0].Contains('.'))
                        {
                            var pkg = parts[0];
                            ProcessService.RunAsync("dotnet", $"add package {pkg}", cwd, streamOutput: false)
                                .GetAwaiter().GetResult();
                        }
                    }
                }
                updated++;
            }
            else
            {
                updated++;
            }
            Console.WriteLine();
        }

        if (hasCargoToml)
        {
            ConsoleService.Info("Mise a jour des dependances Cargo...");
            if (ProcessService.CommandExists("cargo-update"))
            {
                ProcessService.RunAsync("cargo", "install-update -a", cwd, streamOutput: true)
                    .GetAwaiter().GetResult();
            }
            else
            {
                var result = ProcessService.RunAsync("cargo", "update", cwd, streamOutput: true)
                    .GetAwaiter().GetResult();
                if (result.ExitCode == 0) updated++;
            }
            Console.WriteLine();
        }

        if (hasGoMod)
        {
            ConsoleService.Info("Mise a jour des dependances Go...");
            var result = ProcessService.RunAsync("go", "get -u ./...", cwd, streamOutput: true)
                .GetAwaiter().GetResult();
            if (result.ExitCode == 0)
            {
                ProcessService.RunAsync("go", "mod tidy", cwd, streamOutput: true)
                    .GetAwaiter().GetResult();
                updated++;
            }
            Console.WriteLine();
        }

        if (hasRequirements)
        {
            ConsoleService.Info("Mise a jour des dependances Python...");
            var pipResult = ProcessService.RunAsync("pip", "list --outdated --format=columns", cwd)
                .GetAwaiter().GetResult();
            if (!string.IsNullOrWhiteSpace(pipResult.Output))
            {
                var lines = pipResult.Output.Split('\n').Skip(2);
                foreach (var line in lines)
                {
                    var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 1 && !parts[0].Contains('-'))
                    {
                        ProcessService.RunAsync("pip", $"install --upgrade {parts[0]}", cwd, streamOutput: false)
                            .GetAwaiter().GetResult();
                    }
                }
                updated++;
            }
            Console.WriteLine();
        }

        if (updated == 0)
        {
            ConsoleService.Warning("Aucun projet reconnu. Verifie que tu es dans un dossier de projet.");
            ConsoleService.Info("Formats supportes : package.json, .csproj, Cargo.toml, go.mod, requirements.txt");
            return 1;
        }

        ConsoleService.Success($"Mise a jour terminee pour {updated} gestionnaire(s) de paquets.");
        return 0;
    }
}
