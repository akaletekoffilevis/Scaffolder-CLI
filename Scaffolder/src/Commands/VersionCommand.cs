using System.CommandLine;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class VersionCommand : Command
{
    public VersionCommand() : base("version", "Affiche la version de Scaffolder")
    {
        SetAction(HandleVersion);
    }

    private static int HandleVersion(ParseResult pr)
    {
        var version = UpdateService.CurrentVersion;
        Console.WriteLine($"Scaffolder v{version}");
        return 0;
    }
}
