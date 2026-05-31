using System.CommandLine;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class FixCommand : Command
{
    public FixCommand() : base("fix", "Suggere une solution pour une erreur")
    {
        var errorArg = new Argument<string[]>("error")
        {
            Arity = ArgumentArity.OneOrMore,
            Description = "Message d'erreur ou code (ex: CS1061 port already in use)"
        };
        Add(errorArg);
        this.SetAction(async (ParseResult pr) => await HandleFix(pr.GetValue(errorArg)));
    }

    private static async Task<int> HandleFix(string[]? error)
    {
        if (error == null || error.Length == 0)
        {
            ConsoleService.Error("Usage : scaffold fix <message d'erreur>");
            ConsoleService.Info("Exemple : scaffold fix CS1061");
            ConsoleService.Info("Exemple : scaffold fix port already in use");
            return 1;
        }

        var errorText = string.Join(" ", error);
        ConsoleService.Info("Analyse de l'erreur...");
        var (title, fix) = await AIService.FixAsync(errorText);

        if (title == null)
        {
            ConsoleService.Warning("Aucune solution connue pour cette erreur.");
            ConsoleService.Info("Essaie de chercher sur :");
            ConsoleService.Info("  - https://stackoverflow.com/search?q=" + Uri.EscapeDataString(errorText));
            ConsoleService.Info("  - https://github.com/search?q=" + Uri.EscapeDataString(errorText));
            return 1;
        }

        ConsoleService.Info($"{title}");
        Console.WriteLine();
        Console.WriteLine(fix);
        return 0;
    }
}
