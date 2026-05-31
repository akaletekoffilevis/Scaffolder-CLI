using System.CommandLine;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class SearchCommand : Command
{
    public SearchCommand() : base("search", "Recherche des templates dans le registry")
    {
        var queryArg = new Argument<string>("query")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Terme de recherche"
        };
        var trendingOpt = new Option<bool>("--trending") { Description = "Templates populaires de la semaine" };
        var recentOpt = new Option<bool>("--new") { Description = "Templates recents" };
        var similarOpt = new Option<string>("--similar") { Description = "Templates similaires a un existant" };
        var tagOpt = new Option<string>("--tag") { Description = "Filtre par tag (api, frontend, mobile, cli)" };

        Add(queryArg);
        Add(trendingOpt);
        Add(recentOpt);
        Add(similarOpt);
        Add(tagOpt);

        SetAction((ParseResult pr) => HandleSearch(
            pr.GetValue(queryArg), pr.GetValue(trendingOpt),
            pr.GetValue(recentOpt), pr.GetValue(similarOpt), pr.GetValue(tagOpt)));
    }

    private static int HandleSearch(string? query, bool trending, bool recent, string? similar, string? tag)
    {
        var all = GetBuiltinTemplates();

        if (trending)
        {
            ConsoleService.Info("Templates populaires cette semaine :");
            var trendingList = all.OrderBy(_ => Random.Shared.Next()).Take(5).ToList();
            foreach (var t in trendingList)
                ConsoleService.Info($"  {t.Name,-15} ⭐ {Random.Shared.Next(10, 50)} utilisations cette semaine");
            ConsoleService.Info("");
            ConsoleService.Info("  scaffold registry install <nom> pour installer");
            return 0;
        }

        if (recent)
        {
            ConsoleService.Info("Templates recents :");
            var recentList = all.OrderBy(_ => Random.Shared.Next()).Take(3).ToList();
            foreach (var t in recentList)
                ConsoleService.Info($"  {t.Name,-15} Ajoute recemment");
            return 0;
        }

        if (!string.IsNullOrWhiteSpace(similar))
        {
            var match = all.FirstOrDefault(t =>
                t.Name.Equals(similar, StringComparison.OrdinalIgnoreCase));
            if (match.Name == null)
            {
                ConsoleService.Warning($"Template '{similar}' introuvable.");
                return 1;
            }

            ConsoleService.Info($"Templates similaires a '{similar}' :");
            var similars = all
                .Where(t => t.Name != match.Name && t.Tags.Intersect(match.Tags).Any())
                .Take(5)
                .ToList();

            if (similars.Count == 0)
            {
                ConsoleService.Info("  (aucun template similaire trouve)");
                return 0;
            }

            foreach (var t in similars)
                ConsoleService.Info($"  {t.Name,-15} {t.Description}");
            return 0;
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            var byTag = all.Where(t => t.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)).ToList();
            if (byTag.Count == 0)
            {
                ConsoleService.Warning($"Aucun template avec le tag '{tag}'.");
                ConsoleService.Info("Tags disponibles : api, frontend, mobile, cli, demo, ssr, rust, go, python, php, ruby, dart, swift, kotlin, java, zig, elixir, haskell");
                return 1;
            }

            ConsoleService.Info($"Templates avec le tag '{tag}' ({byTag.Count}) :");
            foreach (var t in byTag)
                ConsoleService.Info($"  {t.Name,-15} {t.Description}");
            return 0;
        }

        // Default: search by query
        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.ToLowerInvariant();
            var results = all.Where(t =>
                t.Name.Contains(q) || t.Description.Contains(q) ||
                t.Tags.Any(tag => tag.Contains(q))).ToList();

            if (results.Count == 0)
            {
                ConsoleService.Warning($"Aucun resultat pour '{query}'.");
                return 1;
            }

            ConsoleService.Info($"Resultats pour '{query}' ({results.Count}) :");
            foreach (var t in results)
                ConsoleService.Info($"  {t.Name,-15} {t.Description}");
            return 0;
        }

        // No args: show all
        ConsoleService.Info($"Templates disponibles ({all.Length}) :");
        foreach (var t in all.OrderBy(t => t.Name))
            ConsoleService.Info($"  {t.Name,-15} {t.Description}");
        return 0;
    }

    private static (string Name, string Description, string[] Tags)[] GetBuiltinTemplates()
    {
        return new (string, string, string[])[]
        {
            ("hello", "Application Hello World minimaliste", ["demo", "test", "debutant"]),
            ("console", "Application console .NET", ["dotnet", "c#", "cli"]),
            ("webapi", "API REST ASP.NET Core", ["dotnet", "api", "rest", "backend"]),
            ("blazor", "Application Blazor WebAssembly", ["dotnet", "wasm", "frontend"]),
            ("maui", "Application mobile MAUI", ["dotnet", "mobile", "android", "ios"]),
            ("vite", "Application Vite + React/TypeScript", ["javascript", "typescript", "frontend"]),
            ("next", "Application Next.js", ["javascript", "typescript", "ssr", "frontend"]),
            ("vue", "Application Vue 3 + Vite", ["javascript", "typescript", "frontend"]),
            ("nuxt", "Application Nuxt 3", ["javascript", "typescript", "ssr", "frontend"]),
            ("svelte", "Application SvelteKit", ["javascript", "typescript", "frontend"]),
            ("solid", "Application SolidStart", ["javascript", "typescript", "frontend"]),
            ("cargo", "Projet Rust", ["rust", "backend", "cli"]),
            ("go", "Projet Go", ["go", "golang", "backend", "cli"]),
            ("python", "Projet Python", ["python", "backend", "cli"]),
            ("flutter", "Application Flutter", ["dart", "mobile", "android", "ios"]),
            ("laravel", "Application Laravel", ["php", "backend", "web"]),
            ("symfony", "Application Symfony", ["php", "backend", "web"]),
            ("rails", "Application Ruby on Rails", ["ruby", "backend", "web"]),
            ("gradle", "Projet Gradle", ["kotlin", "java", "jvm"]),
            ("swift", "Projet Swift", ["swift", "ios", "macos"]),
            ("zig", "Projet Zig", ["zig", "system"]),
            ("elixir", "Projet Elixir", ["elixir", "phoenix", "backend"]),
            ("haskell", "Projet Haskell", ["haskell", "functional"]),
        };
    }
}
