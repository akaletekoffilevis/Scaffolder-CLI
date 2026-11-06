using System.CommandLine;
using Scaffolder.Services;

namespace Scaffolder.Commands;

/// <summary>Acces direct a l'assistant IA configure (OpenAI, Claude, Gemini, Grok).</summary>
public class AiCommand : Command
{
    public AiCommand() : base("ai", "Utilise l'assistant IA configure (ou le mode hors ligne)")
    {
        var statusCmd = new Command("status", "Affiche la configuration IA");
        statusCmd.SetAction(_ => HandleStatus());

        var testCmd = new Command("test", "Verifie que la cle API fonctionne");
        testCmd.SetAction(_ => HandleTest());

        var askCmd = new Command("ask", "Pose une question a l'assistant");
        var promptArg = new Argument<string>("prompt") { Description = "Question a poser" };
        var maxTokensOpt = new Option<int>("--max-tokens")
        {
            Description = "Nombre maximum de tokens en reponse",
            DefaultValueFactory = _ => 600
        };
        askCmd.Add(promptArg);
        askCmd.Add(maxTokensOpt);
        askCmd.SetAction(pr => HandleAsk(pr.GetValue(promptArg), pr.GetValue(maxTokensOpt)));

        var suggestCmd = new Command("suggest", "Suggere un template a partir de mots-cles");
        var keywordsArg = new Argument<string[]>("keywords")
        {
            Description = "Mots-cles (ex: api rest python)",
            Arity = ArgumentArity.OneOrMore
        };
        suggestCmd.Add(keywordsArg);
        suggestCmd.SetAction(pr => HandleSuggest(pr.GetValue(keywordsArg) ?? []));

        var explainCmd = new Command("explain", "Explique un concept");
        var conceptArg = new Argument<string>("concept") { Description = "Concept a expliquer" };
        explainCmd.Add(conceptArg);
        explainCmd.SetAction(pr => HandleExplain(pr.GetValue(conceptArg) ?? "concept inconnu"));

        var fixCmd = new Command("fix", "Propose une solution pour une erreur");
        var errorArg = new Argument<string>("error") { Description = "Message d'erreur" };
        fixCmd.Add(errorArg);
        fixCmd.SetAction(pr => HandleFix(pr.GetValue(errorArg) ?? "erreur inconnue"));

        var modelsCmd = new Command("models", "Liste les modeles par defaut par fournisseur");
        modelsCmd.SetAction(_ => HandleModels());

        Add(statusCmd);
        Add(testCmd);
        Add(askCmd);
        Add(suggestCmd);
        Add(explainCmd);
        Add(fixCmd);
        Add(modelsCmd);

        SetAction(_ =>
        {
            ConsoleService.Info("Sous-commandes : status, test, ask, suggest, explain, fix, models");
            ConsoleService.Info("Configuration : scaffold config set apiKey <cle>");
            ConsoleService.Info("                scaffold config set provider openai|claude|gemini|grok");
            return 0;
        });
    }

    private static int HandleStatus()
    {
        ConsoleService.ShowBanner("Assistant IA", "Configuration actuelle");
        ConsoleService.ShowKeyValue("Cle API", AIService.HasApiKey ? "configuree" : "absente");
        ConsoleService.ShowKeyValue("Fournisseur", AIService.Provider);

        if (AIService.HasApiKey)
            ConsoleService.ShowKeyValue("Modele", AIService.Model);

        ConsoleService.WriteLine();

        if (!AIService.HasApiKey)
        {
            ConsoleService.Info("Sans cle API, les commandes suggest/explain/fix utilisent la base de connaissances locale.");
            ConsoleService.Info("Pour activer l'IA : scaffold config set apiKey <votre-cle>");
        }

        return 0;
    }

    private static int HandleTest()
    {
        var (ok, message) = AIService.TestAsync().GetAwaiter().GetResult();

        if (ok)
            ConsoleService.Success(message);
        else
            ConsoleService.Error(message);

        return ok ? 0 : 1;
    }

    private static int HandleAsk(string prompt, int maxTokens)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            ConsoleService.Error("Question vide.");
            return 2;
        }

        if (!AIService.HasApiKey)
        {
            ConsoleService.Warning("Aucune cle API configuree.");
            ConsoleService.Info("Configure-la avec : scaffold config set apiKey <cle>");
            ConsoleService.Info("Sans cle, utilise `scaffold explain` ou `scaffold fix` (base locale).");
            return 1;
        }

        var answer = AIService.AskAsync(prompt, Math.Clamp(maxTokens, 1, 8192)).GetAwaiter().GetResult();

        if (string.IsNullOrWhiteSpace(answer))
        {
            ConsoleService.Error("Aucune reponse de l'assistant.");
            return 1;
        }

        Console.WriteLine();
        Console.WriteLine(answer.Trim());
        Console.WriteLine();
        return 0;
    }

    private static int HandleSuggest(string[] keywords)
    {
        var suggestion = AIService.SuggestAsync(keywords).GetAwaiter().GetResult();
        ConsoleService.Success($"Template suggere : {suggestion}");
        return 0;
    }

    private static int HandleExplain(string concept)
    {
        var (title, content) = AIService.ExplainAsync(concept).GetAwaiter().GetResult();

        if (string.IsNullOrWhiteSpace(content))
        {
            ConsoleService.Error($"Aucune explication disponible pour '{concept}'.");
            return 1;
        }

        ConsoleService.ShowBanner(title ?? concept, "Explication");
        Console.WriteLine(content);
        Console.WriteLine();
        return 0;
    }

    private static int HandleFix(string error)
    {
        var (title, fix) = AIService.FixAsync(error).GetAwaiter().GetResult();

        if (string.IsNullOrWhiteSpace(fix))
        {
            ConsoleService.Error("Aucune solution trouvee pour cette erreur.");
            return 1;
        }

        ConsoleService.ShowBanner(title ?? "Solution", "Correction proposee");
        Console.WriteLine(fix);
        Console.WriteLine();
        return 0;
    }

    private static int HandleModels()
    {
        ConsoleService.ShowBanner("Modeles par defaut", "Selon le fournisseur");
        foreach (var provider in new[] { "openai", "claude", "gemini", "grok" })
            ConsoleService.ShowKeyValue(provider, AIService.DefaultModel(provider));
        return 0;
    }
}
