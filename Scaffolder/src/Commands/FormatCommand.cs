using System.CommandLine;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class FormatCommand : Command
{
    public FormatCommand() : base("format", "Formate le code du projet")
    {
        SetAction(HandleFormat);
    }

    private static int HandleFormat(ParseResult pr)
    {
        var cwd = Directory.GetCurrentDirectory();
        var cmd = DetectFormatCommand(cwd);

        if (cmd == null)
        {
            ConsoleService.Warning("Aucun formateur detecte.");
            return 1;
        }

        ConsoleService.Info($"  {cmd}");
        Console.WriteLine();

        var parts = cmd.Split(' ', 2);
        var result = ProcessService.RunAsync(parts[0], parts.Length > 1 ? parts[1] : "",
            workingDirectory: cwd, streamOutput: true).GetAwaiter().GetResult();
        return result.ExitCode;
    }

    private static string? DetectFormatCommand(string dir)
    {
        if (File.Exists(Path.Combine(dir, "package.json")))
        {
            var pkg = File.ReadAllText(Path.Combine(dir, "package.json"));
            if (pkg.Contains("\"format\"")) return "npm run format";
        }

        if (Directory.GetFiles(dir, "*.csproj").Length > 0)
            return "dotnet format";

        if (File.Exists(Path.Combine(dir, "Cargo.toml")))
            return "cargo fmt";

        return null;
    }
}
