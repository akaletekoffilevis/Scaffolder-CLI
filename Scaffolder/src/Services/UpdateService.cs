using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace Scaffolder.Services;

public static class UpdateService
{
    private const string RepoOwner = "akaletekoffilevis";
    private const string RepoName = "Scaffolder-CLI";
    private const string GitHubApi = "https://api.github.com/repos";

    public static string CurrentVersion
    {
        get
        {
            try
            {
                var v = Assembly.GetExecutingAssembly()
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                    ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                    ?? "2.0.0";
                return v.TrimStart('v');
            }
            catch { return "2.0.0"; }
        }
    }

    public static async Task<(string? LatestVersion, string? DownloadUrl)> CheckForUpdateAsync()
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Scaffolder");
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            var url = $"{GitHubApi}/{RepoOwner}/{RepoName}/releases/latest";
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return (null, null);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var tag = doc.RootElement.GetProperty("tag_name").GetString() ?? "";

            var assets = doc.RootElement.GetProperty("assets");
            string? downloadUrl = null;

            var arch = RuntimeInfo.Arch;
            var os = RuntimeInfo.OS;
            var rid = RuntimeInfo.RID;

            // Try exact RID match first, then fallback to arch+os
            foreach (var asset in assets.EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString() ?? "";
                if (name.Contains(rid) && name.EndsWith(".tar.gz"))
                {
                    downloadUrl = asset.GetProperty("browser_download_url").GetString();
                    break;
                }
                if (name.Contains(arch) && name.Contains(os) && name.EndsWith(".tar.gz") && downloadUrl == null)
                {
                    downloadUrl = asset.GetProperty("browser_download_url").GetString();
                }
            }

            return (tag.TrimStart('v'), downloadUrl);
        }
        catch
        {
            return (null, null);
        }
    }

    public static async Task<bool> DownloadAndInstallAsync(string downloadUrl)
    {
        try
        {
            var tmpDir = Path.Combine(Path.GetTempPath(), "scaffolder-update-" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(tmpDir);

            var archivePath = Path.Combine(tmpDir, "update.tar.gz");

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Scaffolder");

            ConsoleService.Info("Telechargement...");
            var data = await client.GetByteArrayAsync(downloadUrl);
            await File.WriteAllBytesAsync(archivePath, data);
            ConsoleService.Success($"Telecharge ({data.Length / 1024} KB)");

            var extractDir = Path.Combine(tmpDir, "extracted");
            Directory.CreateDirectory(extractDir);

            ConsoleService.Info("Extraction...");
            var process = new ProcessStartInfo
            {
                FileName = "tar",
                Arguments = $"-xzf \"{archivePath}\" -C \"{extractDir}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var tar = new Process { StartInfo = process };
            tar.Start();
            await tar.WaitForExitAsync();

            if (tar.ExitCode != 0)
            {
                ConsoleService.Error("Echec de l'extraction de l'archive.");
                return false;
            }

            var binaryName = OperatingSystem.IsWindows() ? "scaffold.exe" : "scaffold";
            var extractedBinary = Directory.GetFiles(extractDir, binaryName, SearchOption.AllDirectories)
                .FirstOrDefault();

            if (extractedBinary == null)
            {
                ConsoleService.Error("Binaire introuvable dans l'archive.");
                return false;
            }

            // Determine the current binary path
            var currentPath = Environment.ProcessPath
                ?? Path.Combine(AppContext.BaseDirectory, binaryName);

            if (!File.Exists(currentPath))
            {
                // Try to find in PATH
                var whichResult = await ProcessService.RunAsync(
                    OperatingSystem.IsWindows() ? "where" : "which", "scaffold",
                    streamOutput: false).ConfigureAwait(false);
                var pathInPath = whichResult.Output.Trim();
                if (!string.IsNullOrEmpty(pathInPath) && File.Exists(pathInPath))
                    currentPath = pathInPath;
                else
                {
                    ConsoleService.Error("Impossible de trouver le binaire Scaffolder.");
                    return false;
                }
            }

            ConsoleService.Info($"Installation vers : {currentPath}");

            // On Linux/macOS, check if we need sudo
            var needSudo = false;
            try
            {
                var testWrite = File.OpenWrite(currentPath);
                testWrite.Close();
            }
            catch (UnauthorizedAccessException)
            {
                needSudo = true;
            }

            if (needSudo)
            {
                ConsoleService.Warning("Permission requise pour installer la mise a jour.");
                ConsoleService.Info("Utilisation de sudo...");

                var sudoResult = await ProcessService.RunAsync(
                    "sudo", $"cp \"{extractedBinary}\" \"{currentPath}\" && sudo chmod +x \"{currentPath}\"",
                    streamOutput: true).ConfigureAwait(false);

                if (sudoResult.ExitCode != 0)
                {
                    ConsoleService.Error("Echec de l'installation avec sudo.");
                    ConsoleService.Info("Installation manuelle :");
                    ConsoleService.Info($"  sudo cp {extractedBinary} {currentPath}");
                    return false;
                }
            }
            else
            {
                // Rename old binary as backup, copy new one
                var backupPath = currentPath + ".bak";
                if (File.Exists(backupPath)) File.Delete(backupPath);
                File.Move(currentPath, backupPath);

                try
                {
                    File.Copy(extractedBinary, currentPath, overwrite: true);
                    if (!OperatingSystem.IsWindows())
                    {
                        var chmodResult = await ProcessService.RunAsync(
                            "chmod", $"+x \"{currentPath}\"",
                            streamOutput: false).ConfigureAwait(false);
                    }
                    ConsoleService.Success("Mise a jour installee !");
                    // Clean up backup
                    if (File.Exists(backupPath)) File.Delete(backupPath);
                }
                catch (Exception ex)
                {
                    // Restore backup
                    if (File.Exists(backupPath))
                        File.Move(backupPath, currentPath);
                    ConsoleService.Error($"Echec de l'installation : {ex.Message}");
                    return false;
                }
            }

            // Cleanup temp
            try { Directory.Delete(tmpDir, recursive: true); } catch { }

            return true;
        }
        catch (Exception ex)
        {
            ConsoleService.Error($"Echec de la mise a jour : {ex.Message}");
            return false;
        }
    }
}

internal static class RuntimeInfo
{
    public static string OS => OperatingSystem.IsWindows() ? "windows" :
                               OperatingSystem.IsMacOS() ? "macos" : "linux";

    public static string Arch =>
        System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture switch
        {
            System.Runtime.InteropServices.Architecture.Arm64 => "arm64",
            _ => "x64"
        };

    public static string RID
    {
        get
        {
            if (OperatingSystem.IsWindows())
                return Arch == "arm64" ? "win-arm64" : "win-x64";
            if (OperatingSystem.IsMacOS())
                return Arch == "arm64" ? "osx-arm64" : "osx-x64";
            // Linux - detect musl
            try
            {
                var ldd = File.ReadAllText("/lib/x86_64-linux-gnu/libc.so.6");
                return "linux-x64";
            }
            catch
            {
                try
                {
                    var osRelease = File.ReadAllText("/etc/os-release");
                    return osRelease.Contains("alpine", StringComparison.OrdinalIgnoreCase)
                        ? "linux-musl-x64" : "linux-x64";
                }
                catch { return "linux-x64"; }
            }
        }
    }
}
