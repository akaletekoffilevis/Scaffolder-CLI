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
            WriteLine("  🔍 " + text, ConsoleColor.DarkGray);
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
        WriteLine(" ✅ " + text, ConsoleColor.Green);
    }

    public static void Error(string text)
    {
        WriteLine(" ❌ " + text, ConsoleColor.Red);
    }

    public static void Warning(string text)
    {
        WriteLine(" ⚠️  " + text, ConsoleColor.Yellow);
    }

    public static void Info(string text)
    {
        WriteLine(" ℹ️  " + text, ConsoleColor.Cyan);
    }

    public static void Highlight(string text)
    {
        Write(text, ConsoleColor.Magenta);
    }

    public static void ShowLogo()
    {
        WriteLine(@"
   ╔══════════════════════════════════════════╗
   ║        🏗️  S C A F F O L D E R          ║
   ║    CLI universel pour générer des projets ║
   ╚══════════════════════════════════════════╝", ConsoleColor.Cyan);
    }

    public static void CheckFirstRun()
    {
        if (File.Exists(ConfigFile)) return;

        ShowLogo();
        WriteLine();
        WriteLine("👋  Bienvenue dans Scaffolder !", ConsoleColor.Green);
        WriteLine();
        Info("Je vais t'aider à créer ton premier projet en 30 secondes.");
        Info("Scaffolder fonctionne avec tous les langages : C#, Python, JS, Rust, Go...");
        WriteLine();
        WriteLine("📖  Tape `scaffold --help` pour voir toutes les commandes.", ConsoleColor.Yellow);
        WriteLine("🚀  Tape `scaffold new` pour créer ton premier projet.", ConsoleColor.Green);
        WriteLine();

        Directory.CreateDirectory(ConfigDir);
        File.WriteAllText(ConfigFile, """
        {
          "firstRun": false,
          "theme": "default",
          "experience": "beginner"
        }
        """);
    }

    public static string Prompt(string question, string defaultValue = "")
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

        Write(question + " ", ConsoleColor.Cyan);
        try
        {
            var input = Console.ReadLine() ?? "";
            return string.IsNullOrWhiteSpace(input) ? defaultValue : input.Trim();
        }
        catch (InvalidOperationException)
        {
            WriteLine($"  > {defaultValue}", ConsoleColor.Green);
            return defaultValue;
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

        var selected = 0;
        ConsoleKey key;

        WriteLine(question, ConsoleColor.Cyan);

        var top = Console.CursorTop;
        var left = Console.CursorLeft;

        for (int i = 0; i < options.Length; i++)
        {
            Console.SetCursorPosition(left, top + i);
            var prefix = i == selected ? " ▶" : "  ";
            WriteLine($" {prefix} {options[i]}", i == selected ? ConsoleColor.Green : ConsoleColor.Gray);
        }

        try
        {
            do
            {
                key = Console.ReadKey(true).Key;

                var prevSelected = selected;

                switch (key)
                {
                    case ConsoleKey.UpArrow:
                        selected = Math.Max(0, selected - 1);
                        break;
                    case ConsoleKey.DownArrow:
                        selected = Math.Min(options.Length - 1, selected + 1);
                        break;
                    case ConsoleKey.Enter:
                        Console.SetCursorPosition(left, top + options.Length);
                        return options[selected];
                }

                if (prevSelected != selected)
                {
                    Console.SetCursorPosition(left, top + prevSelected);
                    WriteLine($"    {options[prevSelected]}", ConsoleColor.Gray);
                    Console.SetCursorPosition(left, top + selected);
                    WriteLine($"  ▶ {options[selected]}", ConsoleColor.Green);
                }
            } while (true);
        }
        catch (InvalidOperationException)
        {
            WriteLine($"  > {options[0]}", ConsoleColor.Green);
            return options[0];
        }
    }

    public static void WriteCmdLine(string text, ConsoleColor? color = null)
    {
        WriteLine($"    $ {text}", color ?? ConsoleColor.DarkGray);
    }

    public static async Task ShowSpinner(string message, Func<Task> action)
    {
        var frames = new[] { '⠋', '⠙', '⠹', '⠸', '⠼', '⠴', '⠦', '⠧', '⠇', '⠏' };
        var index = 0;
        var top = Console.CursorTop;
        var left = Console.CursorLeft;

        var task = action();

        while (!task.IsCompleted)
        {
            Console.SetCursorPosition(left, top);
            Write($" {frames[index % frames.Length]} {message}...  ", ConsoleColor.Cyan);
            index++;
            await Task.Delay(80);
        }

        await task;

        Console.SetCursorPosition(left, top);
        WriteLine($" ✅ {message} — terminé", ConsoleColor.Green);
    }
}
