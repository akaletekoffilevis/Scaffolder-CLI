using System.CommandLine;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class TestCommand : Command
{
    public TestCommand() : base("test", "Lance les tests du projet")
    {
        SetAction(HandleTest);
    }

    private static int HandleTest(ParseResult pr)
    {
        var cwd = Directory.GetCurrentDirectory();
        var cmd = DetectCommand(cwd);

        if (cmd == null)
        {
            ConsoleService.Error("Impossible de detecter la commande de test.");
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
            if (pkg.Contains("\"test\"")) return "npm test";
            return "npm test";
        }

        if (Directory.GetFiles(dir, "*.csproj").Length > 0)
            return "dotnet test";

        if (File.Exists(Path.Combine(dir, "Cargo.toml")))
            return "cargo test";

        return null;
    }
}
