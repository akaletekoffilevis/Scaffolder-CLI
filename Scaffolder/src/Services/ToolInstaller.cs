using System.Runtime.InteropServices;

namespace Scaffolder.Services;

/// <summary>
/// Installe automatiquement les outils manquants selon le système d'exploitation.
/// Chaque installation demande confirmation et peut nécessiter des droits administrateur.
/// </summary>
public static class ToolInstaller
{
    public sealed record InstallPlan(string Tool, string Command, string FileName, IEnumerable<string> Arguments, string? Notes);

    private static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    private static bool IsMac => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    private static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    /// <summary>Renvoie un plan d'installation pour l'outil donné, ou null si non supporté.</summary>
    public static InstallPlan? GetPlan(string tool)
    {
        tool = tool.Trim().ToLowerInvariant();

        // Normalisation des alias.
        if (tool is "node" or "nodejs") tool = "node";
        if (tool is "python3") tool = "python";

        return tool switch
        {
            "dotnet" => Plan("dotnet",
                linux: ("bash", ["-c", "curl -fsSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 10.0"]),
                mac:   ("bash", ["-c", "curl -fsSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 10.0"]),
                win:   ("powershell", ["-Command", "Invoke-WebRequest https://dot.net/v1/dotnet-install.ps1 -OutFile dotnet-install.ps1; ./dotnet-install.ps1 -Channel 10.0"])),
            "node" or "npm" => Plan("node",
                linux: ("bash", ["-c", "curl -fsSL https://deb.nodesource.com/setup_22.x | sudo -E bash - && sudo apt-get install -y nodejs"]),
                mac:   ("brew", ["install", "node"]),
                win:   ("winget", ["install", "--id", "OpenJS.NodeJS.LTS", "-e"])),
            "cargo" or "rust" => Plan("cargo",
                linux: ("bash", ["-c", "curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh -s -- -y"]),
                mac:   ("bash", ["-c", "curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh -s -- -y"]),
                win:   ("powershell", ["-Command", "Invoke-WebRequest https://win.rustup.rs/x86_64 -OutFile rustup-init.exe; ./rustup-init.exe -y"])),
            "go" => Plan("go",
                linux: ("bash", ["-c", "sudo apt-get update && sudo apt-get install -y golang-go || (cd /tmp && wget https://go.dev/dl/go1.23.0.linux-amd64.tar.gz && sudo tar -C /usr/local -xzf go1.23.0.linux-amd64.tar.gz)"]),
                mac:   ("brew", ["install", "go"]),
                win:   ("winget", ["install", "--id", "GoLang.Go", "-e"])),
            "python" => Plan("python",
                linux: ("bash", ["-c", "sudo apt-get update && sudo apt-get install -y python3 python3-pip"]),
                mac:   ("brew", ["install", "python@3.12"]),
                win:   ("winget", ["install", "--id", "Python.Python.3.12", "-e"])),
            "flutter" => Plan("flutter",
                linux: ("bash", ["-c", "sudo snap install flutter --classic || (git clone https://github.com/flutter/flutter.git ~/flutter && echo 'export PATH=\"$HOME/flutter/bin:$PATH\"' >> ~/.bashrc)"]),
                mac:   ("brew", ["install", "--cask", "flutter"]),
                win:   ("powershell", ["-Command", "git clone https://github.com/flutter/flutter.git C:\\flutter"])),
            "composer" => Plan("composer",
                linux: ("bash", ["-c", "php -r \"copy('https://getcomposer.org/installer','composer-setup.php');\" && php composer-setup.php && sudo mv composer.phar /usr/local/bin/composer"]),
                mac:   ("brew", ["install", "composer"]),
                win:   ("powershell", ["-Command", "Invoke-WebRequest https://getcomposer.org/Composer-Setup.exe -OutFile composer-setup.exe; ./composer-setup.exe /SILENT"])),
            "git" => Plan("git",
                linux: ("bash", ["-c", "sudo apt-get update && sudo apt-get install -y git"]),
                mac:   ("brew", ["install", "git"]),
                win:   ("winget", ["install", "--id", "Git.Git", "-e"])),
            "docker" => Plan("docker",
                linux: ("bash", ["-c", "curl -fsSL https://get.docker.com | sh"]),
                mac:   ("brew", ["install", "--cask", "docker"]),
                win:   ("winget", ["install", "--id", "Docker.DockerDesktop", "-e"])),
            "gh" => Plan("gh",
                linux: ("bash", ["-c", "sudo apt-get update && sudo apt-get install -y gh || (curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg | sudo dd of=/usr/share/keyrings/githubcli-archive-keyring.gpg && echo 'deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main' | sudo tee /etc/apt/sources.list.d/github-cli.list && sudo apt update && sudo apt install gh -y)"]),
                mac:   ("brew", ["install", "gh"]),
                win:   ("winget", ["install", "--id", "GitHub.cli", "-e"])),
            _ => null
        };
    }

    private static InstallPlan? Plan(string tool,
        (string FileName, string[] Arguments) linux,
        (string FileName, string[] Arguments) mac,
        (string FileName, string[] Arguments) win)
    {
        (string FileName, string[] Arguments) chosen;
        if (IsWindows) chosen = win;
        else if (IsMac) chosen = mac;
        else chosen = linux;

        return new InstallPlan(tool, $"{chosen.FileName} {string.Join(' ', chosen.Arguments)}",
            chosen.FileName, chosen.Arguments, null);
    }

    public static bool CanInstall(string tool) => GetPlan(tool) is not null;

    public static async Task<(int ExitCode, string Output)> InstallAsync(string tool, bool streamOutput = true)
    {
        var plan = GetPlan(tool);
        if (plan is null)
            return (1, $"Installation automatique non supportée pour '{tool}'. Installez-le manuellement.");

        ConsoleService.Info($"Installation de {tool} via : {plan.Command}");
        ConsoleService.Warning("Cette opération peut nécessiter des droits administrateur et une connexion réseau.");

        var result = await ProcessService.RunResultAsync(
            plan.FileName, plan.Arguments, streamOutput: streamOutput,
            timeout: TimeSpan.FromMinutes(30));

        if (result.Success)
            ConsoleService.Success($"{tool} installé avec succès.");
        else
            ConsoleService.Error($"Échec de l'installation de {tool} (code {result.ExitCode}).");

        return (result.ExitCode, result.Output);
    }

    /// <summary>Tente d'installer l'outil si manquant, après confirmation.</summary>
    public static async Task<bool> EnsureAsync(string tool, bool autoConfirm = false)
    {
        if (ProcessService.CommandExists(tool)) return true;
        if (!CanInstall(tool)) return false;

        if (!autoConfirm)
        {
            ConsoleService.Warning($"L'outil '{tool}' n'est pas installé.");
            if (!ConsoleService.Confirm($"Voulez-vous l'installer automatiquement ?", true))
                return false;
        }

        var (code, _) = await InstallAsync(tool);
        return code == 0 && ProcessService.CommandExists(tool);
    }
}
