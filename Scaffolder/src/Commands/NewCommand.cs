using System.CommandLine;
using System.Threading.Tasks;
using Scaffolder;
using Scaffolder.Adapters;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class NewCommand : Command
{
    private static readonly IAdapter[] Adapters =
    [
        new DotnetAdapter(),
        new NpmAdapter(),
        new CargoAdapter(),
        new GoAdapter(),
        new PythonAdapter(),
        new FlutterAdapter(),
        new ComposerAdapter(),
        new RailsAdapter(),
        new GradleAdapter(),
        new SwiftAdapter(),
        new ZigAdapter(),
        new MixAdapter(),
        new CabalAdapter(),
        new ScalaAdapter(),
        new CrystalAdapter()
    ];

    private static readonly string[] HelloLanguages =
    [
        "python", "javascript", "typescript", "c#", "rust", "go",
        "ruby", "php", "dart", "swift", "kotlin", "java", "zig",
        "scala", "elixir", "crystal"
    ];

    private readonly Option<string> _nameOpt = new("--name")
    {
        Description = "Nom du projet",
        Required = false
    };

    private readonly Option<string> _templateOpt = new("--template")
    {
        Description = "Template (hello, dotnet, npm, cargo, go, python, flutter)",
        Required = false
    };

    private readonly Option<string> _languageOpt = new("--language")
    {
        Description = $"Langage ({string.Join(", ", HelloLanguages)})",
        Required = false
    };

    private readonly Option<DirectoryInfo?> _outputOpt = new("--output")
    {
        Description = "Dossier de sortie",
        Required = false
    };

    private readonly Option<bool> _dryRunOpt = new("--dry-run")
    {
        Description = "Prévisualisation sans générer"
    };

    private readonly Option<bool> _silentOpt = new("--silent")
    {
        Description = "Mode silencieux (pour CI)"
    };

    private readonly Option<bool> _noGitOpt = new("--no-git")
    {
        Description = "Ne pas initialiser Git"
    };

    private readonly Option<bool> _verboseOpt = new("--verbose")
    {
        Description = "Mode verbeux (logs détaillés)"
    };

    private readonly Option<bool> _favOpt = new("--fav")
    {
        Description = "Utilise le template favori (configuré avec scaffold config set fav <template>)"
    };

    private readonly Option<string> _authOpt = new("--auth")
    {
        Description = "Type d'authentification (none, jwt, identity)",
        Required = false
    };

    private readonly Option<string> _dbOpt = new("--db")
    {
        Description = "Base de donnees (sqlite, sqlserver, postgresql, none)",
        Required = false
    };

    private readonly Option<string> _archOpt = new("--arch")
    {
        Description = "Architecture (clean, mvc, minimal)",
        Required = false
    };

    private readonly Option<bool> _dockerOpt = new("--docker")
    {
        Description = "Generer le Dockerfile"
    };

    private readonly Option<bool> _ciOpt = new("--ci")
    {
        Description = "Generer la config CI/CD"
    };

    private readonly Option<bool> _listOpt = new("--list", ["-l"])
    {
        Description = "Liste les templates disponibles"
    };

    private readonly Option<bool> _forceOpt = new("--force", ["-f"])
    {
        Description = "Ecrase le dossier de sortie sans confirmation"
    };

    private readonly Option<string> _frameworkOpt = new("--framework")
    {
        Description = "Framework cible .NET (ex: net10.0) ou variante (ex: react-ts)",
        Required = false
    };

    public NewCommand() : base("new", "Crée un nouveau projet")
    {
        Aliases.Add("create");

        Add(_nameOpt);
        Add(_templateOpt);
        Add(_languageOpt);
        Add(_outputOpt);
        Add(_dryRunOpt);
        Add(_silentOpt);
        Add(_noGitOpt);
        Add(_verboseOpt);
        Add(_favOpt);
        Add(_authOpt);
        Add(_dbOpt);
        Add(_archOpt);
        Add(_dockerOpt);
        Add(_ciOpt);
        Add(_listOpt);
        Add(_forceOpt);
        Add(_frameworkOpt);

        this.SetAction((ParseResult pr) => HandleNew(pr));
    }

    private int HandleNew(ParseResult pr)
    {
        var silent = pr.GetValue(_silentOpt);
        var dryRun = pr.GetValue(_dryRunOpt);
        var noGit = pr.GetValue(_noGitOpt);
        var verbose = pr.GetValue(_verboseOpt);
        var useFav = pr.GetValue(_favOpt);
        var auth = pr.GetValue(_authOpt);
        var db = pr.GetValue(_dbOpt);
        var arch = pr.GetValue(_archOpt);
        var docker = pr.GetValue(_dockerOpt);
        var ci = pr.GetValue(_ciOpt);
        var force = pr.GetValue(_forceOpt);
        var framework = pr.GetValue(_frameworkOpt);

        if (pr.GetValue(_listOpt))
            return ShowTemplateList();

        // --verbose global et --verbose local se cumulent (ne jamais ecraser AppOptions).
        ConsoleService.Verbose |= verbose;
        if (silent) ConsoleService.Quiet = true;

        var hasTemplate = !string.IsNullOrWhiteSpace(pr.GetValue(_templateOpt));
        var isInteractive = !hasTemplate && !useFav;

        if (!silent)
        {
            ConsoleService.ShowLogo();
            ConsoleService.WriteLine();
        }

        string? fav = null;
        if (useFav)
        {
            fav = GetFavTemplate();
            ConsoleService.Debug($"Template favori : {fav}");
        }

        var template = pr.GetValue(_templateOpt) ?? fav;
        var name = pr.GetValue(_nameOpt);
        var language = pr.GetValue(_languageOpt);
        var outputDir = pr.GetValue(_outputOpt)?.FullName;

        if (isInteractive)
        {
            (name, template, language) = RunInteractiveWizard(auth, db, arch);
            if (string.IsNullOrEmpty(template))
                return 0;
            outputDir ??= Path.Combine(Directory.GetCurrentDirectory(), name);
        }
        else
        {
            template ??= "hello";
            name ??= "mon-projet";
            outputDir ??= Path.Combine(Directory.GetCurrentDirectory(), name);
        }

        if (!IsValidProjectName(name))
        {
            ConsoleService.Error($"Nom de projet invalide : '{name}'.");
            ConsoleService.Info("Utilise des lettres, chiffres, '-', '_' ou '.' (sans espace ni separateur de chemin).");
            return 2;
        }

        if (!string.IsNullOrWhiteSpace(framework))
            ConfigService.Set(ConfigService.Keys.Framework, framework);

        if (!silent) ConsoleService.WriteLine();

        if (dryRun)
        {
            ShowDryRun(name, template, language, outputDir, auth, db, arch, framework);
            return 0;
        }

        if (Directory.Exists(outputDir) && IsDirectoryNotEmpty(outputDir))
        {
            if (force)
            {
                BackupExisting(outputDir);
            }
            else if (silent)
            {
                ConsoleService.Error($"Le dossier '{outputDir}' n'est pas vide. Utilise --force pour l'ecraser.");
                return 1;
            }
            else if (!ConsoleService.Confirm("[yellow]Le dossier existe déjà. Écraser ?[/]", false))
            {
                ConsoleService.Warning("Opération annulée.");
                return 1;
            }
            else
            {
                BackupExisting(outputDir);
            }
        }

        if (template.Contains('+'))
        {
            return HandleComposite(silent, noGit, name, template, outputDir, language);
        }

        if (template == "hello")
        {
            if (string.IsNullOrWhiteSpace(language) && isInteractive)
            {
                language = PickHelloLanguage();
            }
        }

        return GenerateAndFinish(silent, noGit, name, template, outputDir, language, auth, db, arch, docker, ci);
    }

    private static bool IsValidProjectName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        if (name is "." or "..") return false;
        if (name.Length > 100) return false;
        if (name.Contains(' ') || name.Contains('/') || name.Contains('\\')) return false;
        return name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
    }

    private static bool IsDirectoryNotEmpty(string dir)
    {
        try
        {
            return Directory.EnumerateFileSystemEntries(dir).Any();
        }
        catch (IOException)
        {
            return true;
        }
    }

    private static int ShowTemplateList()
    {
        ConsoleService.MarkupLine("[bold cyan]Templates disponibles[/]");
        ConsoleService.WriteLine();

        ConsoleService.MarkupLine("[bold]Hello World[/]");
        foreach (var lang in HelloLanguages)
            ConsoleService.MarkupLine($"  [green]hello[/]  [gray]({Escape(lang)})[/]");

        ConsoleService.WriteLine();

        foreach (var adapter in Adapters)
        {
            var status = adapter.IsAvailable ? "" : " [red](outil non installe)[/]";
            ConsoleService.MarkupLine($"[bold]{Escape(adapter.Name)}[/]{status} — {Escape(adapter.Description)}");
            foreach (var sub in adapter.SubTemplates)
                ConsoleService.MarkupLine($"  [green]{Escape(adapter.Name)} {Escape(sub)}[/]");
            ConsoleService.WriteLine();
        }

        ConsoleService.Info("Composition : scaffold new --template dotnet+react");
        ConsoleService.Info("Registry    : scaffold registry list");
        return 0;
    }


    private static string PickHelloLanguage()
    {
        return ConsoleService.Select(
            "  [white]\u2714[/] Choisis ton langage :",
            HelloLanguages.Select(l => char.ToUpper(l[0]) + l[1..]).ToArray()
        ).ToLowerInvariant();
    }

    private (string name, string template, string? language) RunInteractiveWizard(
        string? defaultAuth = null, string? defaultDb = null, string? defaultArch = null)
    {
        var totalSteps = 6;
        ConsoleService.MarkupLine("[bold yellow]\u26a1  Assistant de création de projet[/]");
        ConsoleService.MarkupLine("[gray]  Suis les étapes pour créer ton projet.[/]");
        ConsoleService.KeyboardHint();
        ConsoleService.WriteLine();

        ConsoleService.StepHeader(1, totalSteps, "Nom du projet");
        var name = ConsoleService.Prompt(
            "  [white]\u2714[/] Nom du projet :",
            "mon-projet",
            validate: v =>
            {
                if (string.IsNullOrWhiteSpace(v)) return false;
                if (v.Contains(' ')) return false;
                if (v.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return false;
                return true;
            },
            errorMsg: "Nom invalide. Utilise uniquement des lettres, chiffres et tirets."
        );

        ConsoleService.StepHeader(2, totalSteps, "Type de projet");
        ConsoleService.KeyboardHint();
        var category = ConsoleService.Select(
            "  [white]\u2714[/] Choisis un type :",
            ["Application Hello World", "Framework complet", "Stack fullstack"]
        );

        string template;
        string? language = null;

        switch (category)
        {
            case "Application Hello World":
                template = "hello";
                ConsoleService.StepHeader(3, totalSteps, "Langage");
                ConsoleService.KeyboardHint();
                language = PickHelloLanguage();
                break;

            case "Stack fullstack":
                template = "stack";
                ConsoleService.MarkupLine("[yellow]Lancement de l'assistant stack...[/]");
                ConsoleService.MarkupLine("[cyan]Exécute plutôt : [white]scaffold stack --name {name}[/][/]");
                ConsoleService.WriteLine();
                return ("", "", "");

            default:
                template = SelectFrameworkWithVariant();
                if (string.IsNullOrEmpty(template))
                    return ("", "", null);
                break;
        }

        var authChoice = defaultAuth;
        var dbChoice = defaultDb;
        var archChoice = defaultArch;

        if (template != "hello" && template != "stack")
        {
            ConsoleService.StepHeader(4, totalSteps, "Configuration");
            ConsoleService.KeyboardHint();
            authChoice = authChoice ?? ConsoleService.Select(
                "  [white]\u2714[/] Authentification :",
                ["none", "jwt", "identity"]
            );
            dbChoice = dbChoice ?? ConsoleService.Select(
                "  [white]\u2714[/] Base de donnees :",
                ["none", "sqlite", "sqlserver", "postgresql"]
            );
            archChoice = archChoice ?? ConsoleService.Select(
                "  [white]\u2714[/] Architecture :",
                ["mvc", "clean", "minimal"]
            );
        }

        ConsoleService.StepHeader(template == "hello" || template == "stack" ? 3 : 5, totalSteps, "Recapitulatif");
        ConsoleService.WriteLine();
        ConsoleService.SummaryLine("Projet", name);
        ConsoleService.SummaryLine("Template", template);
        if (language != null)
            ConsoleService.SummaryLine("Langage", language);
        if (authChoice != null && authChoice != "none")
            ConsoleService.SummaryLine("Auth", authChoice);
        if (dbChoice != null && dbChoice != "none")
            ConsoleService.SummaryLine("BDD", dbChoice);
        if (archChoice != null && archChoice != "mvc")
            ConsoleService.SummaryLine("Architecture", archChoice);
        ConsoleService.SummaryLine("Dossier", Path.Combine(Directory.GetCurrentDirectory(), name));
        ConsoleService.WriteLine();

        var confirm = ConsoleService.Confirm(
            "[green]  Générer le projet ?[/]", true);

        if (!confirm)
        {
            ConsoleService.Warning("Opération annulée.");
            return ("", "", null);
        }

        ConsoleService.WriteLine();
        return (name, template, language);
    }

    private string SelectFrameworkWithVariant()
    {
        ConsoleService.KeyboardHint();
        var adapterChoice = ConsoleService.Select(
            "  [white]\u2714[/] Choisis un ecosysteme :",
            Adapters.Select(a =>
            {
                var avail = a.IsAvailable ? "" : " (\u274c outil non installe)";
                var subs = a.SubTemplates.Length > 1 ? $" ({a.SubTemplates.Length} variantes)" : "";
                return $"{a.Name} \u2014 {a.Description}{subs}{avail}";
            }).ToArray()
        );

        var adapter = Adapters.First(a =>
            adapterChoice.StartsWith(a.Name + " \u2014"));

        if (!adapter.IsAvailable)
        {
            ConsoleService.Warning($"L'outil requis pour '{adapter.Name}' n'est pas installe.");
            var fallback = ConsoleService.Confirm(
                "[yellow]  Utiliser le template Hello World à la place ?[/]", true);
            if (fallback)
            {
                ConsoleService.StepHeader(3, 4, "Langage");
                ConsoleService.KeyboardHint();
                return PickHelloLanguage();
            }
            ConsoleService.Warning("Opération annulée.");
            return "";
        }

        if (adapter.SubTemplates.Length > 1)
        {
            ConsoleService.StepHeader(3, 4, "Variante");
            ConsoleService.KeyboardHint();
            return ConsoleService.Select(
                "  [white]\u2714[/] Choisis une variante :",
                adapter.SubTemplates
            );
        }

        return adapter.SubTemplates.FirstOrDefault() ?? adapter.Name;
    }

    private int HandleComposite(bool silent, bool noGit, string name, string template, string outputDir, string? language)
    {
        var parts = template.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        ConsoleService.Info($"Composition de {parts.Length} templates : {string.Join(", ", parts)}");
        ConsoleService.WriteLine();

        var allOk = true;
        var used = new List<string>();

        foreach (var part in parts)
        {
            var subDir = Path.Combine(outputDir, part);
            var (code, _, usedTmpl) = GenerateProjectCore(part, part, subDir, language);
            if (code != 0) allOk = false;
            if (usedTmpl != null) used.Add(usedTmpl);
        }

        if (allOk)
        {
            if (!noGit) GitService.InitWithSpinner(outputDir);
            ConsoleService.Success($"Projet composé '{name}' créé avec succès !");
            ConsoleService.Info($"  {outputDir}");
            ConsoleService.Info($"  Templates : {string.Join(", ", used)}");
        }

        return allOk ? 0 : 1;
    }

    private int GenerateAndFinish(
        bool silent, bool noGit, string name, string template, string outputDir, string? language,
        string? auth = null, string? db = null, string? arch = null, bool docker = false, bool ci = false)
    {
        var success = true;
        string? usedTemplate = null;

        ConsoleService.ShowSpinner(
            "  Generation du projet...",
            () =>
            {
                var result = GenerateProjectCore(name, template, outputDir, language);
                success = result.ExitCode == 0;
                usedTemplate = result.UsedTemplate;
                return Task.CompletedTask;
            }).GetAwaiter().GetResult();

        ConsoleService.WriteLine();

        if (success)
        {
            var effectiveTemplate = usedTemplate ?? template;

            ApplyProjectOptions(effectiveTemplate, outputDir, name, auth, db, arch, docker, ci);
            RunPostGenHooks(effectiveTemplate, outputDir, silent);

            if (!noGit)
                GitService.InitWithSpinner(outputDir);

            if (!silent)
            {
                ConsoleService.WriteLine();
                ConsoleService.MarkupLine($"[bold green]\u2728  Projet '{Escape(name)}' créé avec succès ![/]");
                ConsoleService.WriteLine();
                ConsoleService.MarkupLine("[gray]  Pour commencer :[/]");
                ConsoleService.MarkupLine($"    [green]cd[/] [cyan]{Escape(name)}[/]");

                var runCmd = GetRunCommand(effectiveTemplate);
                if (runCmd != null)
                    ConsoleService.MarkupLine($"    [green]{Escape(runCmd)}[/]");

                ConsoleService.WriteLine();
            }
        }
        else if (!silent)
        {
            ConsoleService.Error($"Échec de la création du projet '{name}'.");
        }

        return success ? 0 : 1;
    }

    /// <summary>Applique les options --auth/--db/--arch/--docker/--ci apres generation.</summary>
    private static void ApplyProjectOptions(
        string template, string outputDir, string name,
        string? auth, string? db, string? arch, bool docker, bool ci)
    {
        var isDotnet = Adapters.OfType<DotnetAdapter>().Any(a =>
            a.Name == template || a.SubTemplates.Contains(template));

        if (isDotnet)
            DotnetProjectOptions.Apply(outputDir, name, auth, db, arch, docker);

        if (ci)
            CiGenerator.WriteForProject(outputDir, LanguageDetector.Detect(outputDir));
    }

    private void ShowDryRun(
        string name, string template, string? language, string outputDir,
        string? auth = null, string? db = null, string? arch = null, string? framework = null)
    {
        ConsoleService.Info("PREVISUALISATION (--dry-run) :");
        ConsoleService.SummaryLine("Template", template);
        ConsoleService.SummaryLine("Nom", name);
        ConsoleService.SummaryLine("Langage", language ?? "auto");
        ConsoleService.SummaryLine("Dossier", outputDir);
        if (!string.IsNullOrWhiteSpace(framework)) ConsoleService.SummaryLine("Framework", framework);
        if (!string.IsNullOrWhiteSpace(auth) && auth != "none") ConsoleService.SummaryLine("Auth", auth);
        if (!string.IsNullOrWhiteSpace(db) && db != "none") ConsoleService.SummaryLine("BDD", db);
        if (!string.IsNullOrWhiteSpace(arch) && arch != "mvc") ConsoleService.SummaryLine("Architecture", arch);
        ConsoleService.Info("Aucun fichier genere. Retire --dry-run pour generer.");
    }

    private static string? GetFavTemplate()
    {
        return ConfigService.Get(ConfigService.Keys.Fav);
    }

    public static (int ExitCode, string Message, string? UsedTemplate) GenerateProjectStatic(
        string name, string template, string outputDir, string? language)
    {
        return GenerateProjectCore(name, template, outputDir, language);
    }

    private static (int ExitCode, string Message, string? UsedTemplate) GenerateProjectCore(
        string name, string template, string outputDir, string? language)
    {
        // Les outils natifs (dotnet new, npm create, cargo new, flutter create...)
        // creent eux-memes leur dossier : on ne le pre-cree pas.
        // Seuls les generateurs internes (hello, registry) ont besoin du dossier.
        if (template == "hello")
        {
            Directory.CreateDirectory(outputDir);
            GenerateHelloWorld(name, outputDir, language);
            return (0, "", "hello");
        }

        var adapter = Adapters.FirstOrDefault(a =>
            a.Name == template ||
            a.SubTemplates.Contains(template));

        if (adapter != null)
        {
            if (!adapter.IsAvailable)
            {
                ConsoleService.Warning($"L'outil requis pour '{adapter.Name}' n'est pas installe.");
                ConsoleService.Info("Utilisation du template Hello World a la place.");
                Directory.CreateDirectory(outputDir);
                GenerateHelloWorld(name, outputDir, language);
                return (42, "Fallback vers hello (outil requis non installe)", "hello");
            }
            var subTemplate = template == adapter.Name ? "" : template;
            var result = adapter.ScaffoldAsync(name, outputDir, subTemplate, language).GetAwaiter().GetResult();
            return (result.ExitCode, result.Message, adapter.Name);
        }

        Directory.CreateDirectory(outputDir);
        if (TryGenerateFromRegistry(template, outputDir, name))
            return (0, "", template);

        ConsoleService.Error($"Template '{template}' inconnu.");
        ConsoleService.Info("Templates disponibles : hello, dotnet, npm, cargo, go, python, flutter");
        return (1, "Template inconnu", null);
    }

    private static bool TryGenerateFromRegistry(string template, string outputDir, string name)
    {
        var registryDir = Paths.GetRegistryTemplateDir(template);

        if (!Directory.Exists(registryDir))
            return false;

        ConsoleService.Info($"Génération depuis le template registry '{template}'...");
        CopyDirectory(registryDir, outputDir);

        var readmePath = Path.Combine(outputDir, "README.md");
        if (File.Exists(readmePath))
        {
            var content = File.ReadAllText(readmePath);
            content = content.Replace("{{ProjectName}}", name).Replace("{{project-name}}", name);
            File.WriteAllText(readmePath, content);
        }

        return true;
    }

    private static void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            if (file.Contains(".git") || file.EndsWith("metadata.json"))
                continue;
            var rel = Path.GetRelativePath(source, file);
            var target = Path.Combine(dest, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }

    private static int GenerateHelloWorld(string name, string outputDir, string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            language = ConsoleService.Select(
                "  Choisis ton langage :",
                HelloLanguages.Select(l => char.ToUpper(l[0]) + l[1..]).ToArray()
            );
        }

        language = language.ToLowerInvariant();
        WriteHelloFiles(name, outputDir, language);
        return 0;
    }

    private static void WriteHelloFiles(string name, string outputDir, string lang)
    {
        switch (lang)
        {
            case "python":
                WriteFile(outputDir, "main.py", "print(\"Hello, {0}!\")\n", name);
                WriteReadme(outputDir, name, "Python", "python main.py");
                break;
            case "javascript":
                WriteFile(outputDir, "index.js", "console.log(\"Hello, {0}!\");\n", name);
                WriteFile(outputDir, "package.json",
                    "{{\"name\": \"{0}\",\"version\": \"1.0.0\",\"private\": true,\"scripts\": {{\"start\": \"node index.js\"}}}}\n", name);
                WriteReadme(outputDir, name, "JavaScript", "npm start");
                break;
            case "typescript":
                WriteFile(outputDir, "index.ts", "console.log(\"Hello, {0}!\");\n", name);
                WriteFile(outputDir, "package.json",
                    "{{\"name\": \"{0}\",\"version\": \"1.0.0\",\"private\": true,\"scripts\": {{\"start\": \"npx tsx index.ts\"}},\"devDependencies\": {{\"tsx\": \"^4.0.0\",\"typescript\": \"^5.0.0\"}}}}\n", name);
                WriteFile(outputDir, "tsconfig.json",
                    "{{\"compilerOptions\": {{\"target\": \"ES2022\",\"module\": \"ESNext\",\"strict\": true}}}}\n");
                WriteReadme(outputDir, name, "TypeScript", "npm start");
                break;
            case "c#":
                WriteFile(outputDir, "Program.cs", "Console.WriteLine(\"Hello, {0}!\");\n", name);
                WriteFile(outputDir, $"{name}.csproj",
                    "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup></Project>\n");
                WriteReadme(outputDir, name, "C#", "dotnet run");
                break;
            case "rust":
                Directory.CreateDirectory(Path.Combine(outputDir, "src"));
                WriteFile(Path.Combine(outputDir, "src"), "main.rs",
                    "fn main() {{\n    println!(\"Hello, {0}!\");\n}}\n", name);
                WriteFile(outputDir, "Cargo.toml",
                    "[package]\nname = \"{0}\"\nversion = \"0.1.0\"\nedition = \"2021\"\n", name);
                WriteReadme(outputDir, name, "Rust", "cargo run");
                break;
            case "go":
                WriteFile(outputDir, "main.go",
                    "package main\n\nimport \"fmt\"\n\nfunc main() {{\n    fmt.Println(\"Hello, {0}!\")\n}}\n", name);
                WriteReadme(outputDir, name, "Go", "go run main.go");
                break;
            case "ruby":
                WriteFile(outputDir, "main.rb", "puts \"Hello, {0}!\"\n", name);
                WriteReadme(outputDir, name, "Ruby", "ruby main.rb");
                break;
            case "php":
                WriteFile(outputDir, "index.php", "<?php\necho \"Hello, {0}!\\n\";\n", name);
                WriteReadme(outputDir, name, "PHP", "php index.php");
                break;
            case "dart":
                WriteFile(outputDir, "main.dart", "void main() {{\n  print('Hello, {0}!');\n}}\n", name);
                WriteReadme(outputDir, name, "Dart", "dart run main.dart");
                break;
            case "swift":
                WriteFile(outputDir, "main.swift", "print(\"Hello, {0}!\")\n", name);
                WriteReadme(outputDir, name, "Swift", "swift main.swift");
                break;
            case "kotlin":
                WriteFile(outputDir, "Main.kt", "fun main() {{\n    println(\"Hello, {0}!\")\n}}\n", name);
                WriteReadme(outputDir, name, "Kotlin", "kotlin Main.kt");
                break;
            case "java":
                Directory.CreateDirectory(Path.Combine(outputDir, "src"));
                WriteFile(Path.Combine(outputDir, "src"), "Main.java",
                    "public class Main {{\n    public static void main(String[] args) {{\n        System.out.println(\"Hello, {0}!\");\n    }}\n}}\n", name);
                WriteReadme(outputDir, name, "Java", "cd src && javac Main.java && java Main");
                break;
            case "zig":
                WriteFile(outputDir, "main.zig",
                    "const std = @import(\"std\");\n\npub fn main() !void {{\n    std.debug.print(\"Hello, {0}!\\n\", .{{}});\n}}\n", name);
                WriteReadme(outputDir, name, "Zig", "zig run main.zig");
                break;
            case "scala":
                Directory.CreateDirectory(Path.Combine(outputDir, "src", "main", "scala"));
                WriteFile(Path.Combine(outputDir, "src", "main", "scala"), "Main.scala",
                    "@main def hello() = println(\"Hello, {0}!\")\n", name);
                WriteFile(outputDir, "build.sbt",
                    "name := \"{0}\"\nversion := \"0.1.0\"\nscalaVersion := \"3.4.0\"\n", name);
                WriteReadme(outputDir, name, "Scala", "sbt run");
                break;
            case "elixir":
                WriteFile(outputDir, "main.exs",
                    "IO.puts(\"Hello, {0}!\")\n", name);
                var mixExs = $"defmodule {name}.MixProject do\n  use Mix.Project\n\n" +
                    "  def project do\n" +
                    $"    [app: :{name.ToLowerInvariant().Replace("-", "_")}, version: \"0.1.0\", elixir: \"~> 1.16\"]\n" +
                    "  end\n\n  def application do\n" +
                    "    [extra_applications: [:logger]]\n  end\nend\n";
                File.WriteAllText(Path.Combine(outputDir, "mix.exs"), mixExs);
                WriteReadme(outputDir, name, "Elixir", "elixir main.exs");
                break;
            case "crystal":
                Directory.CreateDirectory(Path.Combine(outputDir, "src"));
                WriteFile(Path.Combine(outputDir, "src"), $"{name.Replace("-", "_")}.cr",
                    "puts \"Hello, {0}!\"\n", name);
                WriteFile(outputDir, "shard.yml",
                    "name: {0}\nversion: 0.1.0\n", name);
                WriteReadme(outputDir, name, "Crystal", $"crystal run src/{name.Replace("-", "_")}.cr");
                break;
            default:
                ConsoleService.Error($"Langage '{lang}' non supporté.");
                ConsoleService.Info($"Langages disponibles : {string.Join(", ", HelloLanguages)}");
                return;
        }

        WriteFile(outputDir, ".gitignore",
            "# Scaffolder generated\nbin/\nobj/\nnode_modules/\n__pycache__/\n.DS_Store\n");
    }

    private static void WriteFile(string dir, string file, string content, string? name = null)
    {
        var text = name != null
            ? string.Format(content, name)
            : content;
        File.WriteAllText(Path.Combine(dir, file), text);
    }

    private static void WriteReadme(string dir, string projectName, string lang, string runCmd)
    {
        var content = $"# {projectName}\n\n"
            + $"Hello World project in {lang}.\n\n"
            + $"## Usage\n\n```bash\n{runCmd}\n```\n";
        File.WriteAllText(Path.Combine(dir, "README.md"), content);
    }

    private static void RunPostGenHooks(string template, string outputDir, bool silent = false)
    {
        var npmTemplates = new[] { "npm", "vite", "next", "react", "vue", "nuxt", "svelte", "solid" };
        if (npmTemplates.Contains(template))
        {
            if (silent) return;
            ConsoleService.ShowSpinner(
                "  Installation des dependances npm...",
                async () =>
                {
                    var npmResult = await ProcessService.RunAsync(
                        "npm", "install",
                        workingDirectory: outputDir,
                        streamOutput: false
                    );
                    if (npmResult.ExitCode == 0)
                        ConsoleService.Success("Dependances installees avec succes.");
                    else
                        ConsoleService.Warning("npm install a echoue. Lance 'npm install' manuellement.");
                }).GetAwaiter().GetResult();
        }

        if (template == "cargo" || template == "rust" || template.StartsWith("cargo-"))
        {
            if (silent) return;
            ConsoleService.ShowSpinner(
                "  Verification du projet Rust...",
                async () =>
                {
                    var cargoResult = await ProcessService.RunAsync(
                        "cargo", "check",
                        workingDirectory: outputDir,
                        streamOutput: false
                    );
                    if (cargoResult.ExitCode == 0)
                        ConsoleService.Success("Projet Rust verifie avec succes.");
                    else
                        ConsoleService.Warning("cargo check a echoue. Verifie le projet manuellement.");
                }).GetAwaiter().GetResult();
        }

        if (template == "go" || template == "golang")
        {
            if (silent) return;
            ConsoleService.ShowSpinner(
                "  Telechargement des dependances Go...",
                async () =>
                {
                    var goResult = await ProcessService.RunAsync(
                        "go", "mod tidy",
                        workingDirectory: outputDir,
                        streamOutput: false
                    );
                    if (goResult.ExitCode == 0)
                        ConsoleService.Success("Dependances Go installees.");
                }).GetAwaiter().GetResult();
        }
    }

    private static void BackupExisting(string outputDir)
    {
        var backupDir = outputDir.TrimEnd('/') + ".backup-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
        ConsoleService.Debug($"Sauvegarde du dossier existant vers {backupDir}");
        Directory.Move(outputDir, backupDir);
        ConsoleService.Warning($"Dossier existant sauvegarde : {backupDir}");
    }

    private static string Escape(string text)
    {
        return text?.Replace("[", "[[").Replace("]", "]]") ?? "";
    }

    private static string? GetRunCommand(string template)
    {
        var t = template.ToLowerInvariant();
        return t switch
        {
            "dotnet" or "webapi" or "blazor" or "console" => "dotnet run",
            "npm" or "vite" or "react" or "vue" or "svelte" or "solid" or "next" or "nuxt" => "npm run dev",
            "express" or "nest" => "npm run dev",
            "cargo" or "rust" or "actix" or "rocket" => "cargo run",
            "go" or "golang" => "go run .",
            "python" or "fastapi" => "uvicorn main:app --reload",
            "django" => "python manage.py runserver",
            "flutter" => "flutter run",
            "laravel" => "php artisan serve",
            "rails" => "rails server",
            "mix" or "phoenix" or "phx" => "mix phx.server",
            "zig" => "zig run src/main.zig",
            _ => null
        };
    }
}
