using System.CommandLine;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Scaffolder.Adapters;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class DoctorCommand : Command
{
    private readonly Option<bool> _jsonOpt = new("--json", ["-j"])
    {
        Description = "Sortie JSON"
    };

    private static readonly (string Name, string[] Args, bool Required)[] Tools =
    [
        ("dotnet", ["--version"], false),
        ("node", ["--version"], false),
        ("npm", ["--version"], false),
        ("cargo", ["--version"], false),
        ("go", ["version"], false),
        ("flutter", ["--version"], false),
        ("python3", ["--version"], false),
        ("git", ["--version"], true),
        ("docker", ["--version"], false),
        ("gh", ["--version"], false),
    ];

    public DoctorCommand() : base("doctor", "Diagnostique l'environnement et la configuration")
    {
        Add(_jsonOpt);
        SetAction(HandleDoctor);
    }

    private int HandleDoctor(ParseResult pr)
    {
        var json = pr.GetValue(_jsonOpt);

        var results = new List<(string Name, bool Found, string Version, bool Required)>();

        foreach (var (name, args, required) in Tools)
        {
            var found = ProcessService.CommandExists(name);
            var version = "";

            if (found)
            {
                try
                {
                    var result = ProcessService.RunResultAsync(
                        name, args, timeout: TimeSpan.FromSeconds(15)).GetAwaiter().GetResult();
                    var output = result.Output.Split('\n').FirstOrDefault(l => !string.IsNullOrWhiteSpace(l)) ?? "";
                    version = output.Trim();
                }
                catch
                {
                    version = "(version inconnue)";
                }
            }

            results.Add((name, found, version, required));
        }

        if (json)
            return PrintJson(results);

        ConsoleService.ShowLogo();
        ConsoleService.WriteLine();
        ConsoleService.Info("🔍 Diagnostic de l'environnement :");
        ConsoleService.WriteLine();

        ConsoleService.ShowSection("Systeme");
        ConsoleService.ShowKeyValue("OS", GetOS());
        ConsoleService.ShowKeyValue("Architecture", GetArch());
        ConsoleService.ShowKeyValue("Shell", GetShell());
        ConsoleService.ShowKeyValue("Dossier courant", Directory.GetCurrentDirectory());
        ConsoleService.ShowKeyValue(".NET runtime", System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);

        ConsoleService.ShowSection("Configuration");
        CheckConfig();

        ConsoleService.ShowSection("Base de donnees");
        CheckDatabase();

        ConsoleService.ShowSection("Outils");
        foreach (var (name, found, version, required) in results)
        {
            if (!found)
            {
                if (required)
                    ConsoleService.Warning($"  {name} (requis, non installe)");
                else
                    ConsoleService.Info($"  {name} (non installe)");
                continue;
            }

            ConsoleService.Success($"  {name} — {version}");
        }

        ConsoleService.ShowSection("Templates");
        var adapters = new IAdapter[]
        {
            new DotnetAdapter(), new NpmAdapter(), new CargoAdapter(), new GoAdapter(),
            new PythonAdapter(), new FlutterAdapter(), new ComposerAdapter(), new RailsAdapter(),
            new GradleAdapter(), new SwiftAdapter(), new ZigAdapter(), new MixAdapter(),
            new CabalAdapter(), new ScalaAdapter(), new CrystalAdapter()
        };

        var available = adapters.Count(a => a.IsAvailable);
        ConsoleService.ShowKeyValue("Ecosystemes disponibles", $"{available}/{adapters.Length}");

        ConsoleService.ShowSection("Version");
        ConsoleService.ShowKeyValue("Scaffolder", $"v{UpdateService.CurrentVersion}");

        ConsoleService.WriteLine();

        var criticalMissing = results.Any(r => r.Required && !r.Found);
        if (criticalMissing)
        {
            ConsoleService.Warning("Certains outils requis sont manquants.");
            return 1;
        }

        ConsoleService.Success("Tout est pret !");
        return 0;
    }

    private static int PrintJson(List<(string Name, bool Found, string Version, bool Required)> results)
    {
        // JsonNode + contexte source-gen : compatible AOT (pas de types anonymes).
        var tools = new List<JsonNode?>();
        foreach (var r in results)
        {
            tools.Add(new JsonObject
            {
                ["name"] = r.Name,
                ["found"] = r.Found,
                ["version"] = r.Version,
                ["required"] = r.Required
            });
        }

        var payload = new JsonObject
        {
            ["version"] = UpdateService.CurrentVersion,
            ["os"] = GetOS(),
            ["arch"] = GetArch(),
            ["shell"] = GetShell(),
            ["tools"] = new JsonArray(tools.ToArray()),
            ["database"] = new JsonObject
            {
                ["path"] = Paths.DatabaseFile,
                ["exists"] = File.Exists(Paths.DatabaseFile),
                ["size"] = DatabaseService.GetStats().Size
            },
            ["config"] = new JsonObject
            {
                ["apiKey"] = AIService.HasApiKey,
                ["provider"] = AIService.Provider,
                ["model"] = AIService.Model
            }
        };

        Console.WriteLine(JsonSerializer.Serialize<JsonNode>(payload, JsonContext.Default.JsonNode));
        return 0;
    }

    private static string GetOS()
    {
        if (OperatingSystem.IsWindows()) return "Windows";
        if (OperatingSystem.IsMacOS()) return "macOS";
        return "Linux";
    }

    private static string GetArch() =>
        System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString();

    private static string GetShell() =>
        Environment.GetEnvironmentVariable("SHELL") ?? "inconnu";

    private static void CheckConfig()
    {
        ConsoleService.ShowKeyValue("Cle API", AIService.HasApiKey ? "configuree" : "absente");

        if (AIService.HasApiKey)
        {
            ConsoleService.ShowKeyValue("Fournisseur", AIService.Provider);
            ConsoleService.ShowKeyValue("Modele", AIService.Model);
        }

        foreach (var key in new[]
                 {
                     ConfigService.Keys.Theme, ConfigService.Keys.Experience,
                     ConfigService.Keys.Author, ConfigService.Keys.Company,
                     ConfigService.Keys.DefaultAuth, ConfigService.Keys.DefaultDb
                 })
        {
            var value = ConfigService.Get(key);
            if (!string.IsNullOrWhiteSpace(value))
                ConsoleService.ShowKeyValue(key, value);
        }
    }

    private static void CheckDatabase()
    {
        if (!File.Exists(Paths.DatabaseFile))
        {
            ConsoleService.Warning("  Aucune base locale (elle sera creee au prochain lancement).");
            return;
        }

        try
        {
            var db = DatabaseService.LoadDatabase();
            var (size, created, modified) = DatabaseService.GetStats();

            ConsoleService.ShowKeyValue("Fichier", Paths.DatabaseFile);
            ConsoleService.ShowKeyValue("Taille", size);
            ConsoleService.ShowKeyValue("Sections", db.Count.ToString());
            ConsoleService.ShowKeyValue("Creee le", created);
            ConsoleService.ShowKeyValue("Modifiee le", modified);

            // Verifie que le dechiffrement fonctionne reellement.
            _ = db.Count;
            ConsoleService.Success("  Base dechiffree correctement.");
        }
        catch (Exception ex)
        {
            ConsoleService.Error($"  Base illisible : {ex.Message}");
        }
    }
}
