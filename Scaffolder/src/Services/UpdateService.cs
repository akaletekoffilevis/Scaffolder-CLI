using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace Scaffolder.Services;

public sealed record UpdateInfo(string Version, string? DownloadUrl, string? ChecksumUrl);

public static class UpdateService
{
    private const string RepoOwner = "akaletekoffilevis";
    private const string RepoName = "Scaffolder-CLI";
    private const string GitHubApi = "https://api.github.com/repos";

    private static readonly HttpClient Client = CreateClient();

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        client.DefaultRequestHeaders.Add("User-Agent", "Scaffolder");
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        return client;
    }

    public static string CurrentVersion
    {
        get
        {
            try
            {
                var v = Assembly.GetExecutingAssembly()
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

                if (!string.IsNullOrWhiteSpace(v))
                {
                    // Retire un eventuel suffixe de commit ajoute par le SDK.
                    var plus = v.IndexOf('+');
                    return plus > 0 ? v[..plus] : v;
                }

                return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "2.4.0";
            }
            catch
            {
                return "2.4.0";
            }
        }
    }

    public static async Task<(string? LatestVersion, string? DownloadUrl)> CheckForUpdateAsync()
    {
        var info = await CheckForUpdateDetailedAsync();
        return (info?.Version, info?.DownloadUrl);
    }

    /// <summary>Interroge GitHub et retourne la derniere version, le binaire correspondant et un checksum si publie.</summary>
    public static async Task<UpdateInfo?> CheckForUpdateDetailedAsync()
    {
        try
        {
            var url = $"{GitHubApi}/{RepoOwner}/{RepoName}/releases/latest";
            using var response = await Client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (!doc.RootElement.TryGetProperty("tag_name", out var tagElement))
                return null;

            var tag = tagElement.GetString() ?? "";
            if (string.IsNullOrWhiteSpace(tag))
                return null;

            var version = tag.TrimStart('v', 'V');
            var rid = RuntimeInfo.RID;
            var arch = RuntimeInfo.Arch;
            var os = RuntimeInfo.OS;

            string? downloadUrl = null;
            string? checksumUrl = null;

            if (doc.RootElement.TryGetProperty("assets", out var assets) &&
                assets.ValueKind == JsonValueKind.Array)
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    var name = asset.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    var assetUrl = asset.TryGetProperty("browser_download_url", out var u) ? u.GetString() : null;

                    if (string.IsNullOrEmpty(assetUrl)) continue;

                    if (IsChecksumAsset(name))
                    {
                        checksumUrl = assetUrl;
                        continue;
                    }

                    if (!name.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (name.Contains(rid, StringComparison.OrdinalIgnoreCase))
                    {
                        // Meilleure correspondance possible : on s'arrete.
                        downloadUrl = assetUrl;
                        break;
                    }

                    if (downloadUrl == null &&
                        name.Contains(arch, StringComparison.OrdinalIgnoreCase) &&
                        name.Contains(os, StringComparison.OrdinalIgnoreCase))
                    {
                        downloadUrl = assetUrl;
                    }
                }
            }

            return new UpdateInfo(version, downloadUrl, checksumUrl);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            ConsoleService.Debug($"Verification de mise a jour impossible : {ex.Message}");
            return null;
        }
    }

    private static bool IsChecksumAsset(string name) =>
        name.Equals("checksums.txt", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("checksums.sha256", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("SHA256SUMS", StringComparison.OrdinalIgnoreCase) ||
        name.EndsWith(".sha256", StringComparison.OrdinalIgnoreCase);

    public static async Task<bool> DownloadAndInstallAsync(string downloadUrl, string? checksumUrl = null)
    {
        if (string.IsNullOrWhiteSpace(downloadUrl))
        {
            ConsoleService.Error("URL de telechargement invalide.");
            return false;
        }

        var tmpDir = Path.Combine(Path.GetTempPath(), "scaffolder-update-" + Guid.NewGuid().ToString("N")[..8]);

        try
        {
            Directory.CreateDirectory(tmpDir);
            var archivePath = Path.Combine(tmpDir, "update.tar.gz");

            ConsoleService.Info("Telechargement...");
            var data = await Client.GetByteArrayAsync(downloadUrl);
            await File.WriteAllBytesAsync(archivePath, data);
            ConsoleService.Success($"Telecharge ({data.Length / 1024} KB)");

            if (!await VerifyChecksumAsync(archivePath, downloadUrl, checksumUrl))
                return false;

            var extractDir = Path.Combine(tmpDir, "extracted");
            Directory.CreateDirectory(extractDir);

            ConsoleService.Info("Extraction...");
            if (!await SafelyExtractAsync(archivePath, extractDir))
                return false;

            var binaryName = OperatingSystem.IsWindows() ? "scaffold.exe" : "scaffold";
            var extractedBinary = Directory.GetFiles(extractDir, binaryName, SearchOption.AllDirectories)
                .FirstOrDefault();

            if (extractedBinary == null)
            {
                ConsoleService.Error("Binaire introuvable dans l'archive.");
                return false;
            }

            // Defense en profondeur : le binaire doit rester dans le dossier d'extraction.
            var resolved = Path.GetFullPath(extractedBinary);
            if (!resolved.StartsWith(Path.GetFullPath(extractDir), StringComparison.Ordinal))
            {
                ConsoleService.Error("Archive suspecte : chemin de binaire hors du dossier d'extraction.");
                return false;
            }

            var currentPath = ResolveCurrentBinaryPath(binaryName);
            if (currentPath == null)
            {
                ConsoleService.Error("Impossible de trouver le binaire Scaffolder.");
                return false;
            }

            ConsoleService.Info($"Installation vers : {currentPath}");
            return await InstallBinaryAsync(extractedBinary, currentPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or HttpRequestException or TaskCanceledException)
        {
            ConsoleService.Error($"Echec de la mise a jour : {ex.Message}");
            return false;
        }
        finally
        {
            try { Directory.Delete(tmpDir, recursive: true); } catch { /* ignore */ }
        }
    }

    private static string? ResolveCurrentBinaryPath(string binaryName)
    {
        var path = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
            return path;

        var found = ProcessService.FindCommand(binaryName);
        if (found != null) return found;

        var fallback = Path.Combine(AppContext.BaseDirectory, binaryName);
        return File.Exists(fallback) ? fallback : null;
    }

    private static async Task<bool> VerifyChecksumAsync(
        string archivePath, string downloadUrl, string? checksumUrl)
    {
        try
        {
            var candidates = new List<string>();
            if (!string.IsNullOrWhiteSpace(checksumUrl)) candidates.Add(checksumUrl);
            candidates.Add(downloadUrl + ".sha256");
            candidates.Add(downloadUrl + ".sha256sum");

            var archiveName = Path.GetFileName(new Uri(downloadUrl).AbsolutePath);

            foreach (var candidate in candidates)
            {
                string content;
                try
                {
                    using var response = await Client.GetAsync(candidate);
                    if (!response.IsSuccessStatusCode) continue;
                    content = await response.Content.ReadAsStringAsync();
                }
                catch (HttpRequestException)
                {
                    continue;
                }

                var expected = ExtractChecksumFor(content, archiveName);
                if (string.IsNullOrWhiteSpace(expected)) continue;

                await using var stream = File.OpenRead(archivePath);
                var actual = Convert.ToHexString(await SHA256.HashDataAsync(stream));

                if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
                {
                    ConsoleService.Error("Checksum SHA-256 invalide : l'archive a ete rejetee.");
                    return false;
                }

                ConsoleService.Success("Checksum SHA-256 verifie.");
                return true;
            }

            ConsoleService.Debug("Aucun checksum publie pour cette release : verification ignoree.");
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or UriFormatException)
        {
            ConsoleService.Debug($"Verification du checksum impossible : {ex.Message}");
            return true;
        }
    }

    private static string? ExtractChecksumFor(string checksumFile, string archiveName)
    {
        foreach (var rawLine in checksumFile.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length < 64) continue;

            var hash = line[..64];
            if (!hash.All(Uri.IsHexDigit)) continue;

            if (line.Length == 64)
                return hash; // fichier ne contenant qu'un hash

            if (line.Contains(archiveName, StringComparison.OrdinalIgnoreCase))
                return hash;
        }

        return null;
    }

    private static async Task<bool> SafelyExtractAsync(string archivePath, string extractDir)
    {
        // 1) Liste les entrées et rejette tout chemin absolu ou remontant (zip-slip).
        var listing = await ProcessService.RunResultAsync(
            "tar", ["-tzf", archivePath], timeout: TimeSpan.FromMinutes(2));

        if (!listing.Success)
        {
            ConsoleService.Error("Archive illisible.");
            return false;
        }

        foreach (var entry in listing.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var relative = entry.Trim();
            if (relative.Length == 0) continue;

            if (relative.StartsWith('/') || relative.StartsWith('\\') ||
                relative.Contains("..", StringComparison.Ordinal))
            {
                ConsoleService.Error($"Archive refusee : entree suspecte '{relative}'.");
                return false;
            }

            var target = Path.GetFullPath(Path.Combine(extractDir, relative));
            if (!target.StartsWith(Path.GetFullPath(extractDir), StringComparison.Ordinal))
            {
                ConsoleService.Error($"Archive refusee : entree hors du dossier cible '{relative}'.");
                return false;
            }
        }

        // 2) Extraction avec des arguments separes (aucune interpolation de commande).
        var extraction = await ProcessService.RunResultAsync(
            "tar", ["-xzf", archivePath, "-C", extractDir], timeout: TimeSpan.FromMinutes(5));

        if (!extraction.Success)
        {
            ConsoleService.Error("Echec de l'extraction de l'archive.");
            ConsoleService.Debug(extraction.Output);
            return false;
        }

        return true;
    }

    private static async Task<bool> InstallBinaryAsync(string source, string target)
    {
        var targetDir = Path.GetDirectoryName(target)!;

        if (await CanWriteAsync(targetDir))
        {
            var staging = target + ".new";
            try
            {
                File.Copy(source, staging, overwrite: true);
                if (!OperatingSystem.IsWindows())
                {
                    var chmod = await ProcessService.RunResultAsync("chmod", ["755", staging]);
                    if (!chmod.Success)
                    {
                        ConsoleService.Error("Impossible de rendre le binaire executable.");
                        return false;
                    }
                }

                // Remplacement atomique (le binaire en cours d'execution peut etre remplace sous Linux/macOS).
                File.Move(staging, target, overwrite: true);
                ConsoleService.Success("Mise a jour installee !");
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                ConsoleService.Error($"Echec de l'installation : {ex.Message}");
                return false;
            }
            finally
            {
                try { if (File.Exists(staging)) File.Delete(staging); } catch { /* ignore */ }
            }
        }

        ConsoleService.Warning("Permission requise pour installer la mise a jour (sudo).");

        var copy = await ProcessService.RunResultAsync(
            "sudo", ["cp", source, target], streamOutput: true);

        if (!copy.Success)
        {
            ConsoleService.Error("Echec de l'installation avec sudo.");
            ConsoleService.Info($"Installation manuelle : sudo cp {source} {target}");
            return false;
        }

        var mode = await ProcessService.RunResultAsync("sudo", ["chmod", "755", target]);
        if (!mode.Success)
        {
            ConsoleService.Warning("Le binaire a ete copie mais chmod a echoue.");
        }

        ConsoleService.Success("Mise a jour installee !");
        return true;
    }

    private static async Task<bool> CanWriteAsync(string directory)
    {
        var probe = Path.Combine(directory, ".scaffold-perm-" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            await File.WriteAllTextAsync(probe, string.Empty);
            File.Delete(probe);
            return true;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
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
            System.Runtime.InteropServices.Architecture.X86 => "x86",
            System.Runtime.InteropServices.Architecture.Arm => "arm",
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

            if (Arch == "arm64")
                return "linux-arm64";

            try
            {
                var osRelease = File.ReadAllText("/etc/os-release");
                return osRelease.Contains("alpine", StringComparison.OrdinalIgnoreCase)
                    ? "linux-musl-x64"
                    : "linux-x64";
            }
            catch
            {
                return "linux-x64";
            }
        }
    }
}
