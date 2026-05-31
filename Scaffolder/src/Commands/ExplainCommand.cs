using System.CommandLine;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class ExplainCommand : Command
{
    public ExplainCommand() : base("explain", "Explique un concept de developpement")
    {
        var conceptArg = new Argument<string>("concept")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Concept a expliquer (ex: middleware, mvc, docker)"
        };
        Add(conceptArg);
        this.SetAction(async (ParseResult pr) => await HandleExplain(pr.GetValue(conceptArg)));
    }

    private static async Task<int> HandleExplain(string? concept)
    {
        if (string.IsNullOrWhiteSpace(concept))
        {
            ConsoleService.Info("Concepts disponibles :");
            foreach (var c in KnowledgeBase.ExplainAllConcepts())
                ConsoleService.Info($"  - {c}");
            ConsoleService.Info("");
            ConsoleService.Info("Exemple : scaffold explain middleware");
            return 0;
        }

        ConsoleService.Info("Recherche...");
        var (title, content) = await AIService.ExplainAsync(concept);

        if (title == null)
        {
            ConsoleService.Warning($"Concept '{concept}' introuvable.");
            ConsoleService.Info("Concepts disponibles :");
            foreach (var c in KnowledgeBase.ExplainAllConcepts())
                ConsoleService.Info($"  - {c}");
            return 1;
        }

        ConsoleService.Info($"{title}");
        Console.WriteLine();
        Console.WriteLine(content);
        return 0;
    }
}
