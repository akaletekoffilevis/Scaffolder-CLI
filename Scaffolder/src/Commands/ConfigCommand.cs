using System.CommandLine;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class ConfigCommand : Command
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".scaffolder");
    private static readonly string ConfigFile = Path.Combine(ConfigDir, "config.json");

    public ConfigCommand() : base("config", "Gère la configuration de Scaffolder")
    {
        var initCmd = new Command("init", "Initialise la configuration");
        initCmd.SetAction(_ => HandleInit());

        var getCmd = new Command("get", "Affiche une valeur de configuration");
        var keyArg = new Argument<string>("key") { Arity = ArgumentArity.ZeroOrOne };
        getCmd.Add(keyArg);
        getCmd.SetAction((ParseResult pr) => HandleGet(pr.GetValue(keyArg)));

        var setCmd = new Command("set", "Définit une valeur de configuration");
        var setKeyArg = new Argument<string>("key") { Arity = ArgumentArity.ZeroOrOne };
        var setValArg = new Argument<string>("value") { Arity = ArgumentArity.ZeroOrOne };
        setCmd.Add(setKeyArg);
        setCmd.Add(setValArg);
        setCmd.SetAction((ParseResult pr) => HandleSet(pr.GetValue(setKeyArg), pr.GetValue(setValArg)));

        var resetCmd = new Command("reset", "Réinitialise la configuration");
        resetCmd.SetAction(_ => HandleReset());

        Add(initCmd);
        Add(getCmd);
        Add(setCmd);
        Add(resetCmd);

        var profileCmd = new Command("profile", "Gère les profils de configuration");
        profileCmd.SetAction(_ => HandleProfile());
        Add(profileCmd);

        var importCmd = new Command("import", "Importe une configuration depuis un fichier");
        var importFileArg = new Argument<FileInfo>("file") { Description = "Fichier de config a importer (.json)" };
        importCmd.Add(importFileArg);
        importCmd.SetAction((ParseResult pr) => HandleImport(pr.GetValue(importFileArg)));
        Add(importCmd);

        var exportCmd = new Command("export", "Exporte la configuration vers un fichier");
        var exportFileArg = new Argument<FileInfo>("file")
        {
            Description = "Fichier de destination (.json)",
            Arity = ArgumentArity.ZeroOrOne
        };
        exportCmd.Add(exportFileArg);
        exportCmd.SetAction((ParseResult pr) => HandleExport(pr.GetValue(exportFileArg)));
        Add(exportCmd);

        SetAction(_ =>
        {
            ConsoleService.Info("Sous-commandes : init, get, set, reset, profile, import, export");
            ConsoleService.Info("Exemple : scaffold config set experience advanced");
            ConsoleService.Info("Exemple : scaffold config import config.json");
            ConsoleService.Info("Exemple : scaffold config export config.json");
            return 0;
        });
    }

    private static int HandleInit()
    {
        Directory.CreateDirectory(ConfigDir);
        var defaults = """
        {
          "firstRun": "false",
          "theme": "default",
          "experience": "beginner"
        }
        """;
        File.WriteAllText(ConfigFile, defaults);
        ConsoleService.Success("Configuration initialisee.");
        return 0;
    }

    private static int HandleGet(string? key)
    {
        if (!ReadConfig(out var dict))
            return 1;

        if (key != null && dict.TryGetValue(key, out var val))
        {
            ConsoleService.Info($"  {key} = {val}");
            return 0;
        }

        ConsoleService.Info("Configuration actuelle :");
        foreach (var (k, v) in dict)
            ConsoleService.Info($"  {k} = {v}");
        return 0;
    }

    private static int HandleSet(string? key, string? value)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
        {
            ConsoleService.Error("Usage : scaffold config set <cle> <valeur>");
            return 1;
        }

        if (!ReadConfig(out var dict))
            return 1;

        dict[key] = value;
        SaveConfig(dict);
        ConsoleService.Success($"  {key} = {value}");
        return 0;
    }

    private static int HandleReset()
    {
        if (File.Exists(ConfigFile))
            File.Delete(ConfigFile);

        HandleInit();
        ConsoleService.Success("Configuration reinitialisee.");
        return 0;
    }

    private static int HandleProfile()
    {
        if (!ReadConfig(out var dict))
            dict = [];

        var current = dict.TryGetValue("profile", out var p) ? p : "default";

        ConsoleService.Info($"Profil actuel : {current}");
        Console.WriteLine();
        ConsoleService.Info("Pour creer/changer de profil :");
        ConsoleService.Info("  scaffold config set profile <nom>");
        Console.WriteLine();
        ConsoleService.Info("Exemples :");
        ConsoleService.Info("  scaffold config set profile perso");
        ConsoleService.Info("  scaffold config set profile pro");
        ConsoleService.Info("  scaffold config set profile equipe");
        return 0;
    }

    private static int HandleImport(FileInfo? file)
    {
        if (file == null || !File.Exists(file.FullName))
        {
            ConsoleService.Error("Fichier introuvable.");
            return 1;
        }

        try
        {
            var imported = new Dictionary<string, string>();
            foreach (var line in File.ReadAllLines(file.FullName))
            {
                var trimmed = line.Trim();
                if (!trimmed.Contains(':')) continue;
                var parts = trimmed.Split(':', 2);
                var k = parts[0].Trim(' ', '"', '\t');
                var v = parts[1].Trim(' ', '"', '\t', ',');
                if (!string.IsNullOrEmpty(k))
                    imported[k] = v;
            }

            if (!ReadConfig(out var current))
                current = [];

            foreach (var (k, v) in imported)
                current[k] = v;

            SaveConfig(current);
            ConsoleService.Success($"Configuration importee depuis {file.Name} ({imported.Count} valeurs).");
            return 0;
        }
        catch (Exception ex)
        {
            ConsoleService.Error($"Erreur d'import : {ex.Message}");
            return 1;
        }
    }

    private static int HandleExport(FileInfo? file)
    {
        if (!ReadConfig(out var dict))
            return 1;

        var exportPath = file?.FullName ?? Path.Combine(Directory.GetCurrentDirectory(), "scaffold-config.json");
        var lines = dict.Select(kv => $"  \"{kv.Key}\": \"{kv.Value}\"");
        var json = "{\n" + string.Join(",\n", lines) + "\n}\n";
        File.WriteAllText(exportPath, json);
        ConsoleService.Success($"Configuration exportee vers {exportPath}");
        return 0;
    }

    private static bool ReadConfig(out Dictionary<string, string> dict)
    {
        dict = [];
        if (!File.Exists(ConfigFile))
        {
            ConsoleService.Warning("Aucune configuration trouvee. Lance 'scaffold config init'.");
            return false;
        }

        try
        {
            dict = [];
            foreach (var line in File.ReadAllLines(ConfigFile))
            {
                var trimmed = line.Trim();
                if (!trimmed.Contains(':')) continue;
                var parts = trimmed.Split(':', 2);
                var k = parts[0].Trim(' ', '"', '\t');
                var v = parts[1].Trim(' ', '"', '\t', ',');
                if (!string.IsNullOrEmpty(k))
                    dict[k] = v;
            }
            return true;
        }
        catch
        {
            ConsoleService.Warning("Configuration corrompue. Lance 'scaffold config reset'.");
            return false;
        }
    }

    private static void SaveConfig(Dictionary<string, string> dict)
    {
        Directory.CreateDirectory(ConfigDir);
        var lines = dict.Select(kv => $"  \"{kv.Key}\": \"{kv.Value}\"");
        var json = "{\n" + string.Join(",\n", lines) + "\n}\n";
        File.WriteAllText(ConfigFile, json);
    }
}
