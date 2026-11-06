using System.CommandLine;
using System.Globalization;
using Scaffolder.Services;

namespace Scaffolder.Commands;

/// <summary>Affiche des statistiques d'utilisation locales (100% hors ligne).</summary>
public class StatsCommand : Command
{
    private readonly Option<bool> _jsonOpt = new("--json", ["-j"])
    {
        Description = "Sortie JSON"
    };

    private readonly Option<int> _limitOpt = new("--limit", ["-n"])
    {
        Description = "Nombre d'entrees d'historique a afficher",
        DefaultValueFactory = _ => 10
    };

    public StatsCommand() : base("stats", "Affiche les statistiques d'utilisation locales")
    {
        Add(_jsonOpt);
        Add(_limitOpt);

        SetAction(HandleStats);
    }

    private int HandleStats(ParseResult pr)
    {
        var limit = Math.Clamp(pr.GetValue(_limitOpt), 1, 200);
        var db = DatabaseService.LoadDatabase();
        var history = DatabaseService.GetHistory(limit);
        var favorites = DatabaseService.GetFavorites();
        var plugins = DatabaseService.GetPlugins();
        var versions = VersionManager.GetHistory(limit);
        var (size, created, modified) = DatabaseService.GetStats();

        var byTemplate = history
            .GroupBy(h => h.GetValueOrDefault("template", "inconnu"))
            .OrderByDescending(g => g.Count())
            .ToDictionary(g => g.Key, g => g.Count());

        if (pr.GetValue(_jsonOpt))
        {
            var payload = new
            {
                database = new { size, created, modified, sections = db.Count },
                generations = history.Count,
                byTemplate,
                favorites = favorites.Count,
                plugins = plugins.Count,
                versions = versions.Count,
                history
            };

            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(
                payload, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        ConsoleService.ShowBanner("Statistiques", "Donnees locales (~/.scaffolder)");

        ConsoleService.ShowSection("Base de donnees");
        ConsoleService.ShowKeyValue("Fichier", Paths.DatabaseFile);
        ConsoleService.ShowKeyValue("Taille", size);
        ConsoleService.ShowKeyValue("Sections", db.Count.ToString(CultureInfo.InvariantCulture));
        ConsoleService.ShowKeyValue("Creee le", FormatDate(created));
        ConsoleService.ShowKeyValue("Modifiee le", FormatDate(modified));

        ConsoleService.ShowSection("Projets");
        ConsoleService.ShowKeyValue("Generations enregistrees", history.Count.ToString(CultureInfo.InvariantCulture));
        ConsoleService.ShowKeyValue("Favoris", favorites.Count.ToString(CultureInfo.InvariantCulture));
        ConsoleService.ShowKeyValue("Plugins", plugins.Count.ToString(CultureInfo.InvariantCulture));
        ConsoleService.ShowKeyValue("Versions connues", versions.Count.ToString(CultureInfo.InvariantCulture));

        if (byTemplate.Count > 0)
        {
            ConsoleService.ShowSection("Templates les plus utilises");
            foreach (var (template, count) in byTemplate.Take(10))
                ConsoleService.ShowKeyValue(template, $"{count} generation(s)");
        }

        if (history.Count > 0)
        {
            ConsoleService.ShowSection($"Derniers projets ({history.Count})");
            ConsoleService.ShowTable(
                ["Nom", "Template", "Date"],
                history.Select(h => new[]
                {
                    h.GetValueOrDefault("name", "?"),
                    h.GetValueOrDefault("template", "?"),
                    FormatDate(h.GetValueOrDefault("date", ""))
                }).ToArray());
        }
        else
        {
            ConsoleService.Info("Aucune generation enregistree pour le moment.");
        }

        return 0;
    }

    private static string FormatDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "N/A";

        return DateTime.TryParse(value, CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind, out var dt)
            ? dt.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
            : value;
    }
}
