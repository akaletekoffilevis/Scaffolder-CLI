using System.CommandLine;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class TemplateCommand : Command
{
    public TemplateCommand() : base("template", "Gère les templates de projet")
    {
        AddPublish();
        AddValidate();
        AddLock();
        AddUnlock();
        AddHistory();
        AddDeps();
        AddStats();
        AddFromDir();

        SetAction(_ =>
        {
            ConsoleService.Info("Sous-commandes : publish, validate, lock, unlock, history, deps, stats, from-dir");
            return 0;
        });
    }

    private void AddPublish()
    {
        var cmd = new Command("publish", "Publie un template local sur le registry");
        var pathArg = new Argument<DirectoryInfo?>("path")
        {
            Description = "Chemin du dossier template",
            Arity = ArgumentArity.ZeroOrOne
        };
        var nameOpt = new Option<string>("--name") { Description = "Nom du template" };
        var descOpt = new Option<string>("--description") { Description = "Description du template" };
        var tagsOpt = new Option<string[]>("--tags")
        {
            Description = "Tags",
            Arity = ArgumentArity.ZeroOrMore
        };
        var remoteOpt = new Option<bool>("--remote")
        {
            Description = "Publie sur le registry distant (si configuré)"
        };
        cmd.Add(pathArg);
        cmd.Add(nameOpt);
        cmd.Add(descOpt);
        cmd.Add(tagsOpt);
        cmd.Add(remoteOpt);

        cmd.SetAction((ParseResult pr) => HandlePublish(
            pr.GetValue(pathArg), pr.GetValue(nameOpt),
            pr.GetValue(descOpt), pr.GetValue(tagsOpt),
            pr.GetValue(remoteOpt)));

        Add(cmd);
    }

    private void AddValidate()
    {
        var cmd = new Command("validate", "Valide la structure d'un template local");
        var pathArg = new Argument<DirectoryInfo?>("path")
        {
            Description = "Chemin du dossier template",
            Arity = ArgumentArity.ZeroOrOne
        };
        cmd.Add(pathArg);
        cmd.SetAction((ParseResult pr) => HandleValidate(pr.GetValue(pathArg)));
        Add(cmd);
    }

    private void AddLock()
    {
        var cmd = new Command("lock", "Verrouille un template a une version exacte");
        var tplArg = new Argument<string>("template") { Description = "Nom du template" };
        var versionArg = new Argument<string>("version") { Description = "Version (ex: 1.2.3)" };
        cmd.Add(tplArg);
        cmd.Add(versionArg);
        cmd.SetAction((ParseResult pr) => HandleLock(
            pr.GetValue(tplArg), pr.GetValue(versionArg)));
        Add(cmd);
    }

    private void AddUnlock()
    {
        var cmd = new Command("unlock", "Déverrouille un template");
        var tplArg = new Argument<string>("template") { Description = "Nom du template" };
        cmd.Add(tplArg);
        cmd.SetAction((ParseResult pr) => HandleUnlock(pr.GetValue(tplArg)));
        Add(cmd);
    }

    private void AddHistory()
    {
        var cmd = new Command("history", "Affiche l'historique des versions d'un template");
        var tplArg = new Argument<string>("template") { Description = "Nom du template" };
        cmd.Add(tplArg);
        cmd.SetAction((ParseResult pr) => HandleHistory(pr.GetValue(tplArg)));
        Add(cmd);
    }

    private void AddDeps()
    {
        var cmd = new Command("deps", "Affiche les dependances d'un template");
        var tplArg = new Argument<string>("template") { Description = "Nom du template" };
        cmd.Add(tplArg);
        cmd.SetAction((ParseResult pr) => HandleDeps(pr.GetValue(tplArg)));
        Add(cmd);
    }

    private void AddStats()
    {
        var cmd = new Command("stats", "Affiche les statistiques d'utilisation d'un template");
        var tplArg = new Argument<string>("template") { Description = "Nom du template" };
        cmd.Add(tplArg);
        cmd.SetAction((ParseResult pr) => HandleStats(pr.GetValue(tplArg)));
        Add(cmd);
    }

    private static int HandlePublish(DirectoryInfo? path, string? name, string? description, string[]? tags, bool remote)
    {
        var cwd = path?.FullName ?? Directory.GetCurrentDirectory();

        // Validate template first
        var (valid, errors) = ValidateTemplate(cwd);
        if (!valid)
        {
            ConsoleService.Error($"Template invalide : {errors.Length} erreurs");
            foreach (var e in errors) ConsoleService.Error($"  - {e}");
            return 1;
        }

        name ??= Path.GetFileName(cwd);
        description ??= $"Template '{name}' publie depuis Scaffolder";

        var registryDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".scaffolder", "registry", name);

        if (Directory.Exists(registryDir))
        {
            ConsoleService.Warning($"Le template '{name}' existe deja dans le registry.");
            var answer = ConsoleService.Prompt("Ecraser ? (o/N)", "N");
            if (answer?.ToLowerInvariant() != "o" && answer?.ToLowerInvariant() != "oui")
                return 1;
            Directory.Delete(registryDir, true);
        }

        CopyDirectory(cwd, registryDir);

        // Save metadata
        var meta = new Dictionary<string, object>
        {
            ["name"] = name,
            ["description"] = description,
            ["tags"] = tags ?? [],
            ["version"] = "1.0.0",
            ["published"] = DateTime.UtcNow.ToString("o"),
            ["downloads"] = 0
        };

        var metaJson = System.Text.Json.JsonSerializer.Serialize(meta, JsonContext.Default.DictionaryStringObject);
        File.WriteAllText(Path.Combine(registryDir, "metadata.json"), metaJson);

        ConsoleService.Success($"Template '{name}' publie dans le registry local.");
        ConsoleService.Info($"  Chemin : {registryDir}");
        ConsoleService.Info("  Les autres utilisateurs peuvent l'installer avec :");
        ConsoleService.Info($"  scaffold registry install {name}");

        if (remote)
        {
            var registryUrl = ConfigService.Get("registry.url");
            if (string.IsNullOrWhiteSpace(registryUrl))
            {
                ConsoleService.Warning("Aucun registry distant configuré.");
                ConsoleService.Info("Configure avec :");
                ConsoleService.Info("  scaffold config set registry.url https://mon-registry.com");
                return 0;
            }

            ConsoleService.Info($"Publication vers le registry distant : {registryUrl}...");

            try
            {
                using var client = new HttpClient();
                var packagePath = Path.Combine(registryDir, "metadata.json");
                var content = new MultipartFormDataContent();
                content.Add(new StringContent(metaJson), "metadata");

                // Add template files as zip
                var zipPath = Path.GetTempFileName() + ".zip";
                if (File.Exists("/bin/zip"))
                {
                    ProcessService.RunAsync("zip", $"-r \"{zipPath}\" .", cwd).Wait();
                    content.Add(new ByteArrayContent(File.ReadAllBytes(zipPath)), "archive", $"{name}.zip");
                    try { File.Delete(zipPath); } catch { }
                }

                var response = client.PostAsync($"{registryUrl.TrimEnd('/')}/api/templates", content)
                    .Result;

                if (response.IsSuccessStatusCode)
                    ConsoleService.Success($"Template '{name}' publié sur le registry distant.");
                else
                    ConsoleService.Warning($"Échec de la publication distante : {response.StatusCode}");
            }
            catch (Exception ex)
            {
                ConsoleService.Warning($"Impossible de contacter le registry distant : {ex.Message}");
                ConsoleService.Info("Le template reste disponible localement.");
            }
        }

        return 0;
    }

    private static int HandleValidate(DirectoryInfo? path)
    {
        var cwd = path?.FullName ?? Directory.GetCurrentDirectory();
        var (valid, errors) = ValidateTemplate(cwd);

        if (valid)
        {
            ConsoleService.Success($"Template valide : {cwd}");
            return 0;
        }

        ConsoleService.Error($"Template invalide : {errors.Length} erreurs");
        foreach (var e in errors) ConsoleService.Error($"  - {e}");
        return 1;
    }

    private static int HandleLock(string? template, string? version)
    {
        if (string.IsNullOrWhiteSpace(template) || string.IsNullOrWhiteSpace(version))
        {
            ConsoleService.Error("Usage : scaffold template lock <template> <version>");
            return 1;
        }

        if (!IsValidSemver(version))
        {
            ConsoleService.Error($"Version invalide : '{version}'. Utilise le format semver (ex: 1.2.3).");
            return 1;
        }

        ConfigService.Set($"lock.{template}", version);
        ConsoleService.Success($"Template '{template}' verrouille a la version {version}.");
        return 0;
    }

    private static int HandleUnlock(string? template)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            ConsoleService.Error("Usage : scaffold template unlock <template>");
            return 1;
        }

        ConfigService.Set($"lock.{template}", "");
        ConsoleService.Success($"Template '{template}' deverouille.");
        return 0;
    }

    private static int HandleHistory(string? template)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            ConsoleService.Error("Usage : scaffold template history <template>");
            return 1;
        }

        var history = GetTemplateHistory(template);
        if (history.Count == 0)
        {
            ConsoleService.Warning($"Aucun historique pour '{template}'.");
            ConsoleService.Info("Les templates integres n'ont pas d'historique de version.");
            return 1;
        }

        ConsoleService.Info($"Historique de '{template}' :");
        foreach (var (ver, date) in history)
            ConsoleService.Info($"  {ver,-12} {date}");
        return 0;
    }

    private static int HandleDeps(string? template)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            ConsoleService.Error("Usage : scaffold template deps <template>");
            return 1;
        }

        var deps = GetTemplateDeps(template);
        if (deps.Count == 0)
        {
            ConsoleService.Info($"Aucune dependance pour '{template}'.");
            return 0;
        }

        ConsoleService.Info($"Dependances de '{template}' :");
        foreach (var (name, version) in deps)
            ConsoleService.Info($"  {name,-20} {version ?? "*"}");
        return 0;
    }

    private static int HandleStats(string? template)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            ConsoleService.Error("Usage : scaffold template stats <template>");
            return 1;
        }

        var tpl = FindTemplate(template);
        if (tpl.Name == null)
        {
            ConsoleService.Warning($"Template '{template}' introuvable.");
            return 1;
        }

        ConsoleService.Info($"Statistiques de '{tpl.Name}' :");
        Console.WriteLine($"  Description   : {tpl.Description}");
        Console.WriteLine($"  Tags          : {string.Join(", ", tpl.Tags)}");
        Console.WriteLine($"  Adapter       : {tpl.AdapterCmd}");
        Console.WriteLine($"  Downloads     : {new Random().Next(100, 9999)} (simule)");
        Console.WriteLine($"  Note          : {new Random().Next(35, 50) / 10.0:F1}/5.0 (simule)");
        return 0;
    }

    private static (bool Valid, string[] Errors) ValidateTemplate(string path)
    {
        var errors = new List<string>();

        if (!Directory.Exists(path))
        {
            errors.Add("Le dossier n'existe pas.");
            return (false, errors.ToArray());
        }

        var files = Directory.GetFiles(path);
        if (files.Length == 0)
        {
            errors.Add("Le dossier est vide. Au moins un fichier est requis.");
        }

        // Check for common template files
        var hasCsproj = files.Any(f => f.EndsWith(".csproj"));
        var hasJson = files.Any(f => f.EndsWith("package.json"));
        var hasCargo = files.Any(f => f.EndsWith("Cargo.toml"));
        var hasGoMod = files.Any(f => f.EndsWith("go.mod"));

        if (!hasCsproj && !hasJson && !hasCargo && !hasGoMod)
        {
            errors.Add("Aucun fichier projet detecte (.csproj, package.json, Cargo.toml, go.mod).");
        }

        // Check for README
        var hasReadme = files.Any(f =>
            Path.GetFileName(f).StartsWith("README", StringComparison.OrdinalIgnoreCase));
        if (!hasReadme)
        {
            errors.Add("README.md recommande mais optionnel.");
        }

        return (errors.Count == 0, errors.ToArray());
    }

    private void AddFromDir()
    {
        var cmd = new Command("from-dir", "Cree un template depuis un dossier existant (reverse scaffold)");
        var pathArg = new Argument<DirectoryInfo?>("path")
        {
            Description = "Dossier a analyser",
            Arity = ArgumentArity.ZeroOrOne
        };
        var nameOpt = new Option<string>("--name") { Description = "Nom du template" };
        cmd.Add(pathArg);
        cmd.Add(nameOpt);
        cmd.SetAction((ParseResult pr) => HandleFromDir(
            pr.GetValue(pathArg), pr.GetValue(nameOpt)));
        Add(cmd);
    }

    private static int HandleFromDir(DirectoryInfo? path, string? name)
    {
        var cwd = path?.FullName ?? Directory.GetCurrentDirectory();

        if (!Directory.Exists(cwd))
        {
            ConsoleService.Error($"Dossier introuvable : {cwd}");
            return 1;
        }

        name ??= Path.GetFileName(cwd);
        ConsoleService.Info($"Analyse de : {cwd}");
        Console.WriteLine();

        var files = Directory.GetFiles(cwd, "*", SearchOption.AllDirectories)
            .Where(f => !f.Contains(".git") && !f.Contains("node_modules")
                       && !f.Contains("bin/") && !f.Contains("obj/") && !f.Contains("target/"))
            .ToList();

        Console.WriteLine($"  Fichiers trouves : {files.Count}");
        Console.WriteLine();

        // Detect project type
        var hasCsproj = files.Any(f => f.EndsWith(".csproj"));
        var hasPackageJson = files.Any(f => Path.GetFileName(f) == "package.json");
        var hasCargoToml = files.Any(f => Path.GetFileName(f) == "Cargo.toml");
        var hasGoMod = files.Any(f => Path.GetFileName(f) == "go.mod");
        var hasDockerfile = files.Any(f => Path.GetFileName(f) == "Dockerfile");
        var hasReadme = files.Any(f => Path.GetFileName(f).StartsWith("README", StringComparison.OrdinalIgnoreCase));

        ConsoleService.Info("Structure detectee :");
        Console.WriteLine($"  Type   : {(hasCsproj ? ".NET" : hasPackageJson ? "Node.js" : hasCargoToml ? "Rust" : hasGoMod ? "Go" : "Inconnu")}");
        Console.WriteLine($"  Docker : {hasDockerfile}");
        Console.WriteLine($"  README : {hasReadme}");
        Console.WriteLine();

        // Create template from directory
        var registryDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".scaffolder", "registry", name);

        if (Directory.Exists(registryDir))
        {
            ConsoleService.Warning($"Le template '{name}' existe deja dans le registry.");
            var answer = ConsoleService.Prompt("Ecraser ? (o/N)", "N");
            if (answer?.ToLowerInvariant() != "o" && answer?.ToLowerInvariant() != "oui")
                return 1;
            Directory.Delete(registryDir, true);
        }

        CopyDirectory(cwd, registryDir);

        // Generate metadata
        var tags = new List<string>();
        if (hasCsproj) tags.Add("dotnet");
        if (hasPackageJson) tags.Add("node");
        if (hasCargoToml) tags.Add("rust");
        if (hasGoMod) tags.Add("go");
        if (hasDockerfile) tags.Add("docker");

        var meta = new Dictionary<string, object>
        {
            ["name"] = name,
            ["description"] = $"Template genere depuis {Path.GetFileName(cwd)}",
            ["tags"] = tags.ToArray(),
            ["version"] = "1.0.0",
            ["published"] = DateTime.UtcNow.ToString("o"),
            ["source"] = cwd,
            ["files"] = files.Count,
            ["downloads"] = 0
        };

        var metaJson = System.Text.Json.JsonSerializer.Serialize(meta, JsonContext.Default.DictionaryStringObject);
        File.WriteAllText(Path.Combine(registryDir, "metadata.json"), metaJson);

        ConsoleService.Success($"Template '{name}' cree depuis le dossier existant.");
        ConsoleService.Info($"  {files.Count} fichiers copies");
        ConsoleService.Info($"  Tags : {string.Join(", ", tags)}");
        Console.WriteLine();
        ConsoleService.Info("Pour generer un projet depuis ce template :");
        ConsoleService.Info($"  scaffold new --template={name} --name=mon-projet");

        return 0;
    }

    private static void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.GetFiles(source))
        {
            var destFile = Path.Combine(dest, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }
        foreach (var dir in Directory.GetDirectories(source))
        {
            var destDir = Path.Combine(dest, Path.GetFileName(dir));
            CopyDirectory(dir, destDir);
        }
    }

    private static bool IsValidSemver(string version)
    {
        var parts = version.Split('.');
        return parts.Length == 3 && parts.All(p => int.TryParse(p, out _));
    }

    private static List<(string Version, string Date)> GetTemplateHistory(string template)
    {
        // Built-in templates have simulated history
        var history = new Dictionary<string, List<(string, string)>>(StringComparer.OrdinalIgnoreCase)
        {
            ["hello"] = [("1.0.0", "2025-01-15"), ("1.1.0", "2025-03-01"), ("2.0.0", "2025-06-10")],
            ["webapi"] = [("1.0.0", "2025-02-01"), ("1.1.0", "2025-04-15")],
            ["vite"] = [("1.0.0", "2025-01-20"), ("2.0.0", "2025-05-01")],
            ["react"] = [("1.0.0", "2025-02-10"), ("1.1.0", "2025-03-15")],
        };

        return history.TryGetValue(template, out var h) ? h : [];
    }

    private static List<(string Name, string? Version)> GetTemplateDeps(string template)
    {
        var deps = new Dictionary<string, List<(string, string?)>>(StringComparer.OrdinalIgnoreCase)
        {
            ["react"] = [("vite", null)],
            ["vue"] = [("vite", null)],
            ["nuxt"] = [("vue", "3.x")],
            ["svelte"] = [("vite", null)],
            ["solid"] = [("vite", null)],
            ["blazor"] = [("dotnet", "9.0")],
            ["maui"] = [("dotnet", "9.0")],
        };

        return deps.TryGetValue(template, out var d) ? d : [];
    }

    private static (string Name, string Description, string[] Tags, string AdapterCmd) FindTemplate(string name)
    {
        var templates = GetBuiltinTemplates();
        return templates.FirstOrDefault(t =>
            t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    private static (string Name, string Description, string[] Tags, string AdapterCmd)[] GetBuiltinTemplates()
    {
        return new (string, string, string[], string)[]
        {
            ("hello", "Application Hello World minimaliste", ["demo", "test"], "hello"),
            ("console", "Application console .NET", ["dotnet", "cli"], "dotnet console"),
            ("webapi", "API REST ASP.NET Core", ["dotnet", "api"], "dotnet webapi"),
            ("blazor", "Application Blazor WebAssembly", ["dotnet", "wasm"], "dotnet blazor"),
            ("maui", "Application mobile MAUI", ["dotnet", "mobile"], "dotnet maui"),
            ("vite", "Application Vite + React/TypeScript", ["js", "ts", "frontend"], "npm vite"),
            ("next", "Application Next.js", ["js", "ts", "ssr"], "npm next"),
            ("vue", "Application Vue 3 + Vite", ["js", "ts", "frontend"], "npm vue"),
            ("nuxt", "Application Nuxt 3", ["js", "ts", "ssr"], "npm nuxt"),
            ("svelte", "Application SvelteKit", ["js", "ts", "frontend"], "npm svelte"),
            ("cargo", "Projet Rust", ["rust"], "cargo"),
            ("go", "Projet Go", ["go"], "go"),
            ("python", "Projet Python", ["python"], "python"),
            ("flutter", "Application Flutter", ["dart", "mobile"], "flutter"),
            ("laravel", "Application Laravel", ["php"], "composer laravel"),
            ("symfony", "Application Symfony", ["php"], "composer symfony"),
            ("rails", "Application Ruby on Rails", ["ruby"], "rails"),
            ("gradle", "Projet Gradle", ["kotlin", "java"], "gradle"),
            ("swift", "Projet Swift", ["swift"], "swift"),
            ("zig", "Projet Zig", ["zig"], "zig"),
            ("elixir", "Projet Elixir", ["elixir"], "mix"),
            ("haskell", "Projet Haskell", ["haskell"], "cabal"),
        };
    }
}
