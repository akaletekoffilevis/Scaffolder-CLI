using System.CommandLine;
using System.Diagnostics;
using System.Threading.Tasks;
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
        new CabalAdapter()
    ];

    private static readonly string[] HelloLanguages =
    [
        "python", "javascript", "typescript", "c#", "rust", "go",
        "ruby", "php", "dart", "swift", "kotlin", "java", "zig"
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
        Description = "Previsualisation sans generer"
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
        Description = "Mode verbeux (logs detailles)"
    };

    private readonly Option<bool> _favOpt = new("--fav")
    {
        Description = "Utilise le template favori (configure avec scaffold config set fav <template>)"
    };

    public NewCommand() : base("new", "Crée un nouveau projet")
    {
        Add(_nameOpt);
        Add(_templateOpt);
        Add(_languageOpt);
        Add(_outputOpt);
        Add(_dryRunOpt);
        Add(_silentOpt);
        Add(_noGitOpt);
        Add(_verboseOpt);
        Add(_favOpt);

        this.SetAction((ParseResult pr) => HandleNew(pr));
    }

    private int HandleNew(ParseResult pr)
    {
        var silent = pr.GetValue(_silentOpt);
        var dryRun = pr.GetValue(_dryRunOpt);
        var noGit = pr.GetValue(_noGitOpt);
        var verbose = pr.GetValue(_verboseOpt);
        var useFav = pr.GetValue(_favOpt);

        ConsoleService.Verbose = verbose;
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
            (name, template, language) = RunInteractiveWizard();
            outputDir ??= Path.Combine(Directory.GetCurrentDirectory(), name);
        }
        else
        {
            template ??= "hello";
            name ??= "mon-projet";
            outputDir ??= Path.Combine(Directory.GetCurrentDirectory(), name);
        }

        if (!silent) ConsoleService.WriteLine();

        if (dryRun)
        {
            ShowDryRun(name, template, language, outputDir);
            return 0;
        }

        if (Directory.Exists(outputDir) && Directory.GetFiles(outputDir).Length > 0)
        {
            var overwrite = silent || ConsoleService.Confirm(
                "[yellow]Le dossier existe deja. Ecraser ?[/]", false);
            if (!overwrite)
            {
                ConsoleService.Warning("Operation annulee.");
                return 1;
            }
            BackupExisting(outputDir);
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

        return GenerateAndFinish(silent, noGit, name, template, outputDir, language);
    }

    private static string PickHelloLanguage()
    {
        return ConsoleService.Select(
            "  [white]\u2714[/] Choisis ton langage :",
            HelloLanguages.Select(l => char.ToUpper(l[0]) + l[1..]).ToArray()
        ).ToLowerInvariant();
    }

    private (string name, string template, string? language) RunInteractiveWizard()
    {
        ConsoleService.MarkupLine("[bold yellow]\u26a1  Assistant de creation de projet[/]");
        ConsoleService.MarkupLine("[gray]  Suis les etapes pour creer ton projet.[/]");
        ConsoleService.KeyboardHint();
        ConsoleService.WriteLine();

        ConsoleService.StepHeader(1, 4, "Nom du projet");
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

        ConsoleService.StepHeader(2, 4, "Type de projet");
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
                ConsoleService.StepHeader(3, 4, "Langage");
                ConsoleService.KeyboardHint();
                language = PickHelloLanguage();
                break;

            case "Stack fullstack":
                template = "stack";
                ConsoleService.MarkupLine("[yellow]Lancement de l'assistant stack...[/]");
                ConsoleService.MarkupLine("[cyan]Execute plutot : [white]scaffold stack --name {name}[/][/]");
                ConsoleService.WriteLine();
                return ("", "", "");

            default:
                template = SelectFrameworkWithVariant();
                break;
        }

        ConsoleService.StepHeader(3, 4, "Recapitulatif");
        ConsoleService.WriteLine();
        ConsoleService.SummaryLine("Projet", name);
        ConsoleService.SummaryLine("Template", template);
        if (language != null)
            ConsoleService.SummaryLine("Langage", language);
        ConsoleService.SummaryLine("Dossier", Path.Combine(Directory.GetCurrentDirectory(), name));
        ConsoleService.WriteLine();

        var confirm = ConsoleService.Confirm(
            "[green]  Generer le projet ?[/]", true);

        if (!confirm)
        {
            ConsoleService.Warning("Operation annulee.");
            Environment.Exit(0);
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
                "[yellow]  Utiliser le template Hello World a la place ?[/]", true);
            if (fallback)
            {
                ConsoleService.StepHeader(3, 4, "Langage");
                ConsoleService.KeyboardHint();
                return PickHelloLanguage();
            }
            Environment.Exit(1);
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
            if (!noGit) GitInit(outputDir);
            ConsoleService.Success($"Projet compose '{name}' cree avec succes !");
            ConsoleService.Info($"  {outputDir}");
            ConsoleService.Info($"  Templates : {string.Join(", ", used)}");
        }

        return allOk ? 0 : 1;
    }

    private int GenerateAndFinish(bool silent, bool noGit, string name, string template, string outputDir, string? language)
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
            RunPostGenHooks(usedTemplate ?? template, outputDir, silent);

            if (!noGit)
                GitInit(outputDir);

            if (!silent)
            {
                ConsoleService.WriteLine();
                ConsoleService.MarkupLine($"[bold green]\u2728  Projet '{Escape(name)}' cree avec succes ![/]");
                ConsoleService.WriteLine();
                ConsoleService.MarkupLine("[gray]  Pour commencer :[/]");
                ConsoleService.MarkupLine($"    [cyan]cd {Escape(name)}[/]");
                ConsoleService.MarkupLine("    [cyan]dotnet run[/]  (ou la commande indiquee dans le README)");
                ConsoleService.WriteLine();
            }
        }
        else if (!silent)
        {
            ConsoleService.Error($"Echec de la creation du projet '{name}'.");
        }

        return success ? 0 : 1;
    }

    private void ShowDryRun(string name, string template, string? language, string outputDir)
    {
        ConsoleService.Info("PREVISUALISATION (--dry-run) :");
        ConsoleService.SummaryLine("Template", template);
        ConsoleService.SummaryLine("Nom", name);
        ConsoleService.SummaryLine("Langage", language ?? "auto");
        ConsoleService.SummaryLine("Dossier", outputDir);
        ConsoleService.Info("Aucun fichier genere. Retire --dry-run pour generer.");
    }

    private static string? GetFavTemplate()
    {
        var configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".scaffolder");
        var configFile = Path.Combine(configDir, "config.json");
        if (File.Exists(configFile))
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(
                    File.ReadAllText(configFile));
                if (json != null && json.TryGetValue("fav", out var fav))
                    return fav.GetString();
            }
            catch { }
        }
        return null;
    }

    public static (int ExitCode, string Message, string? UsedTemplate) GenerateProjectStatic(
        string name, string template, string outputDir, string? language)
    {
        return GenerateProjectCore(name, template, outputDir, language);
    }

    private static (int ExitCode, string Message, string? UsedTemplate) GenerateProjectCore(
        string name, string template, string outputDir, string? language)
    {
        Directory.CreateDirectory(outputDir);

        if (template == "hello")
        {
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
                GenerateHelloWorld(name, outputDir, language);
                return (42, "Fallback vers hello (outil requis non installe)", "hello");
            }
            var result = adapter.ScaffoldAsync(name, outputDir, template, language).GetAwaiter().GetResult();
            return (result.ExitCode, result.Message, adapter.Name);
        }

        if (TryGenerateFromRegistry(template, outputDir, name))
            return (0, "", template);

        ConsoleService.Error($"Template '{template}' inconnu.");
        ConsoleService.Info("Templates disponibles : hello, dotnet, npm, cargo, go, python, flutter");
        return (1, "Template inconnu", null);
    }

    private static bool TryGenerateFromRegistry(string template, string outputDir, string name)
    {
        var registryDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".scaffolder", "registry", template);

        if (!Directory.Exists(registryDir))
            return false;

        ConsoleService.Info($"Generation depuis le template registry '{template}'...");
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
                    "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net9.0</TargetFramework></PropertyGroup></Project>\n");
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
            default:
                ConsoleService.Error($"Langage '{lang}' non supporte.");
                ConsoleService.Info($"Langages disponibles : {string.Join(", ", HelloLanguages)}");
                Environment.Exit(1);
                break;
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

    private static bool IsPortAvailable(int port)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "ss",
                Arguments = $"-tlnp sport = :{port}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = new Process { StartInfo = psi };
            proc.Start();
            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(2000);
            return !output.Contains($"sport = :{port}");
        }
        catch
        {
            return true;
        }
    }

    private static void GitInit(string outputDir)
    {
        ConsoleService.ShowSpinner(
            "  Initialisation du depot Git...",
            async () =>
            {
                var initResult = await ProcessService.RunAsync(
                    "git", "init",
                    workingDirectory: outputDir,
                    streamOutput: false
                );

                if (initResult.ExitCode == 0)
                {
                    await ProcessService.RunAsync(
                        "git", "add .",
                        workingDirectory: outputDir,
                        streamOutput: false
                    );

                    var commitResult = await ProcessService.RunAsync(
                        "git", "commit -m \"Initial commit with Scaffolder\" --allow-empty",
                        workingDirectory: outputDir,
                        streamOutput: false
                    );

                    if (commitResult.ExitCode == 0)
                        ConsoleService.Success("Depot Git initialise avec le premier commit.");
                    else
                        ConsoleService.Warning("Git commit a echoue. Verifie ta config Git (user.name, user.email).");
                }
                else
                {
                    ConsoleService.Warning("Git n'est pas installe ou a echoue.");
                }
            }).GetAwaiter().GetResult();
    }

    private static string Escape(string text)
    {
        return text?.Replace("[", "[[").Replace("]", "]]") ?? "";
    }
}
