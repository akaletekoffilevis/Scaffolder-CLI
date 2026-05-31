using System.CommandLine;
using System.Diagnostics;
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

    private readonly Option<string> _favOpt = new("--fav")
    {
        Description = "Utilise un template favori (configure avec scaffold config set fav <template>)"
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

        SetAction(HandleNew);
    }

    private int HandleNew(ParseResult pr)
    {
        var silent = pr.GetValue(_silentOpt);
        var dryRun = pr.GetValue(_dryRunOpt);
        var noGit = pr.GetValue(_noGitOpt);
        var verbose = pr.GetValue(_verboseOpt);
        var fav = pr.GetValue(_favOpt);

        ConsoleService.Verbose = verbose;

        var isInteractive = string.IsNullOrWhiteSpace(pr.GetValue(_templateOpt))
                            && string.IsNullOrWhiteSpace(fav);

        if (!silent && !isInteractive)
        {
            ConsoleService.ShowLogo();
            Console.WriteLine();
        }

        var template = pr.GetValue(_templateOpt);
        if (!string.IsNullOrWhiteSpace(fav))
        {
            template = fav;
            ConsoleService.Debug($"Template favori : {fav}");
        }

        if (string.IsNullOrWhiteSpace(template))
        {
            template = ConsoleService.Select(
                "  Choisis un template :",
                GetTemplateOptions()
            );
            template = template.Split(" \u2014 ")[0];
        }

        var name = pr.GetValue(_nameOpt);
        if (string.IsNullOrWhiteSpace(name))
        {
            name = ConsoleService.Prompt("  Nom du projet :", "mon-projet");
        }

        var outputDir = pr.GetValue(_outputOpt)?.FullName
            ?? Path.Combine(Directory.GetCurrentDirectory(), name);

        var language = pr.GetValue(_languageOpt);

        if (!silent)
            Console.WriteLine();

        if (dryRun)
        {
            ConsoleService.Info("PREVISUALISATION (--dry-run) :");
            ConsoleService.Info($"  Template : {template}");
            ConsoleService.Info($"  Nom : {name}");
            ConsoleService.Info($"  Langage : {language ?? "auto"}");
            ConsoleService.Info($"  Dossier : {outputDir}");
            ConsoleService.Info("  Aucun fichier genere. Passe --dry-run pour generer.");
            return 0;
        }

        if (Directory.Exists(outputDir) && Directory.GetFiles(outputDir).Length > 0)
        {
            BackupExisting(outputDir);
        }

        ConsoleService.Debug($"Generation : template={template}, name={name}, output={outputDir}, lang={language ?? "auto"}");

        var (exitCode, _, usedTemplate) = GenerateProject(name, template, outputDir, language);

        if (!silent)
            Console.WriteLine();

        if (exitCode == 0)
        {
            RunPostGenHooks(usedTemplate ?? template, outputDir);

            if (!noGit)
                GitInit(outputDir);

            if (!silent)
            {
                ConsoleService.Success($"Projet '{name}' cree avec succes !");
                ConsoleService.Info($"  {outputDir}");
                ConsoleService.Info($"  cd {outputDir} && regarde le README.md pour commencer");
            }
        }
        else if (!silent)
        {
            ConsoleService.Error($"Echec de la creation du projet '{name}'.");
        }

        return exitCode;
    }

    private static string[] GetTemplateOptions()
    {
        var options = new List<string>
        {
            "hello \u2014 Application Hello World minimaliste"
        };

        foreach (var adapter in Adapters)
        {
            var avail = adapter.IsAvailable ? "" : " (outil non installe)";
            options.Add($"{adapter.Name} \u2014 {adapter.Description}{avail}");
        }

        // Ajouter les templates du registry
        var registryDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".scaffolder", "registry");
        if (Directory.Exists(registryDir))
        {
            foreach (var tmplDir in Directory.GetDirectories(registryDir))
            {
                var tmplName = Path.GetFileName(tmplDir);
                if (options.Any(o => o.StartsWith(tmplName + " \u2014"))) continue;
                var desc = "Template personnalise";
                var metaPath = Path.Combine(tmplDir, "metadata.json");
                if (File.Exists(metaPath))
                {
                    try
                    {
                        var meta = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                            File.ReadAllText(metaPath), JsonContext.Default.DictionaryStringObject);
                        if (meta != null && meta.TryGetValue("description", out var d))
                            desc = d?.ToString() ?? desc;
                    }
                    catch { }
                }
                options.Add($"{tmplName} \u2014 {desc}");
            }
        }

        return [.. options];
    }

    public static (int ExitCode, string Message, string? UsedTemplate) GenerateProjectStatic(
        string name, string template, string outputDir, string? language)
        => GenerateProject(name, template, outputDir, language);

    private static (int ExitCode, string Message, string? UsedTemplate) GenerateProject(
        string name, string template, string outputDir, string? language)
    {
        Directory.CreateDirectory(outputDir);

        // Template composition: "webapi+react" -> generate both
        if (template.Contains('+'))
        {
            var parts = template.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            ConsoleService.Info($"Composition de {parts.Length} templates : {string.Join(", ", parts)}");
            Console.WriteLine();
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
                ConsoleService.Success($"Projet compose '{name}' cree avec succes !");
                ConsoleService.Info($"  {outputDir}");
                ConsoleService.Info($"  Templates : {string.Join(", ", used)}");
            }
            return (allOk ? 0 : 1, "", string.Join("+", used));
        }

        return GenerateProjectCore(name, template, outputDir, language);
    }

    private static (int ExitCode, string Message, string? UsedTemplate) GenerateProjectCore(
        string name, string template, string outputDir, string? language)
    {

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
                ConsoleService.Info("Utilisation du template hello a la place.");
                return (GenerateHelloWorld(name, outputDir, language), "Fallback vers hello", "hello");
            }
            var result = adapter.ScaffoldAsync(name, outputDir, template, language).GetAwaiter().GetResult();
            return (result.ExitCode, result.Message, adapter.Name);
        }

        // Try registry templates
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

        // Generate project name in files if applicable
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

    private static void RunPostGenHooks(string template, string outputDir)
    {
        var npmTemplates = new[] { "npm", "vite", "next", "react", "vue", "nuxt", "svelte", "solid" };
        if (npmTemplates.Contains(template))
        {
            ConsoleService.Info("Installation des dependances npm...");
            var npmResult = ProcessService.RunAsync(
                "npm", "install",
                workingDirectory: outputDir,
                streamOutput: false
            ).GetAwaiter().GetResult();

            if (npmResult.ExitCode == 0)
                ConsoleService.Success("Dependances installees avec succes.");
            else
                ConsoleService.Warning("npm install a echoue. Lance 'npm install' manuellement.");
        }

        if (template == "cargo" || template == "rust" || template.StartsWith("cargo-"))
        {
            ConsoleService.Info("Verification du projet Rust...");
            var cargoResult = ProcessService.RunAsync(
                "cargo", "check",
                workingDirectory: outputDir,
                streamOutput: false
            ).GetAwaiter().GetResult();

            if (cargoResult.ExitCode == 0)
                ConsoleService.Success("Projet Rust verifie avec succes.");
            else
                ConsoleService.Warning("cargo check a echoue. Verifie le projet manuellement.");
        }

        if (template == "go" || template == "golang")
        {
            ConsoleService.Info("Telechargement des dependances Go...");
            var goResult = ProcessService.RunAsync(
                "go", "mod tidy",
                workingDirectory: outputDir,
                streamOutput: false
            ).GetAwaiter().GetResult();

            if (goResult.ExitCode == 0)
                ConsoleService.Success("Dependances Go installees.");
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
            return true; // si on peut pas verifier, on laisse faire
        }
    }

    private static void GitInit(string outputDir)
    {
        ConsoleService.Info("Initialisation du depot Git...");

        var initResult = ProcessService.RunAsync(
            "git", "init",
            workingDirectory: outputDir,
            streamOutput: false
        ).GetAwaiter().GetResult();

        if (initResult.ExitCode == 0)
        {
            ProcessService.RunAsync(
                "git", "add .",
                workingDirectory: outputDir,
                streamOutput: false
            ).GetAwaiter().GetResult();

            var commitResult = ProcessService.RunAsync(
                "git", "commit -m \"Initial commit with Scaffolder\" --allow-empty",
                workingDirectory: outputDir,
                streamOutput: false
            ).GetAwaiter().GetResult();

            if (commitResult.ExitCode == 0)
                ConsoleService.Success("Depot Git initialise avec le premier commit.");
            else
                ConsoleService.Warning("Git commit a echoue. Verifie ta config Git (user.name, user.email).");
        }
        else
        {
            ConsoleService.Warning("Git n'est pas installe ou a echoue. Ignore l'initialisation Git.");
        }
    }
}
