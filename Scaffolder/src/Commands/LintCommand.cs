using System.CommandLine;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class LintCommand : Command
{
    public LintCommand() : base("lint", "Execute le linter du projet")
    {
        SetAction(HandleLint);
    }

    private static int HandleLint(ParseResult pr)
    {
        var cwd = Directory.GetCurrentDirectory();
        var cmd = DetectLintCommand(cwd);

        if (cmd == null)
        {
            ConsoleService.Warning("Aucun linter detecte. Essaie : eslint, ruff, dotnet format");
            return 1;
        }

        ConsoleService.Info($"  {cmd}");
        Console.WriteLine();

        var parts = cmd.Split(' ', 2);
        var result = ProcessService.RunAsync(parts[0], parts.Length > 1 ? parts[1] : "",
            workingDirectory: cwd, streamOutput: true).GetAwaiter().GetResult();
        return result.ExitCode;
    }

    private static string? DetectLintCommand(string dir)
    {
        if (File.Exists(Path.Combine(dir, "package.json")))
        {
            var pkg = File.ReadAllText(Path.Combine(dir, "package.json"));
            if (pkg.Contains("\"lint\"")) return "npm run lint";
        }

        if (Directory.GetFiles(dir, "*.csproj").Length > 0)
            return "dotnet format --verify-no-changes";

        if (File.Exists(Path.Combine(dir, "Cargo.toml")))
            return "cargo clippy";

        return null;
    }
}
