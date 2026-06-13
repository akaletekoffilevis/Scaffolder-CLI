using Spectre.Console;

namespace Scaffolder.Services;

public static class ConsoleService
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".scaffolder");
    private static readonly string ConfigFile = Path.Combine(ConfigDir, "config.json");

    public static bool Verbose { get; set; } = false;

    public static void Debug(string text)
    {
        if (Verbose)
            AnsiConsole.MarkupLine("[dim]  \U0001f50d {0}[/]", Escape(text));
    }

    public static void Write(string text, ConsoleColor? color = null)
    {
        if (color.HasValue)
        {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = color.Value;
            Console.Write(text);
            Console.ForegroundColor = prev;
        }
        else
        {
            Console.Write(text);
        }
    }

    public static void WriteLine(string text = "", ConsoleColor? color = null)
    {
        Write(text + "\n", color);
    }

    public static void Success(string text)
    {
        AnsiConsole.MarkupLine("[green]  \u2705 {0}[/]", Escape(text));
    }

    public static void Error(string text)
    {
        AnsiConsole.MarkupLine("[red]  \u274c {0}[/]", Escape(text));
    }

    public static void Warning(string text)
    {
        AnsiConsole.MarkupLine("[yellow]  \u26a0\ufe0f  {0}[/]", Escape(text));
    }

    public static void Info(string text)
    {
        AnsiConsole.MarkupLine("[cyan]  \u2139\ufe0f  {0}[/]", Escape(text));
    }

    public static void Highlight(string text)
    {
        AnsiConsole.Markup("[magenta]{0}[/]", Escape(text));
    }

    public static void ShowLogo()
    {
        AnsiConsole.Write(new FigletText("Scaffolder").Centered().Color(Spectre.Console.Color.Blue));
        AnsiConsole.MarkupLine("[cyan]  CLI universel pour generer des projets[/]");
        AnsiConsole.MarkupLine("[cyan]  github.com/akaletekoffilevis/Scaffolder-CLI[/]");
    }

    public static void CheckFirstRun()
    {
        if (File.Exists(ConfigFile)) return;

        ShowLogo();
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[green]\U0001f44b  Bienvenue dans Scaffolder ![/]");
        AnsiConsole.WriteLine();
        Info("Je vais t'aider a creer ton premier projet en 30 secondes.");
        Info("Scaffolder fonctionne avec tous les langages : C#, Python, JS, Rust, Go...");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]\U0001f4d6  Tape `scaffold --help` pour voir toutes les commandes.[/]");
        AnsiConsole.MarkupLine("[green]\U0001f680  Tape `scaffold new` pour creer ton premier projet.[/]");
        AnsiConsole.WriteLine();

        Directory.CreateDirectory(ConfigDir);
        File.WriteAllText(ConfigFile, """
        {
          "firstRun": false,
          "theme": "default",
          "experience": "beginner"
        }
        """);
    }

    public static string Prompt(string question, string defaultValue = "", Func<string, bool>? validate = null, string? errorMsg = null)
    {
        if (Console.IsInputRedirected)
        {
            var input = Console.ReadLine() ?? "";
            if (!string.IsNullOrWhiteSpace(input))
            {
                WriteLine($"  {question} > {input.Trim()}", ConsoleColor.Green);
                return input.Trim();
            }
            return defaultValue;
        }

        while (true)
        {
            var result = AnsiConsole.Ask<string>(question, defaultValue);
            if (validate == null || validate(result))
                return result;
            AnsiConsole.MarkupLine($"[red]  \u274c {Escape(errorMsg ?? "Valeur invalide.")}[/]");
        }
    }

    public static string Select(string question, string[] options)
    {
        if (Console.IsInputRedirected)
        {
            var input = Console.ReadLine() ?? "";
            var trimmed = input.Trim().ToLowerInvariant();

            var match = options.FirstOrDefault(o =>
                o.ToLowerInvariant() == trimmed ||
                o.ToLowerInvariant().StartsWith(trimmed));

            if (match != null)
            {
                WriteLine($"  {question} > {match}", ConsoleColor.Green);
                return match;
            }

            WriteLine($"  {question} > {options[0]} (defaut)", ConsoleColor.Green);
            return options[0];
        }

        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title(question)
                .PageSize(15)
                .HighlightStyle(new Style(foreground: Spectre.Console.Color.Cyan1, decoration: Decoration.Bold))
                .AddChoices(options));
    }

    public static bool Confirm(string question, bool defaultValue = true)
    {
        if (Console.IsInputRedirected)
            return defaultValue;

        return AnsiConsole.Confirm(question, defaultValue);
    }

    public static void StepHeader(int current, int total, string title)
    {
        AnsiConsole.MarkupLine($"\n[bold cyan][{current}/{total}][/] [white]{Escape(title)}[/]");
    }

    public static void KeyboardHint()
    {
        AnsiConsole.MarkupLine("[gray]  \u2328 Utilise les fleches (↑ ↓) et Entree pour choisir. Tape pour chercher.[/]");
    }

    public static void SummaryLine(string label, string value)
    {
        AnsiConsole.MarkupLine($"  [cyan]{Escape(label)}:[/] [white]{Escape(value)}[/]");
    }

    public static async Task ShowSpinner(string message, Func<Task> action)
    {
        await AnsiConsole.Status()
            .StartAsync(message, async _ =>
            {
                await action();
            });
    }

    public static void MarkupLine(string markup)
    {
        AnsiConsole.MarkupLine(markup);
    }

    public static void WriteLine()
    {
        AnsiConsole.WriteLine();
    }

    public static void WriteCmdLine(string text, ConsoleColor? color = null)
    {
        if (color.HasValue)
        {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = color.Value;
            Console.WriteLine("  " + text);
            Console.ForegroundColor = prev;
        }
        else
        {
            Console.WriteLine("  " + text);
        }
    }

    private static string Escape(string text)
    {
        return text?.Replace("[", "[[").Replace("]", "]]") ?? "";
    }
}
