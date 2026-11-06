using System.CommandLine;
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
        if (File.Exists(Paths.ConfigFile))
        {
            ConsoleService.Success("  Configuration trouvee");
            foreach (var line in File.ReadAllLines(Paths.ConfigFile))
                ConsoleService.Info($"    {line.Trim()}");
        }
        else
        {
            ConsoleService.Warning("  Aucune configuration (lance 'scaffold config init')");
        }
    }

    private static void CheckTool(string name, string versionCmd)
    {
        if (ProcessService.CommandExists(name))
            ConsoleService.Success($"  {name}");
        else
            ConsoleService.Warning($"  {name} (non installe)");
    }
}
