using System.CommandLine;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class BuildCommand : Command
{
    public BuildCommand() : base("build", "Compile le projet")
    {
        SetAction(HandleBuild);
    }

    private static int HandleBuild(ParseResult pr)
    {
        var cwd = Directory.GetCurrentDirectory();
        var cmd = DetectCommand(cwd);

        if (cmd == null)
        {
            ConsoleService.Error("Impossible de detecter le type de projet.");
            return 1;
        }

        ConsoleService.Info($"  {cmd}");
        Console.WriteLine();

        var parts = cmd.Split(' ', 2);
        var result = ProcessService.RunAsync(parts[0], parts.Length > 1 ? parts[1] : "",
            workingDirectory: cwd, streamOutput: true).GetAwaiter().GetResult();
        return result.ExitCode;
    }

    private static string? DetectCommand(string dir)
    {
        if (File.Exists(Path.Combine(dir, "package.json")))
        {
            var pkg = File.ReadAllText(Path.Combine(dir, "package.json"));
            if (pkg.Contains("\"build\"")) return "npm run build";
            return "npm run build";
        }

        if (Directory.GetFiles(dir, "*.csproj").Length > 0)
            return "dotnet build";

        if (File.Exists(Path.Combine(dir, "Cargo.toml")))
            return "cargo build";

        return null;
    }
}
