using System.CommandLine;
using Scaffolder.Services;
using Scaffolder.Tui;

namespace Scaffolder.Commands;

/// <summary>Ouvre l'interface terminal plein écran (Terminal.Gui).</summary>
public class TuiCommand : Command
{
    public TuiCommand() : base("tui", "Ouvre l'interface terminal interactive")
    {
        SetAction(_ => Run());
    }

    private static int Run()
    {
        if (Console.IsInputRedirected || Console.IsOutputRedirected)
        {
            ConsoleService.Error("L'interface terminal demande un terminal interactif.");
            ConsoleService.Info("Utilise plutôt : scaffold new --template <nom> --name <nom> --silent");
            return 2;
        }

        try
        {
            ScaffolderTui.Run();
            return 0;
        }
        catch (Exception ex)
        {
            ConsoleService.Error($"L'interface terminal n'a pas pu démarrer : {ex.Message}");
            ConsoleService.Info("Ton terminal doit supporter les couleurs et les caractères Unicode.");
            if (AppOptions.Verbose) Console.WriteLine(ex);
            ConsoleService.Info("Relance avec --verbose pour le détail, ou utilise : scaffold ui");
            return 1;
        }
    }
}
