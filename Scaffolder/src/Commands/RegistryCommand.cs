using System.CommandLine;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class RegistryCommand : Command
{
    public RegistryCommand() : base("registry", "Recherche et installe des templates depuis le registry")
    {
        var searchCmd = new Command("search", "Cherche un template dans le registry");
        var queryArg = new Argument<string>("query")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Terme de recherche (ex: api, react, rust)"
        };
        searchCmd.Add(queryArg);
        searchCmd.SetAction((ParseResult pr) => HandleSearch(pr.GetValue(queryArg)));

        var installCmd = new Command("install", "Installe un template depuis le registry");
        var tplArg = new Argument<string>("template")
        {
            Description = "Nom du template"
        };
        installCmd.Add(tplArg);
        installCmd.SetAction((ParseResult pr) => HandleInstall(pr.GetValue(tplArg)));

        var listCmd = new Command("list", "Liste les templates installes");
        listCmd.SetAction(_ => HandleList());

        Add(searchCmd);
        Add(installCmd);
        Add(listCmd);

        var graphCmd = new Command("graph", "Affiche le graphe de dependances des templates");
        graphCmd.SetAction(_ => HandleGraph());
        Add(graphCmd);

        var communityCmd = new Command("community", "Liste les templates communautaires disponibles");
        communityCmd.SetAction(_ => HandleCommunity());
        Add(communityCmd);

        SetAction(_ =>
        {
            ConsoleService.Info("Sous-commandes : search, install, list, graph, community");
            ConsoleService.Info("Exemple : scaffold registry search api");
            ConsoleService.Info("Exemple : scaffold registry community");
            return 0;
        });
    }

    private static int HandleSearch(string? query)
    {
        var templates = GetBuiltinTemplates();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.ToLowerInvariant();
            templates = templates.Where(t =>
                t.Name.Contains(q) || t.Description.Contains(q) ||
                t.Tags.Any(tag => tag.Contains(q))).ToList();
        }

        if (templates.Count == 0)
        {
            ConsoleService.Warning("Aucun template trouve.");
            return 1;
        }

        ConsoleService.Info($"Templates trouves ({templates.Count}) :");
        foreach (var t in templates.OrderBy(t => t.Name))
        {
            ConsoleService.Info($"  {t.Name,-20} {t.Description}");
            if (t.Tags.Length > 0)
                ConsoleService.Info($"    tags: {string.Join(", ", t.Tags)}");
        }

        ConsoleService.Info("");
        ConsoleService.Info("Pour installer : scaffold registry install <nom>");
        return 0;
    }

    private static int HandleInstall(string? template)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            ConsoleService.Error("Usage : scaffold registry install <template>");
            return 1;
        }

        var all = GetBuiltinTemplates();
        var match = all.FirstOrDefault(t =>
            t.Name.Equals(template, StringComparison.OrdinalIgnoreCase) ||
            t.Aliases.Contains(template, StringComparer.OrdinalIgnoreCase));

        if (match.Name == null)
        {
            ConsoleService.Error($"Template '{template}' introuvable.");
            ConsoleService.Info("Cherche avec : scaffold registry search " + template);
            return 1;
        }

        ConsoleService.Success($"Template '{match.Name}' selectionne :");
        ConsoleService.Info($"  Description : {match.Description}");
        ConsoleService.Info($"  Tags : {string.Join(", ", match.Tags)}");
        ConsoleService.Info("");
        ConsoleService.Info($"  scaffold new --template={match.AdapterCmd}");
        return 0;
    }

    private static int HandleList()
    {
        var templates = GetBuiltinTemplates();
        ConsoleService.Info($"Templates installes ({templates.Count}) :");
        foreach (var t in templates.OrderBy(t => t.Name))
            ConsoleService.Info($"  {t.Name,-20} {t.Description}");
        return 0;
    }

    private static List<(string Name, string Description, string[] Tags, string[] Aliases, string AdapterCmd)> GetBuiltinTemplates()
    {
        return
        [
            ("hello", "Application Hello World minimaliste", ["demo", "test", "debutant"], ["helloworld", "demo"], "hello"),
            ("console", "Application console .NET", ["dotnet", "c#", "cli"], ["dotnet-console"], "dotnet console"),
            ("webapi", "API REST ASP.NET Core", ["dotnet", "api", "rest", "backend"], ["api", "rest-api"], "dotnet webapi"),
            ("blazor", "Application Blazor WebAssembly", ["dotnet", "wasm", "frontend"], ["wasm"], "dotnet blazor"),
            ("maui", "Application mobile MAUI", ["dotnet", "mobile", "android", "ios"], ["mobile"], "dotnet maui"),
            ("vite", "Application Vite + React/TypeScript", ["javascript", "typescript", "frontend", "react"], ["react", "spa"], "npm vite"),
            ("next", "Application Next.js", ["javascript", "typescript", "ssr", "frontend"], ["nextjs", "ssr"], "npm next"),
            ("vue", "Application Vue 3 + Vite", ["javascript", "typescript", "frontend"], ["vuejs", "vue3"], "npm vue"),
            ("nuxt", "Application Nuxt 3", ["javascript", "typescript", "ssr", "frontend"], ["nuxtjs"], "npm nuxt"),
            ("svelte", "Application SvelteKit", ["javascript", "typescript", "frontend"], ["sveltekit"], "npm svelte"),
            ("cargo", "Projet Rust", ["rust", "backend", "cli"], ["rust"], "cargo"),
            ("go", "Projet Go", ["go", "golang", "backend", "cli"], ["golang"], "go"),
            ("python", "Projet Python", ["python", "backend", "cli"], ["python3"], "python"),
            ("flutter", "Application Flutter", ["dart", "mobile", "android", "ios"], ["dart"], "flutter"),
            ("laravel", "Application Laravel", ["php", "backend", "web"], ["php"], "composer laravel"),
            ("symfony", "Application Symfony", ["php", "backend", "web"], [], "composer symfony"),
            ("rails", "Application Ruby on Rails", ["ruby", "backend", "web"], ["ruby"], "rails"),
            ("gradle", "Projet Gradle (Kotlin/Java)", ["kotlin", "java", "jvm"], ["kotlin", "java"], "gradle"),
            ("swift", "Projet Swift", ["swift", "ios", "macos"], ["apple"], "swift"),
            ("zig", "Projet Zig", ["zig", "system"], [], "zig"),
            ("elixir", "Projet Elixir", ["elixir", "phoenix", "backend"], ["phoenix"], "mix"),
            ("haskell", "Projet Haskell", ["haskell", "functional"], [], "cabal"),
        ];
    }

    private static int HandleGraph()
    {
        ConsoleService.Info("Graphe de dependances des templates :");
        Console.WriteLine();

        var deps = new Dictionary<string, string[]>
        {
            ["react"] = ["vite"],
            ["vue"] = ["vite"],
            ["nuxt"] = ["vue"],
            ["svelte"] = ["vite"],
            ["next"] = [],
            ["solid"] = ["vite"],
            ["blazor"] = ["dotnet-console"],
            ["maui"] = ["dotnet-console"],
            ["webapi"] = ["dotnet-console"],
            ["console"] = [],
            ["cargo"] = [],
            ["go"] = [],
            ["python"] = [],
            ["flutter"] = [],
            ["laravel"] = ["composer"],
            ["symfony"] = ["composer"],
            ["rails"] = [],
            ["gradle"] = [],
            ["swift"] = [],
            ["zig"] = [],
            ["elixir"] = [],
            ["haskell"] = [],
        };

        // Print as ASCII graph
        var visited = new HashSet<string>();
        foreach (var tpl in deps.Keys.OrderBy(k => k))
        {
            PrintDeps(tpl, deps, "", visited, true);
        }

        Console.WriteLine();
        ConsoleService.Info("  * = template racine (aucune dependance)");
        ConsoleService.Info("  -> = depend de");
        return 0;
    }

    private static void PrintDeps(string name, Dictionary<string, string[]> deps, string indent, HashSet<string> visited, bool isLast)
    {
        var prefix = indent.Length == 0 ? "" : isLast ? "└── " : "├── ";
        var suffix = deps.TryGetValue(name, out var d) && d.Length == 0 ? " *" : "";

        Console.WriteLine($"{indent}{prefix}{name}{suffix}");

        if (!deps.TryGetValue(name, out var children) || children.Length == 0 || visited.Contains(name))
            return;

        visited.Add(name);
        var newIndent = indent + (isLast ? "    " : "│   ");

        for (var i = 0; i < children.Length; i++)
        {
            PrintDeps(children[i], deps, newIndent, visited, i == children.Length - 1);
        }
    }

    private static int HandleCommunity()
    {
        ConsoleService.Info("Templates communautaires :");
        Console.WriteLine();

        // Built-in community templates
        var communityTemplates = new[]
        {
            (Name: "express-api", Desc: "API REST Express.js avec MongoDB", Author: "community", Stars: 42),
            (Name: "fastapi-crud", Desc: "API CRUD FastAPI avec SQLAlchemy", Author: "community", Stars: 38),
            (Name: "next-blog", Desc: "Blog Next.js avec MDX et Tailwind", Author: "community", Stars: 31),
            (Name: "spring-api", Desc: "API Spring Boot avec PostgreSQL", Author: "community", Stars: 27),
            (Name: "svelte-shop", Desc: "E-commerce SvelteKit avec Stripe", Author: "community", Stars: 19),
            (Name: "react-dashboard", Desc: "Dashboard React avec Material UI", Author: "community", Stars: 15),
        };

        foreach (var (name, desc, author, stars) in communityTemplates)
        {
            Console.WriteLine($"  {name,-20} ⭐ {stars,-3} {desc}");
            Console.WriteLine($"  {' ',20}    par {author}");
            Console.WriteLine();
        }

        ConsoleService.Info("Pour installer un template communautaire :");
        ConsoleService.Info("  scaffold registry install <nom>");

        // Try remote registry
        var registryUrl = ConfigService.Get("registry.url");
        if (!string.IsNullOrWhiteSpace(registryUrl))
        {
            Console.WriteLine();
            ConsoleService.Info($"Templates distants depuis {registryUrl}...");

            try
            {
                using var client = new HttpClient();
                var response = client.GetAsync($"{registryUrl.TrimEnd('/')}/api/templates")
                    .Result;
                if (response.IsSuccessStatusCode)
                {
                    ConsoleService.Success("Connexion au registry distant réussie.");
                }
                else
                {
                    ConsoleService.Warning($"Registry distant injoignable : {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                ConsoleService.Warning($"Registry distant : {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine();
            ConsoleService.Info("Configure un registry distant pour accéder à plus de templates :");
            ConsoleService.Info("  scaffold config set registry.url https://mon-registry.com");
        }

        return 0;
    }
}
