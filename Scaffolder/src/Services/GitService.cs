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

        var result = await ProcessService.RunAsync("git", "init", directory, streamOutput: false);
        return result.ExitCode == 0;
    }

    public static async Task<bool> AddAndCommitAsync(string directory, string message = "Initial commit with Scaffolder")
    {
        if (!IsGitAvailable()) return false;

        await ProcessService.RunAsync("git", "add .", directory, streamOutput: false);
        var result = await ProcessService.RunAsync("git", $"commit -m \"{message}\" --allow-empty", directory, streamOutput: false);
        return result.ExitCode == 0;
    }

    public static async Task<bool> InitWithCommitAsync(string directory, string message = "Initial commit with Scaffolder")
    {
        var inited = await InitAsync(directory);
        if (!inited) return false;

        var committed = await AddAndCommitAsync(directory, message);
        if (committed)
            ConsoleService.Success("Depot Git initialise avec le premier commit.");
        else
            ConsoleService.Warning("Git commit a echoue. Verifie ta config Git (user.name, user.email).");

        return committed;
    }

    public static async Task<string?> GetRemoteUrlAsync(string directory)
    {
        var result = await ProcessService.RunAsync("git", "remote get-url origin", directory, streamOutput: false);
        return result.ExitCode == 0 ? result.Output.Trim() : null;
    }

    public static async Task<bool> HasUncommittedChangesAsync(string directory)
    {
        var result = await ProcessService.RunAsync("git", "status --porcelain", directory, streamOutput: false);
        return result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.Output);
    }

    public static void InitWithSpinner(string directory, string message = "Initialisation du depot Git...")
    {
        ConsoleService.ShowSpinner(
            $"  {message}",
            async () =>
            {
                var success = await InitWithCommitAsync(directory);
                if (!success && IsGitAvailable())
                    ConsoleService.Warning("Git initialise sans commit.");
            }).GetAwaiter().GetResult();
    }
}
