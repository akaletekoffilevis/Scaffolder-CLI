using System.CommandLine;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class GenerateCommand : Command
{
    public GenerateCommand() : base("generate", "Cree un template personnalise via IA")
    {
        var descArg = new Argument<string[]>("description")
        {
            Arity = ArgumentArity.OneOrMore,
            Description = "Description du template en langage naturel (ex: api rest avec mongodb)"
        };
        var nameOpt = new Option<string>("--name") { Description = "Nom du projet" };
        var dryRunOpt = new Option<bool>("--dry-run") { Description = "Affiche sans creer" };

        Add(descArg);
        Add(nameOpt);
        Add(dryRunOpt);

        this.SetAction(async (ParseResult pr) => await HandleGenerate(
            pr.GetValue(descArg), pr.GetValue(nameOpt), pr.GetValue(dryRunOpt)));
    }

    private static async Task<int> HandleGenerate(string[]? description, string? name, bool dryRun)
    {
        if (description == null || description.Length == 0)
        {
            ConsoleService.Error("Usage : scaffold generate <description>");
            ConsoleService.Info("Exemple : scaffold generate api rest avec postgres et authentification jwt");
            ConsoleService.Info("Exemple : scaffold generate blog next.js avec tailwind");
            return 1;
        }

        var desc = string.Join(" ", description);
        name ??= "mon-projet";

        if (!AIService.HasApiKey)
        {
            ConsoleService.Warning("Aucune cle API configuree. Mode hors-ligne : suggestion de template.");
            ConsoleService.Info("Configure avec :");
            ConsoleService.Info("  scaffold config set apiKey <votre-cle>");
            ConsoleService.Info("  scaffold config set provider grok|openai|claude|gemini");
            Console.WriteLine();

            var suggestion = KnowledgeBase.Suggest(description);
            ConsoleService.Info($"Template suggere : {suggestion}");
            ConsoleService.Info($"  scaffold new --template={suggestion.Split(' ')[0]} --name={name}");
            return 0;
        }

        ConsoleService.Info("Generation du template via IA...");

        var prompt = $"""
Tu es un expert en generation de projets. L'utilisateur veut : {desc}

Genere un plan de projet detaille avec :
1. TECHNOLOGIES : les langages, frameworks et bibliotheques a utiliser
2. STRUCTURE : l'arborescence des fichiers
3. COMMANDES : les commandes pour initialiser (scaffold new --template=X, npm create, dotnet new, etc.)
4. FICHIERS : les fichiers essentiels a creer avec leur contenu

Reponds en francais, soit precis, donne des exemples concrets.
Format :
---
Technologies : ...
Structure : ...
Commandes : ...
Fichiers essentiels :
- README.md : contenu...
- ...
---
""";

        var result = await AIService.AskAsync(prompt, maxTokens: 1000);

        if (result == null)
        {
            ConsoleService.Warning("Erreur IA. Utilisation du mode regles...");
            var suggestion = KnowledgeBase.Suggest(description);
            ConsoleService.Info($"Template suggere : {suggestion}");
            ConsoleService.Info($"  scaffold new --template={suggestion.Split(' ')[0]} --name={name}");
            return 0;
        }

        Console.WriteLine();
        Console.WriteLine(result);
        Console.WriteLine();

        if (dryRun)
        {
            ConsoleService.Info("Mode --dry-run : aucun fichier cree.");
            return 0;
        }

        // Extract suggested template from IA response
        var lines = result.Split('\n');
        var templateMatch = lines.FirstOrDefault(l =>
            l.Contains("scaffold new") || l.Contains("--template="));

        if (templateMatch != null)
        {
            ConsoleService.Info("Pour generer ce projet :");
            ConsoleService.Info($"  {templateMatch.Trim()}");
            ConsoleService.Info($"  --ou--");
            ConsoleService.Info($"  echo \"{templateMatch.Trim()}\" | sh");
        }

        ConsoleService.Success("Plan de projet genere !");
        return 0;
    }
}
