using System.Text;

namespace Scaffolder.Services;

public static class GitService
{
    public static bool IsGitAvailable() => ProcessService.CommandExists("git");

    public static async Task<bool> InitAsync(string directory)
    {
        if (!IsGitAvailable())
        {
            ConsoleService.Warning("Git n'est pas installe.");
            return false;
        }

        // -b main : evite un avertissement et garantit un nom de branche moderne.
        var result = await ProcessService.RunResultAsync(
            "git", ["init", "-b", "main"], workingDirectory: directory);

        if (result.Success) return true;

        // Anciennes versions de Git sans l'option -b.
        result = await ProcessService.RunResultAsync("git", ["init"], workingDirectory: directory);
        return result.Success;
    }

    public static async Task<bool> AddAndCommitAsync(
        string directory, string message = "Initial commit with Scaffolder")
    {
        if (!IsGitAvailable()) return false;

        var add = await ProcessService.RunResultAsync("git", ["add", "-A"], workingDirectory: directory);
        if (!add.Success)
        {
            ConsoleService.Debug($"git add a echoue : {add.Output}");
            return false;
        }

        var commit = await ProcessService.RunResultAsync(
            "git", ["commit", "-m", message, "--allow-empty"], workingDirectory: directory);

        if (commit.Success) return true;

        // Identite Git absente : on retente avec une identite locale explicite.
        if (commit.Output.Contains("Please tell me who you are", StringComparison.OrdinalIgnoreCase) ||
            commit.Output.Contains("unable to auto-detect email", StringComparison.OrdinalIgnoreCase))
        {
            await ProcessService.RunResultAsync(
                "git", ["config", "user.name", "Scaffolder"], workingDirectory: directory);
            await ProcessService.RunResultAsync(
                "git", ["config", "user.email", "scaffolder@localhost"], workingDirectory: directory);

            commit = await ProcessService.RunResultAsync(
                "git", ["commit", "-m", message, "--allow-empty"], workingDirectory: directory);

            if (commit.Success)
            {
                ConsoleService.Debug("Identite Git locale configuree automatiquement.");
                return true;
            }
        }

        ConsoleService.Debug($"git commit a echoue : {commit.Output}");
        return false;
    }

    public static async Task<bool> InitWithCommitAsync(
        string directory, string message = "Initial commit with Scaffolder")
    {
        if (!await InitAsync(directory)) return false;

        if (await AddAndCommitAsync(directory, message))
        {
            ConsoleService.Success("Depot Git initialise avec le premier commit.");
            return true;
        }

        ConsoleService.Warning("Git initialise, mais le commit a echoue (verifie ta configuration Git).");
        return false;
    }

    public static async Task<string?> GetRemoteUrlAsync(string directory)
    {
        var result = await ProcessService.RunResultAsync(
            "git", ["remote", "get-url", "origin"], workingDirectory: directory);
        return result.Success ? result.StandardOutput.Trim() : null;
    }

    public static async Task<bool> HasUncommittedChangesAsync(string directory)
    {
        var result = await ProcessService.RunResultAsync(
            "git", ["status", "--porcelain"], workingDirectory: directory);
        return result.Success && !string.IsNullOrWhiteSpace(result.StandardOutput);
    }

    public static async Task<string?> GetCurrentBranchAsync(string directory)
    {
        var result = await ProcessService.RunResultAsync(
            "git", ["rev-parse", "--abbrev-ref", "HEAD"], workingDirectory: directory);
        return result.Success ? result.StandardOutput.Trim() : null;
    }

    /// <summary>Clone un depot dans un dossier. L'URL est passee comme argument (aucune injection possible).</summary>
    public static async Task<(bool Ok, string Message)> CloneAsync(
        string url, string destination, int depth = 1)
    {
        if (!IsGitAvailable())
            return (false, "Git n'est pas installe.");

        if (string.IsNullOrWhiteSpace(url))
            return (false, "URL Git invalide.");

        var args = new List<string> { "clone" };
        if (depth > 0)
        {
            args.Add("--depth");
            args.Add(depth.ToString());
        }
        args.Add("--");
        args.Add(url);
        args.Add(destination);

        var result = await ProcessService.RunResultAsync("git", args, streamOutput: true);

        return result.Success
            ? (true, $"Depot clone dans {destination}")
            : (false, result.Output);
    }

    public static string? GetShortStatus(string directory)
    {
        var result = ProcessService.RunResultAsync("git", ["status", "--short", "--branch"], workingDirectory: directory)
            .GetAwaiter().GetResult();

        return result.Success ? result.StandardOutput.Trim() : null;
    }

    public static void InitWithSpinner(string directory, string message = "Initialisation du depot Git...")
    {
        ConsoleService.ShowSpinner(
            $"  {message}",
            async () => { await InitWithCommitAsync(directory); })
            .GetAwaiter().GetResult();
    }

    /// <summary>Genere un .gitignore adapte au langage detecte (si absent).</summary>
    public static void EnsureGitignore(string directory)
    {
        var path = Path.Combine(directory, ".gitignore");
        if (File.Exists(path)) return;

        var language = LanguageDetector.Detect(directory);
        var content = language switch
        {
            "dotnet" => "bin/\nobj/\n.vs/\n*.user\nTestResults/\n",
            "node" or "deno" => "node_modules/\ndist/\nbuild/\n.env\n.env.local\ncoverage/\n",
            "rust" => "target/\nCargo.lock\n",
            "go" => "*.exe\n*.test\n*.out\nvendor/\n",
            "python" => "__pycache__/\n*.pyc\n.venv/\nvenv/\n.env\ndist/\nbuild/\n",
            "ruby" => ".bundle/\nvendor/bundle/\n*.gem\n",
            "php" => "vendor/\n.env\n",
            "dart" => ".dart_tool/\nbuild/\n.pub-cache/\n",
            "jvm" or "scala" => "build/\ntarget/\n.gradle/\n*.class\n",
            "swift" => ".build/\nPackage.resolved\n",
            "zig" => "zig-out/\n.zig-cache/\n",
            "elixir" => "_build/\ndeps/\n*.beam\n",
            "crystal" => "lib/\nbin/\n",
            "haskell" => "dist-newstyle/\n.stack-work/\n",
            _ => ""
        };

        var builder = new StringBuilder(content);
        builder.AppendLine("# OS");
        builder.AppendLine(".DS_Store");
        builder.AppendLine("Thumbs.db");
        builder.AppendLine();

        try
        {
            File.WriteAllText(path, builder.ToString());
            ConsoleService.Debug(".gitignore genere.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ConsoleService.Debug($"Impossible d'ecrire .gitignore : {ex.Message}");
        }
    }
}
