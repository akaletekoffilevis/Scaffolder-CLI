using System.CommandLine;
using Scaffolder.Cli;

namespace Scaffolder.Services;

/// <summary>Description d'un paramètre (argument positionnel ou option) lue depuis System.CommandLine.</summary>
public sealed record CliParameter(
    string Token,        // "--name" pour une option, "<name>" pour un argument
    string Description,
    bool IsArgument,
    bool IsFlag,         // option booléenne (case à cocher)
    bool Required,
    bool Multiple,       // argument ou option acceptant plusieurs valeurs
    string? Default);

/// <summary>Une commande exécutable : son chemin (ex: "config get") et ses paramètres.</summary>
public sealed record CliEntry(string Path, string Description, IReadOnlyList<CliParameter> Parameters);

/// <summary>
/// Catalogue généré automatiquement depuis l'arbre de commandes : toute nouvelle commande
/// apparaît dans le TUI et le web sans code supplémentaire.
/// </summary>
public static class CliCatalog
{
    // Commandes qui ne doivent pas être lancées depuis une interface (interactives, serveur, auto-modifiantes).
    public static readonly HashSet<string> Excluded = ["ui", "tui", "watch", "bug", "upgrade", "update"];

    private static readonly HashSet<string> HiddenOptions = ["--help", "-h", "-?", "--version", "-v"];

    private static List<CliEntry>? _cache;

    public static IReadOnlyList<CliEntry> All => _cache ??= Build();

    private static List<CliEntry> Build()
    {
        var root = RootFactory.Create();
        var entries = new List<CliEntry>();
        Walk(root, "", entries);
        return entries
            .Where(e => !Excluded.Contains(e.Path.Split(' ')[0]))
            .OrderBy(e => e.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void Walk(Command command, string prefix, List<CliEntry> entries)
    {
        foreach (var sub in command.Subcommands)
        {
            if (sub.Hidden) continue;

            var path = string.IsNullOrEmpty(prefix) ? sub.Name : $"{prefix} {sub.Name}";
            // Une commande mal formée ne doit jamais casser le catalogue entier.
            var parameters = TryParameters(sub);
            entries.Add(new CliEntry(path, sub.Description ?? "", parameters));
            Walk(sub, path, entries);
        }
    }

    private static List<CliParameter> TryParameters(Command command)
    {
        try
        {
            return ParametersOf(command);
        }
        catch (InvalidOperationException)
        {
            return [];
        }
    }

    private static List<CliParameter> ParametersOf(Command command)
    {
        var list = new List<CliParameter>();

        foreach (var arg in command.Arguments)
        {
            var multiple = arg.ValueType == typeof(string[]);
            list.Add(new CliParameter(
                $"<{arg.Name}>",
                arg.Description ?? "",
                IsArgument: true,
                IsFlag: false,
                Required: arg.Arity.MinimumNumberOfValues > 0,
                Multiple: multiple,
                Default: null));
        }

        foreach (var opt in command.Options)
        {
            if (opt.Aliases.Any(HiddenOptions.Contains) || HiddenOptions.Contains(opt.Name)) continue;

            var token = opt.Aliases.FirstOrDefault(a => a.StartsWith("--")) ?? opt.Name;
            list.Add(new CliParameter(
                token,
                opt.Description ?? "",
                IsArgument: false,
                IsFlag: opt.ValueType == typeof(bool),
                Required: opt.Required,
                Multiple: opt.ValueType == typeof(string[]),
                Default: null));
        }

        return list;
    }
}
