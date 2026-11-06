using System.CommandLine;
using System.Text.Json;
using Scaffolder.Adapters;
using Scaffolder.Services;

namespace Scaffolder.Commands;

/// <summary>
/// Liste tous les templates disponibles (adapters, variantes et Hello World)
/// avec leur etat d'installation. Remplace l'ancien `new --list` incomplet.
/// </summary>
public class ListCommand : Command
{
    private static readonly IAdapter[] Adapters =
    [
        new DotnetAdapter(), new NpmAdapter(), new CargoAdapter(), new GoAdapter(),
        new PythonAdapter(), new FlutterAdapter(), new ComposerAdapter(), new RailsAdapter(),
        new GradleAdapter(), new SwiftAdapter(), new ZigAdapter(), new MixAdapter(),
        new CabalAdapter(), new ScalaAdapter(), new CrystalAdapter()
    ];

    private static readonly string[] HelloLanguages =
    [
        "python", "javascript", "typescript", "c#", "rust", "go", "ruby", "php",
        "dart", "swift", "kotlin", "java", "zig", "scala", "elixir", "crystal"
    ];

    private readonly Option<bool> _jsonOpt = new("--json", ["-j"])
    {
        Description = "Sortie JSON (pour scripts)"
    };

    private readonly Option<bool> _availableOpt = new("--available")
    {
        Description = "N'affiche que les templates dont l'outil est installe"
    };

    public ListCommand() : base("list", "Liste les templates disponibles")
    {
        Aliases.Add("templates");

        Add(_jsonOpt);
        Add(_availableOpt);

        SetAction(HandleList);
    }

    private int HandleList(ParseResult pr)
    {
        var json = pr.GetValue(_jsonOpt);
        var onlyAvailable = pr.GetValue(_availableOpt);

        var entries = new List<(string Template, string Description, bool Available, string? Language)>();

        foreach (var lang in HelloLanguages)
            entries.Add(("hello", $"Hello World en {lang}", true, lang));

        foreach (var adapter in Adapters)
        {
            var available = adapter.IsAvailable;
            foreach (var sub in adapter.SubTemplates)
            {
                var display = sub == adapter.Name ? adapter.Name : $"{adapter.Name} {sub}";
                entries.Add((display, adapter.Description, available, null));
            }
        }

        if (onlyAvailable)
            entries = entries.Where(e => e.Available).ToList();

        if (json)
            return PrintJson(entries);

        ConsoleService.ShowBanner("Templates disponibles", $"{entries.Count} entrees");

        foreach (var group in entries.GroupBy(e => e.Template.Split(' ')[0]))
        {
            ConsoleService.MarkupLine($"[bold cyan]{ConsoleService.Escape(group.Key)}[/]");
            foreach (var entry in group)
            {
                var status = entry.Available ? "[green]OK[/]" : "[red]outil manquant[/]";
                var detail = entry.Language != null ? $" ({entry.Language})" : "";
                ConsoleService.MarkupLine(
                    $"  [white]{ConsoleService.Escape(entry.Template)}[/]{ConsoleService.Escape(detail)} — " +
                    $"{ConsoleService.Escape(entry.Description)} {status}");
            }
            ConsoleService.WriteLine();
        }

        ConsoleService.Info("Utilise : scaffold new --template <nom> --name <projet>");
        ConsoleService.Info("Composition : scaffold new --template dotnet+react");
        return 0;
    }

    private static int PrintJson(List<(string Template, string Description, bool Available, string? Language)> entries)
    {
        var payload = entries.Select(e => new
        {
            template = e.Template,
            language = e.Language,
            description = e.Description,
            available = e.Available
        });

        Console.WriteLine(JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }
}
