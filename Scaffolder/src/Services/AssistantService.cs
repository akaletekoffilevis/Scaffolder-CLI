using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using Scaffolder.Adapters;
using Scaffolder.Commands;

namespace Scaffolder.Services;

/// <summary>
/// Assistant conscient du contexte : il connaît le dossier courant, les outils installés,
/// les templates et les commandes Scaffolder, et propose une commande exacte que l'utilisateur confirme.
/// </summary>
public static class AssistantService
{
    public sealed record Reply(string Text, string? Command, bool FromAi);

    // Commandes qu'on ne lance jamais depuis l'assistant (interactives, longues ou qui modifient le CLI lui-même).
    private static readonly HashSet<string> BlockedSubcommands =
        ["ui", "tui", "update", "upgrade", "bug", "watch", "completion", "plugin", "ai"];

    private static readonly string[] CommandNames =
    [
        "new", "stack", "add", "init", "list", "stats", "doctor", "info", "run", "build", "test",
        "clean", "lint", "format", "template", "registry", "search", "compare", "project", "batch",
        "audit", "generate", "docker", "github", "ci", "env", "license", "deploy", "store",
        "update-deps", "workspace", "migrate", "config", "explain", "suggest", "fix"
    ];

    public static string DetectProject(string cwd)
    {
        var found = new List<string>();
        void Check(string pattern, string label)
        {
            var hits = Directory.Exists(cwd) ? Directory.GetFiles(cwd, pattern) : [];
            if (hits.Length > 0) found.Add($"{label} ({Path.GetFileName(hits[0])})");
        }

        Check("*.sln", "solution .NET");
        Check("*.csproj", "projet .NET");
        Check("package.json", "projet Node/JS");
        Check("Cargo.toml", "projet Rust");
        Check("go.mod", "projet Go");
        Check("pyproject.toml", "projet Python");
        Check("requirements.txt", "projet Python (pip)");
        Check("pubspec.yaml", "projet Flutter/Dart");
        Check("composer.json", "projet PHP");
        Check("Gemfile", "projet Ruby");
        if (File.Exists(Path.Combine(cwd, "Dockerfile"))) found.Add("Dockerfile");
        if (Directory.Exists(Path.Combine(cwd, ".git"))) found.Add("dépôt Git");

        return found.Count == 0 ? "aucun projet reconnu dans le dossier courant" : string.Join(", ", found);
    }

    /// <summary>Liste le contenu du dossier courant (dossiers d'abord), pour répondre aux questions sur les fichiers.</summary>
    public static string DescribeFolder(string cwd, int limit = 80)
    {
        try
        {
            var dirs = Directory.GetDirectories(cwd).Select(d => Path.GetFileName(d) + "/").OrderBy(n => n);
            var files = Directory.GetFiles(cwd).Select(Path.GetFileName).OrderBy(n => n);
            var all = dirs.Concat(files!).ToList();
            var shown = all.Take(limit);
            var suffix = all.Count > limit ? $" … (+{all.Count - limit} autres)" : "";
            return all.Count == 0 ? "(dossier vide)" : string.Join(", ", shown) + suffix;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return "(contenu illisible)";
        }
    }

    public static string DetectTools()
    {
        string[] tools = ["dotnet", "node", "npm", "cargo", "go", "python", "flutter", "composer", "git", "docker"];
        var present = tools.Where(ProcessService.CommandExists).ToArray();
        var missing = tools.Except(present).ToArray();
        return $"installés : {(present.Length == 0 ? "aucun" : string.Join(", ", present))} ; absents : {(missing.Length == 0 ? "aucun" : string.Join(", ", missing))}";
    }

