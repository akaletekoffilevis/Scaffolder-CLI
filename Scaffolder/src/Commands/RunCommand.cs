using System.CommandLine;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class RunCommand : Command
{
    public RunCommand() : base("run", "Lance le projet (npm run dev, dotnet run, cargo run...)")
    {
        SetAction(HandleRun);
    }

    private static int HandleRun(ParseResult pr)
    {
        var cwd = Directory.GetCurrentDirectory();
        var cmd = DetectRunCommand(cwd);

        if (cmd == null)
        {
            ConsoleService.Error("Impossible de detecter la commande a lancer.");
            ConsoleService.Info("Types detectes : package.json, *.csproj, Cargo.toml, go.mod");
            return 1;
        }

        ConsoleService.Info($"  {cmd}");
        Console.WriteLine();

        var parts = cmd.Split(' ', 2);
        var result = ProcessService.RunAsync(parts[0], parts.Length > 1 ? parts[1] : "",
            workingDirectory: cwd, streamOutput: true).GetAwaiter().GetResult();

        return result.ExitCode;
    }

    private static string? DetectRunCommand(string dir)
    {
        if (File.Exists(Path.Combine(dir, "package.json")))
        {
            var pkg = File.ReadAllText(Path.Combine(dir, "package.json"));
            if (pkg.Contains("\"dev\"")) return "npm run dev";
            if (pkg.Contains("\"start\"")) return "npm run start";
            return "npm run dev";
        }

        if (Directory.GetFiles(dir, "*.csproj").Length > 0)
            return "dotnet run";

        if (File.Exists(Path.Combine(dir, "Cargo.toml")))
            return "cargo run";

        if (File.Exists(Path.Combine(dir, "go.mod")))
            return "go run .";

        return null;
    }
}
