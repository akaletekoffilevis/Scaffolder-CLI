namespace Scaffolder.Services;

/// <summary>
/// Options globales acceptees par toutes les commandes : --yes, --no-color, --verbose, --quiet.
/// Elles sont extraites de la ligne de commande avant l'analyse par System.CommandLine,
/// ce qui garantit qu'elles fonctionnent partout (y compris sur les sous-commandes).
/// </summary>
public static class AppOptions
{
    /// <summary>Repond "oui" a toutes les confirmations (mode non interactif / CI).</summary>
    public static bool Yes { get; set; }

    /// <summary>Desactive les couleurs ANSI.</summary>
    public static bool NoColor { get; set; }

    /// <summary>Active les logs detailles.</summary>
    public static bool Verbose { get; set; }

    /// <summary>Reduit la sortie au strict necessaire.</summary>
    public static bool Quiet { get; set; }

    /// <summary>Retire les options globales de la ligne de commande et les applique.</summary>
    public static string[] Parse(string[] args)
    {
        var remaining = new List<string>(args.Length);

        foreach (var arg in args)
        {
            switch (arg)
            {
                case "--yes" or "-y":
                    Yes = true;
                    break;
                case "--no-color" or "--no-colour":
                    NoColor = true;
                    break;
                case "--verbose":
                    Verbose = true;
                    break;
                case "--quiet" or "-q":
                    Quiet = true;
                    break;
                default:
                    remaining.Add(arg);
                    break;
            }
        }

        if (Verbose) ConsoleService.Verbose = true;
        if (Quiet) ConsoleService.Quiet = true;
        if (NoColor) ConsoleService.DisableColor();

        return remaining.ToArray();
    }

    public static void Reset()
    {
        Yes = false;
        NoColor = false;
        Verbose = false;
        Quiet = false;
    }
}
