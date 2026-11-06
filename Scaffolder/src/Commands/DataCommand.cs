using System.CommandLine;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class DataCommand : Command
{
    public DataCommand() : base("data", "Gere la base de donnees chiffree locale")
    {
        var statusCmd = new Command("status", "Affiche l'etat de la base");
        statusCmd.SetAction(_ => HandleStatus());

        var exportCmd = new Command("export", "Exporte la base en JSON lisible");
        var exportFileArg = new Argument<FileInfo?>("file") { Arity = ArgumentArity.ZeroOrOne };
        exportCmd.Add(exportFileArg);
        exportCmd.SetAction((ParseResult pr) => HandleExport(pr.GetValue(exportFileArg)));

        var importCmd = new Command("import", "Importe une base depuis un JSON");
        var importFileArg = new Argument<FileInfo>("file") { Description = "Fichier JSON a importer" };
        importCmd.Add(importFileArg);
        importCmd.SetAction((ParseResult pr) => HandleImport(pr.GetValue(importFileArg)));

        var resetCmd = new Command("reset", "Reinitialise la base (supprime toutes les donnees)");
        resetCmd.SetAction(_ => HandleReset());

        var sectionsCmd = new Command("sections", "Liste les sections disponibles");
        sectionsCmd.SetAction(_ => HandleSections());

        var getCmd = new Command("get", "Lit une valeur de la base");
        var sectionArg = new Argument<string>("section");
        var keyArg = new Argument<string>("key");
        getCmd.Add(sectionArg);
        getCmd.Add(keyArg);
        getCmd.SetAction((ParseResult pr) => HandleGet(pr.GetValue(sectionArg), pr.GetValue(keyArg)));

        var setCmd = new Command("set", "Ecrit une valeur dans la base");
        var setSectionArg = new Argument<string>("section");
        var setKeyArg = new Argument<string>("key");
        var setValueArg = new Argument<string>("value");
        setCmd.Add(setSectionArg);
        setCmd.Add(setKeyArg);
        setCmd.Add(setValueArg);
        setCmd.SetAction((ParseResult pr) => HandleSet(
            pr.GetValue(setSectionArg), pr.GetValue(setKeyArg), pr.GetValue(setValueArg)));

        Add(statusCmd);
        Add(exportCmd);
        Add(importCmd);
        Add(resetCmd);
        Add(sectionsCmd);
        Add(getCmd);
        Add(setCmd);

        SetAction(_ =>
        {
            ConsoleService.Info("Sous-commandes : status, export, import, reset, sections, get, set");
            ConsoleService.Info("Exemple : scaffold data status");
            ConsoleService.Info("Exemple : scaffold data get settings theme");
            ConsoleService.Info("Exemple : scaffold data set settings author 'Mon Nom'");
            ConsoleService.Info("Exemple : scaffold data export backup.json");
            return 0;
        });
    }

    private static int HandleStatus()
    {
        ConsoleService.ShowBanner("Base de donnees chiffree", "~/.scaffolder/scaffolder.scaffolder-cli");
        Console.WriteLine();

        var (size, created, modified) = DatabaseService.GetStats();
        ConsoleService.SummaryLine("Taille", size);
        ConsoleService.SummaryLine("Creee le", created);
        ConsoleService.SummaryLine("Modifiee le", modified);
        Console.WriteLine();

        var db = DatabaseService.LoadDatabase();
        ConsoleService.ShowSection("Sections");
        foreach (var section in db.Keys.OrderBy(k => k))
        {
            var count = db[section].Count;
            ConsoleService.SummaryLine($"  {section}", $"{count} cle(s)");
        }

        Console.WriteLine();
        ConsoleService.Info("La base est chiffree avec AES-256.");
        ConsoleService.Info("Cle derivee du numero de machine (AOT-safe, aucune dependance externe).");
        return 0;
    }

    private static int HandleExport(FileInfo? file)
    {
        var path = file?.FullName ?? Path.Combine(Directory.GetCurrentDirectory(), "scaffolder-export.json");
        DatabaseService.ExportToFile(path);
        return 0;
    }

    private static int HandleImport(FileInfo? file)
    {
        if (file == null)
        {
            ConsoleService.Error("Usage : scaffold data import <fichier.json>");
            return 1;
        }
        return DatabaseService.ImportFromFile(file.FullName) ? 0 : 1;
    }

    private static int HandleReset()
    {
        var confirm = ConsoleService.Confirm(
            "[red]  Supprimer toutes les donnees de la base ? Cette action est irreversible.[/]", false);
        if (!confirm)
        {
            ConsoleService.Warning("Operation annulee.");
            return 0;
        }

        DatabaseService.Reset();
        return 0;
    }

    private static int HandleSections()
    {
        var db = DatabaseService.LoadDatabase();
        ConsoleService.ShowTable(
            ["Section", "Cles", "Exemple"],
            db.Keys.OrderBy(k => k).Select(k =>
            {
                var keys = db[k].Keys.Take(5).ToList();
                var example = keys.Count > 0 ? $"{keys[0]} = {db[k][keys[0]][..Math.Min(30, db[k][keys[0]].Length)]}" : "(vide)";
                return new[] { k, db[k].Count.ToString(), example };
            }).ToArray()
        );
        return 0;
    }

    private static int HandleGet(string? section, string? key)
    {
        if (string.IsNullOrWhiteSpace(section))
        {
            ConsoleService.Error("Usage : scaffold data get <section> <cle>");
            return 1;
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            var values = DatabaseService.GetSection(section);
            if (values.Count == 0)
            {
                ConsoleService.Warning($"Section '{section}' vide ou inexistante.");
                return 0;
            }

            ConsoleService.ShowSection(section);
            foreach (var (k, v) in values)
                ConsoleService.SummaryLine(k, v);
            return 0;
        }

        var val = DatabaseService.GetValue(section, key);
        if (val != null)
        {
            ConsoleService.SummaryLine($"{section}.{key}", val);
            return 0;
        }

        ConsoleService.Warning($"Cle '{key}' introuvable dans '{section}'.");
        return 1;
    }

    private static int HandleSet(string? section, string? key, string? value)
    {
        if (string.IsNullOrWhiteSpace(section) || string.IsNullOrWhiteSpace(key) || value == null)
        {
            ConsoleService.Error("Usage : scaffold data set <section> <cle> <valeur>");
            return 1;
        }

        DatabaseService.SetValue(section, key, value);
        ConsoleService.Success($"{section}.{key} = {value}");
        return 0;
    }
}
