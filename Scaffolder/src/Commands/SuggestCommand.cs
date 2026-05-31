using System.CommandLine;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class SuggestCommand : Command
{
    public SuggestCommand() : base("suggest", "Suggere un template a partir de mots-cles")
    {
        var keywordsArg = new Argument<string[]>("keywords")
        {
            Arity = ArgumentArity.OneOrMore,
            Description = "Mots-cles decrivant le projet (ex: api react mobile)"
        };
        Add(keywordsArg);
        this.SetAction(async (ParseResult pr) => await HandleSuggest(pr.GetValue(keywordsArg)));
    }

    private static async Task<int> HandleSuggest(string[]? keywords)
    {
        if (keywords == null || keywords.Length == 0)
        {
            ConsoleService.Error("Usage : scaffold suggest <mots-cles>");
            ConsoleService.Info("Exemple : scaffold suggest api rest");
            ConsoleService.Info("Exemple : scaffold suggest mobile flutter");
            return 1;
        }

        ConsoleService.Info("Analyse des mots-cles...");
        var template = await AIService.SuggestAsync(keywords);
        ConsoleService.Success($"Template suggere : {template}");
        ConsoleService.Info("  Pour generer :");
        ConsoleService.Info($"  scaffold new --template={template.Split(' ')[0]} --name=mon-projet");
        return 0;
    }
}