    public static string BuildSystemPrompt(string cwd)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Tu es l'assistant intégré de Scaffolder, un CLI qui génère des projets.");
        sb.AppendLine("Réponds en français, de façon concise et concrète.");
        sb.AppendLine();
        sb.AppendLine("CONTEXTE ACTUEL");
        sb.AppendLine($"- Dossier courant : {cwd}");
        sb.AppendLine($"- Projet détecté : {DetectProject(cwd)}");
        sb.AppendLine($"- Outils : {DetectTools()}");
        sb.AppendLine($"- Contenu du dossier courant : {DescribeFolder(cwd)}");
        sb.AppendLine();
        sb.AppendLine("TEMPLATES DISPONIBLES (noms exacts)");
        sb.AppendLine("- hello --language <python|javascript|typescript|c#|rust|go|ruby|php|dart|swift|kotlin|java|zig|scala|elixir|crystal>");
        foreach (var adapter in NewCommand.Adapters)
            sb.AppendLine($"- {adapter.Name} : {string.Join(", ", adapter.SubTemplates)}");
        sb.AppendLine();
        sb.AppendLine("COMMANDES UTILES");
        sb.AppendLine("- scaffold new --template <nom> --name <nom> [--output <dossier>] [--language <lang>] [--auth none|jwt|identity] [--db none|sqlite|sqlserver|postgresql] [--arch mvc|clean|minimal] [--docker] [--ci] [--no-git] --silent");
        sb.AppendLine("- scaffold stack --name <nom> --frontend react|vue|svelte|next|nuxt|none --backend webapi|fastapi|laravel|rails|none --db postgres|mysql|sqlite|mongodb|none");
        sb.AppendLine("- scaffold add <type> <nom>   (dans le dossier du projet ; liste : scaffold add --list)");
        sb.AppendLine("- scaffold doctor   (vérifie les outils)");
        sb.AppendLine("- scaffold list [--available]   (templates)");
        sb.AppendLine("- scaffold stats");
        sb.AppendLine("- Autres sous-commandes : " + string.Join(", ", CommandNames));
        sb.AppendLine();
        sb.AppendLine("FORMAT DE RÉPONSE OBLIGATOIRE");
        sb.AppendLine("Réponds UNIQUEMENT par un objet JSON sur une ligne, sans bloc de code :");
        sb.AppendLine("{\"reply\": \"ta réponse en français\", \"command\": \"scaffold ...\" ou null}");
        sb.AppendLine("- Mets une commande seulement si elle aide vraiment, et utilise uniquement les noms ci-dessus.");
        sb.AppendLine("- N'invente jamais de template ni d'option.");
        sb.AppendLine("- Réponds à partir du CONTEXTE (liste des dossiers, outils…) quand la question porte dessus.");
        sb.AppendLine("- Tu ne peux pas exécuter de commande shell ni modifier de fichiers toi-même : si la demande l'exige, dis-le et propose la commande scaffold la plus proche, ou demande une précision.");
        sb.AppendLine("- Texte simple, sans markdown (pas d'astérisques, pas de titres).");
        return sb.ToString();
    }

    public static async Task<Reply> AskAsync(string userMessage, IReadOnlyList<(string Role, string Text)> history, string cwd)
    {
        if (!AIService.HasApiKey)
            return OfflineReply(userMessage, cwd);

        var prompt = new StringBuilder();
        var recent = history.TakeLast(8);
        foreach (var (role, text) in recent)
            prompt.AppendLine($"{(role == "user" ? "Utilisateur" : "Assistant")} : {text}");
        prompt.AppendLine($"Utilisateur : {userMessage}");
        prompt.AppendLine("Assistant (JSON) :");

        var raw = await AIService.AskAsync(
            prompt.ToString(), maxTokens: 2048, system: BuildSystemPrompt(cwd), temperature: 0.2);

        if (string.IsNullOrWhiteSpace(raw))
        {
            var offline = OfflineReply(userMessage, cwd);
            return offline with { Text = "Le fournisseur IA ne répond pas.\n\n" + offline.Text };
        }

        return ParseReply(raw);
    }

    public static Reply ParseReply(string raw)
    {
        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');
        if (start >= 0 && end > start)
        {
            try
            {
                using var doc = JsonDocument.Parse(raw[start..(end + 1)]);
                var root = doc.RootElement;
                var text = root.TryGetProperty("reply", out var r) && r.ValueKind == JsonValueKind.String
                    ? r.GetString() ?? "" : raw.Trim();
                var command = root.TryGetProperty("command", out var c) && c.ValueKind == JsonValueKind.String
                    ? Regex.Replace(c.GetString() ?? "", @"\s+", " ").Trim() : null;
                return new Reply(text.Trim(), IsAllowed(command) ? command : null, true);
            }
            catch (JsonException)
            {
                // Réponse non JSON : on affiche le texte brut.
            }
        }

        // JSON tronqué ou mal formé : on récupère au moins le texte de "reply".
        var match = Regex.Match(raw, "\"reply\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)", RegexOptions.Singleline);
        if (match.Success)
        {
            var text = Regex.Unescape(match.Groups[1].Value);
            return new Reply(text.Trim(), null, true);
        }

        return new Reply(raw.Trim(), null, true);
    }

    /// <summary>Accepte uniquement `scaffold <sous-commande> ...` avec une sous-commande autorisée.</summary>
    public static bool IsAllowed(string? command)
    {
        if (string.IsNullOrWhiteSpace(command)) return false;
        var tokens = ProcessService.Tokenize(command);
        if (tokens.Count < 2 || !tokens[0].Equals("scaffold", StringComparison.OrdinalIgnoreCase)) return false;
        var sub = tokens[1];
        if (sub.StartsWith('-')) return false;
        return !BlockedSubcommands.Contains(sub) && tokens.All(t => !t.Contains('`') && !t.Contains('$'));
    }

    /// <summary>Arguments à passer au binaire (sans le mot « scaffold »).</summary>
    public static List<string> ToArguments(string command) =>
        ProcessService.Tokenize(command).Skip(1).ToList();

    private static Reply OfflineReply(string message, string cwd)
    {
        var words = message.Split([' ', ',', ';', '.', '!', '?'], StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.ToLowerInvariant()).ToArray();

        var sb = new StringBuilder();
        sb.AppendLine($"Projet détecté : {DetectProject(cwd)}.");
        sb.AppendLine($"Outils : {DetectTools()}.");
        sb.AppendLine();
        sb.AppendLine("Mode hors ligne (aucune clé API ou fournisseur injoignable) :");
        sb.Append(KnowledgeBase.Suggest(words));
        return new Reply(sb.ToString().Trim(), null, false);
    }
}
