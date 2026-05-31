using System.CommandLine;
using System.Diagnostics;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class DoctorCommand : Command
{
    public DoctorCommand() : base("doctor", "Diagnostique l'environnement et la configuration")
    {
        SetAction(HandleDoctor);
    }

    private static int HandleDoctor(ParseResult pr)
    {
        ConsoleService.ShowLogo();
        Console.WriteLine();
        ConsoleService.Info("🔍 Diagnostic de l'environnement :");
        Console.WriteLine();

        ConsoleService.Info("Systeme :");
        ConsoleService.Info($"  OS : {GetOS()}");
        ConsoleService.Info($"  Arch : {GetArch()}");
        ConsoleService.Info($"  Shell : {GetShell()}");
        Console.WriteLine();

        ConsoleService.Info("Configuration :");
        CheckConfig();
        Console.WriteLine();

        ConsoleService.Info("Outils :");
        CheckTool("dotnet", "dotnet --version");
        CheckTool("node", "node --version");
        CheckTool("npm", "npm --version");
        CheckTool("cargo", "cargo --version");
        CheckTool("go", "go version");
        CheckTool("flutter", "flutter --version");
        CheckTool("python3", "python3 --version");
        CheckTool("git", "git --version");
        CheckTool("docker", "docker --version");
        CheckTool("gh", "gh --version");
        Console.WriteLine();

        ConsoleService.Info("Version Scaffolder : v" + UpdateService.CurrentVersion);
        return 0;
    }

    private static string GetOS()
    {
        if (OperatingSystem.IsWindows()) return "Windows";
        if (OperatingSystem.IsMacOS()) return "macOS";
        return "Linux";
    }

    private static string GetArch() =>
        System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString();

    private static string GetShell() =>
        Environment.GetEnvironmentVariable("SHELL") ?? "inconnu";

    private static void CheckConfig()
    {
        var configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".scaffolder");
        var configFile = Path.Combine(configDir, "config.json");

        if (File.Exists(configFile))
        {
            ConsoleService.Success("  Configuration trouvee");
            foreach (var line in File.ReadAllLines(configFile))
                ConsoleService.Info($"    {line.Trim()}");
        }
        else
        {
            ConsoleService.Warning("  Aucune configuration (lance 'scaffold config init')");
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
                ConsoleService.Success($"  {name}");
            else
                ConsoleService.Warning($"  {name} (non installe)");
        }
        catch
        {
            ConsoleService.Warning($"  {name} (erreur detection)");
        }
    }
}
