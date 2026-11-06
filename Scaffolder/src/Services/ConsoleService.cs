using Spectre.Console;

namespace Scaffolder.Services;

public static class ConsoleService
{
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
        AnsiConsole.MarkupLine("[cyan]  CLI universel pour générer des projets[/]");
        AnsiConsole.MarkupLine("[cyan]  github.com/akaletekoffilevis/Scaffolder-CLI[/]");
    }

    public static void ShowBanner(string title, string subtitle)
    {
        var rule = new Rule($"[bold blue]{Escape(title)}[/]")
        {
            Style = Style.Parse("blue")
        };
        AnsiConsole.Write(rule);
        if (!string.IsNullOrEmpty(subtitle))
            AnsiConsole.MarkupLine($"[gray]  {Escape(subtitle)}[/]");
        AnsiConsole.WriteLine();
    }

    public static void ShowTable(string[] headers, string[][] rows)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(rows);

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Spectre.Console.Color.Grey)
            .Title("")
            .AddColumn(new TableColumn($"[bold]{Escape(headers[0])}[/]").NoWrap());

        for (var i = 1; i < headers.Length; i++)
            table.AddColumn(new TableColumn($"[bold]{Escape(headers[i])}[/]"));

        foreach (var row in rows)
        {
            var cols = row.Length > 0 ? row : [" "];
            table.AddRow(cols.Select(c => Escape(c)).ToArray());
        }

        AnsiConsole.Write(table);
    }

    public static void ShowProgressBar(string label, int current, int total)
    {
        var pct = total > 0 ? (int)((double)current / total * 100) : 0;
        var bar = new BarChart()
            .AddItem(label, pct, Spectre.Console.Color.Green);
        AnsiConsole.Write(bar);
    }

    public static void ShowKeyValue(string key, string value, string? color = null)
    {
        var valueMarkup = color != null ? $"[{color}]{Escape(value)}[/]" : $"[white]{Escape(value)}[/]";
        AnsiConsole.MarkupLine($"  [grey]{Escape(key),-20}[/] {valueMarkup}");
    }

    public static void CheckFirstRun()
    {
        DatabaseService.Initialize();
        var firstRun = ConfigService.Get(ConfigService.Keys.FirstRun);
        if (firstRun == "false") return;

        ShowLogo();
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[green]\U0001f44b  Bienvenue dans Scaffolder ![/]");
        AnsiConsole.WriteLine();
        Info("Je vais t'aider à créer ton premier projet en 30 secondes.");
        Info("Scaffolder fonctionne avec tous les langages : C#, Python, JS, Rust, Go...");
        Info("Toutes tes donnees sont stockees en local (chiffrees AES-256).");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]\U0001f4d6  Tape `scaffold --help` pour voir toutes les commandes.[/]");
        AnsiConsole.MarkupLine("[green]\U0001f680  Tape `scaffold new` pour creer ton premier projet.[/]");
        AnsiConsole.MarkupLine("[cyan]\U0001f512  Tape `scaffold data status` pour voir ta base chiffree.[/]");
        AnsiConsole.WriteLine();

        ConfigService.Set(ConfigService.Keys.FirstRun, "false");
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
        ArgumentNullException.ThrowIfNull(options);
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
        AnsiConsole.MarkupLine($"\n[bold cyan]{Escape($"{current}/{total}")}[/] [white]{Escape(title)}[/]");
    }

    public static void KeyboardHint()
    {
        AnsiConsole.MarkupLine("[gray]  \u2328 Utilise les flèches (↑ ↓) et Entrée pour choisir. Tapez pour filtrer.[/]");
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

    public static void ShowTiming(TimeSpan elapsed, string? action = null)
    {
        var label = action ?? "Termine";
        var ms = elapsed.TotalMilliseconds;
        string formatted;
        if (ms < 1000)
            formatted = $"{ms:F0}ms";
        else if (ms < 60000)
            formatted = $"{elapsed.TotalSeconds:F1}s";
        else
            formatted = $"{elapsed.TotalMinutes:F1}min";

        MarkupLine($"  [grey]\u23f1  {Escape(label)} : [white]{formatted}[/][/]");
    }

    public static void ShowCommandResult(string command, int exitCode, string? output = null)
    {
        if (exitCode == 0)
            Success($"{command} \u2014 succ\u00e8s");
        else
            Error($"{command} \u2014 \u00e9chec (code {exitCode})");

        if (!string.IsNullOrWhiteSpace(output) && Verbose)
        {
            foreach (var line in output.Split('\n').Take(10))
                WriteCmdLine(line, exitCode == 0 ? ConsoleColor.Gray : ConsoleColor.Red);
        }
    }

    public static void ShowSection(string title)
    {
        MarkupLine($"\n[bold blue]\u2500\u2500 {Escape(title)} {'\u2500',-40}[/]");
    }

    public static void ShowDiff(string oldText, string newText, string label = "Diff")
    {
        var table = new Table().Border(TableBorder.Rounded).AddColumn("[red]Ancien[/]").AddColumn("[green]Nouveau[/]");
        table.AddRow(Escape(oldText), Escape(newText));
        AnsiConsole.Write(new Rule($"[yellow]{label}[/]"));
        AnsiConsole.Write(table);
    }

    public static async Task<T> ShowSpinner<T>(string message, Func<Task<T>> action)
    {
        T result = default!;
        await AnsiConsole.Status().StartAsync(message, async _ => { result = await action(); });
        return result;
    }

    public static ProgressTask AddProgressTask(ProgressContext ctx, string description, int maxValue = 100)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        return ctx.AddTask(description, new ProgressTaskSettings { MaxValue = maxValue, AutoStart = true });
    }

    private static string Escape(string text) => text?.Replace("[", "[[").Replace("]", "]]") ?? "";
}
