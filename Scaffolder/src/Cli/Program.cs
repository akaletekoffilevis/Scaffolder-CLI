using System.CommandLine;
using System.Linq;
using Scaffolder.Cli;
using Scaffolder.Commands;
using Scaffolder.Services;

// Les options globales (--yes, --no-color, --verbose, --quiet) sont extraites avant
// l'analyse par System.CommandLine : elles fonctionnent donc sur toutes les commandes.
var scaffoldArgs = AppOptions.Parse(args);

try
{
    DatabaseService.Initialize();
}
catch (Exception ex)
{
    ConsoleService.Warning($"Impossible initialiser la base locale : {ex.Message}");
}

var rootCommand = RootFactory.Create();

rootCommand.Aliases.Add("scaff");
rootCommand.Aliases.Add("s");

var builtinVer = rootCommand.Options.OfType<VersionOption>().FirstOrDefault();
if (builtinVer != null) rootCommand.Options.Remove(builtinVer);
var versionOpt = new Option<bool>("--version", ["-v"]) { Description = "Affiche la version" };
rootCommand.Options.Add(versionOpt);

rootCommand.SetAction((ParseResult pr) =>
{
    ConsoleService.CheckFirstRun();

    if (pr.GetValue(versionOpt))
    {
        Console.WriteLine($"Scaffolder v{UpdateService.CurrentVersion}");
        return 0;
    }

    if (!Console.IsInputRedirected && !Console.IsOutputRedirected)
    {
        Scaffolder.Tui.ScaffolderTui.Run();
        return 0;
    }

    ConsoleService.ShowLogo();
    Console.WriteLine();
    ConsoleService.Info("Utilise `scaffold --help` pour voir toutes les commandes.");
    ConsoleService.Info("Utilise `scaffold new` pour créer ton premier projet.");
    return 0;
});

try
{
    var config = new InvocationConfiguration();
    return await rootCommand.Parse(scaffoldArgs).InvokeAsync(config);
}
catch (OperationCanceledException)
{
    ConsoleService.Warning("Operation annulee.");
    return 130;
}
catch (Exception ex)
{
    ConsoleService.Error($"Erreur inattendue : {ex.Message}");

    if (AppOptions.Verbose)
    {
        Console.WriteLine();
        Console.WriteLine(ex);
    }
    else
    {
        ConsoleService.Info("Relance avec --verbose pour plus de details.");
    }

    return 1;
}
