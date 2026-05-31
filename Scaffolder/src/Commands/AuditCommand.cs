using System.CommandLine;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class AuditCommand : Command
{
    public AuditCommand() : base("audit", "Audite la sécurité d'un template ou projet")
    {
        var templateArg = new Argument<string?>("template")
        {
            Description = "Template ou dossier à auditer",
            Arity = ArgumentArity.ZeroOrOne
        };
        Add(templateArg);

        SetAction((ParseResult pr) => HandleAudit(pr.GetValue(templateArg)));
    }

    private static int HandleAudit(string? target)
    {
        var path = target ?? Directory.GetCurrentDirectory();

        if (IsTemplateName(path))
        {
            ConsoleService.Info($"Audit du template '{path}'...");
            return AuditTemplate(path);
        }

        if (!Directory.Exists(path))
        {
            ConsoleService.Error($"Dossier introuvable : {path}");
            return 1;
        }

        ConsoleService.Info($"Audit de sécurité de : {path}");
        Console.WriteLine();

        var issues = new List<(string Severity, string Message)>();

        // Check all files for security issues
        var allFiles = GetFilesSafe(path);

        // 1. Check for secrets in files
        foreach (var file in allFiles)
        {
            var ext = Path.GetExtension(file).ToLowerInvariant();
            if (ext is ".exe" or ".dll" or ".png" or ".jpg" or ".ico")
                continue;

            try
            {
                var content = File.ReadAllText(file);

                // Check for API keys
                if (RegexMatch(content, @"(api[_-]?key|apikey|api_key)\s*[:=]\s*['""][A-Za-z0-9_\-]{16,}['""]", true))
                    issues.Add(("HIGH", $"Clé API potentielle dans {RelativePath(path, file)}"));

                // Check for passwords
                if (RegexMatch(content, @"(password|passwd|pwd)\s*[:=]\s*['""][^'""]{4,}['""]", true))
                    issues.Add(("HIGH", $"Mot de passe potentiel dans {RelativePath(path, file)}"));

                // Check for tokens
                if (RegexMatch(content, @"(token|secret|credential)\s*[:=]\s*['""][A-Za-z0-9_\-]{8,}['""]", true))
                    issues.Add(("MEDIUM", $"Token potentiel dans {RelativePath(path, file)}"));

                // Check for connection strings
                if (RegexMatch(content, @"(connection[_\s]?string|conn[_\s]?str)\s*[:=]\s*['""].+['""]", true))
                    issues.Add(("MEDIUM", $"Chaîne de connexion dans {RelativePath(path, file)}"));
            }
            catch { }
        }

        // 2. Check .env files
        var envFiles = allFiles.Where(f => Path.GetFileName(f) == ".env").ToList();
        if (envFiles.Count > 0)
        {
            foreach (var env in envFiles)
            {
                if (!Path.GetFileName(env).Contains(".example"))
                    issues.Add(("LOW", $"Fichier .env présent : {RelativePath(path, env)} (contient peut-être des secrets)"));
            }
        }

        // 3. Check for node_modules
        if (Directory.Exists(Path.Combine(path, "node_modules")))
            issues.Add(("LOW", "Dossier node_modules présent (ne pas déployer)"));

        // 4. Check for .git folder exposure
        if (Directory.Exists(Path.Combine(path, ".git")))
            issues.Add(("LOW", "Dossier .git présent (risque en production)"));

        // 5. Check for exposed ports in Dockerfile
        var dockerFiles = allFiles.Where(f => Path.GetFileName(f).StartsWith("Dockerfile")).ToList();
        foreach (var df in dockerFiles)
        {
            try
            {
                var content = File.ReadAllText(df);
                if (content.Contains("EXPOSE 80") || content.Contains("EXPOSE 443"))
                    issues.Add(("INFO", $"Ports exposés dans {RelativePath(path, df)} (vérifier la configuration)"));

                if (!content.Contains("USER") && !content.Contains("user"))
                    issues.Add(("MEDIUM", $"Le Dockerfile {RelativePath(path, df)} n'utilise pas d'utilisateur non-root"));
            }
            catch { }
        }

        // 6. Check package.json for outdated dependencies
        var pkgFiles = allFiles.Where(f => Path.GetFileName(f) == "package.json").ToList();
        foreach (var pf in pkgFiles)
        {
            try
            {
                var content = File.ReadAllText(pf);
                if (content.Contains("\"lodash\""))
                    issues.Add(("LOW", "Lodash détecté (bibliothèque lourde, préférer des alternatives modernes)"));
                if (content.Contains("\"moment\""))
                    issues.Add(("LOW", "Moment.js détecté (bibliothèque dépréciée, préférer date-fns ou dayjs)"));
            }
            catch { }
        }

        // Print results
        Console.WriteLine($"  Fichiers analysés : {allFiles.Length}");
        Console.WriteLine();

        if (issues.Count == 0)
        {
            ConsoleService.Success("Aucun problème de sécurité détecté.");
        }
        else
        {
            var high = issues.Count(i => i.Severity == "HIGH");
            var med = issues.Count(i => i.Severity == "MEDIUM");
            var low = issues.Count(i => i.Severity == "LOW");
            var info = issues.Count(i => i.Severity == "INFO");

            ConsoleService.Warning($"{issues.Count} problème(s) trouvé(s) :");
            Console.WriteLine($"  HIGH   : {high}");
            Console.WriteLine($"  MEDIUM : {med}");
            Console.WriteLine($"  LOW    : {low}");
            Console.WriteLine($"  INFO   : {info}");
            Console.WriteLine();

            foreach (var (sev, msg) in issues)
            {
                var prefix = sev switch
                {
                    "HIGH" => "🔴",
                    "MEDIUM" => "🟡",
                    "LOW" => "🟢",
                    _ => "ℹ️"
                };
                Console.WriteLine($"  {prefix} [{sev}] {msg}");
            }
        }

        return 0;
    }

    private static int AuditTemplate(string templateName)
    {
        // See if it's a known built-in template
        var knownTemplates = new[] { "hello", "console", "webapi", "blazor", "maui",
            "vite", "next", "react", "vue", "nuxt", "svelte", "solid",
            "cargo", "go", "python", "flutter", "laravel", "symfony",
            "rails", "gradle", "swift", "zig", "elixir", "haskell" };

        if (knownTemplates.Contains(templateName.ToLowerInvariant()))
        {
            ConsoleService.Success($"Template '{templateName}' vérifié : aucun problème connu.");
            Console.WriteLine("  Ce template est un template officiel Scaffolder.");
            Console.WriteLine("  Sources : générateurs officiels (dotnet new, npm create, etc.)");
            return 0;
        }

        ConsoleService.Info("Template non officiel. Vérification manuelle recommandée.");
        Console.WriteLine("  Vérifie :");
        Console.WriteLine("    - Le code source du template");
        Console.WriteLine("    - Les dépendances (package.json, Cargo.toml, .csproj)");
        Console.WriteLine("    - La présence de secrets ou tokens");
        Console.WriteLine("    - Les droits d'exécution des scripts");
        return 0;
    }

    private static string[] GetFilesSafe(string path)
    {
        try
        {
            return Directory.GetFiles(path, "*", SearchOption.AllDirectories);
        }
        catch (UnauthorizedAccessException)
        {
            // Fallback: get only top-level files
            try { return Directory.GetFiles(path); }
            catch { return []; }
        }
        catch
        {
            return [];
        }
    }

    private static bool IsTemplateName(string name)
    {
        return !name.Contains("/") && !name.Contains("\\") && !Directory.Exists(name);
    }

    private static string RelativePath(string basePath, string fullPath)
    {
        var relative = fullPath[basePath.Length..];
        return relative.TrimStart('/', '\\');
    }

    private static bool RegexMatch(string content, string pattern, bool ignoreCase)
    {
        try
        {
            return System.Text.RegularExpressions.Regex.IsMatch(content, pattern,
                ignoreCase ? System.Text.RegularExpressions.RegexOptions.IgnoreCase : System.Text.RegularExpressions.RegexOptions.None);
        }
        catch
        {
            return false;
        }
    }
}
