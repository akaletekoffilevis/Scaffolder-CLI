using System.CommandLine;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class CompareCommand : Command
{
    public CompareCommand() : base("compare", "Compare deux templates cote-a-cote")
    {
        var tpl1Arg = new Argument<string>("template1") { Description = "Premier template" };
        var tpl2Arg = new Argument<string>("template2") { Description = "Second template" };
        Add(tpl1Arg);
        Add(tpl2Arg);

        SetAction((ParseResult pr) => HandleCompare(
            pr.GetValue(tpl1Arg), pr.GetValue(tpl2Arg)));
    }

    private static int HandleCompare(string? tpl1, string? tpl2)
    {
        if (string.IsNullOrWhiteSpace(tpl1) || string.IsNullOrWhiteSpace(tpl2))
        {
            ConsoleService.Error("Usage : scaffold compare <template1> <template2>");
            ConsoleService.Info("Exemple : scaffold compare react vue");
            return 1;
        }

        var all = GetAllTemplates();
        var a = all.FirstOrDefault(t => t.Name.Equals(tpl1, StringComparison.OrdinalIgnoreCase));
        var b = all.FirstOrDefault(t => t.Name.Equals(tpl2, StringComparison.OrdinalIgnoreCase));

        if (a.Name == null || b.Name == null)
        {
            ConsoleService.Warning("Template(s) introuvable(s).");
            if (a.Name == null) ConsoleService.Error($"  '{tpl1}' introuvable");
            if (b.Name == null) ConsoleService.Error($"  '{tpl2}' introuvable");
            return 1;
        }

        var width = 35;
        var sep = new string(' ', 3);

        ConsoleService.Info($"Comparaison : {a.Name} vs {b.Name}");
        Console.WriteLine();

        PrintRow("Description", a.Description, b.Description, width, sep);
        PrintRow("Tags", string.Join(", ", a.Tags), string.Join(", ", b.Tags), width, sep);
        PrintRow("Langage", a.Lang, b.Lang, width, sep);
        PrintRow("Type", a.Type, b.Type, width, sep);
        PrintRow("Difficulte", a.Difficulty, b.Difficulty, width, sep);

        Console.WriteLine();
        ConsoleService.Info($"  scaffold new --template={a.Name,-15}  |  scaffold new --template={b.Name}");
        return 0;
    }

    private static void PrintRow(string label, string val1, string val2, int width, string sep)
    {
        Console.Write($"  {label,-12}");
        Console.Write(val1.Length > width ? val1[..(width - 3)] + "..." : val1.PadRight(width));
        Console.Write(sep);
        Console.WriteLine(val2.Length > width - 3 ? val2[..(width - 3)] + "..." : val2);
    }

    private static (string Name, string Description, string[] Tags, string Lang, string Type, string Difficulty)[] GetAllTemplates()
    {
        return new (string, string, string[], string, string, string)[]
        {
            ("react", "Bibliotheque UI React avec JSX et composants", ["js", "ts", "frontend", "spa"], "JavaScript/TypeScript", "SPA", "Intermediaire"),
            ("vue", "Framework progressif Vue 3 avec Composition API", ["js", "ts", "frontend", "spa"], "JavaScript/TypeScript", "SPA", "Facile"),
            ("svelte", "Compilateur Svelte avec zero runtime", ["js", "ts", "frontend", "spa"], "JavaScript/TypeScript", "SPA", "Facile"),
            ("next", "Framework Next.js avec SSR et App Router", ["js", "ts", "frontend", "ssr"], "JavaScript/TypeScript", "SSR/SSG", "Intermediaire"),
            ("nuxt", "Framework Nuxt 3 avec SSR auto", ["js", "ts", "frontend", "ssr"], "JavaScript/TypeScript", "SSR/SSG", "Intermediaire"),
            ("vite", "Build tool Vite avec HMR instantane", ["js", "ts", "frontend"], "JavaScript/TypeScript", "Build tool", "Facile"),
            ("webapi", "API REST ASP.NET Core minimal API", ["dotnet", "api", "backend"], "C#", "API REST", "Intermediaire"),
            ("blazor", "Blazor WebAssembly avec C# cote client", ["dotnet", "wasm", "frontend"], "C#", "WASM", "Avance"),
            ("flask", "Micro-framework Flask Python", ["python", "api", "backend"], "Python", "API", "Facile"),
            ("fastapi", "Framework FastAPI Python asynchrone", ["python", "api", "backend"], "Python", "API", "Intermediaire"),
            ("express", "Framework Express Node.js minimal", ["js", "api", "backend"], "JavaScript/TypeScript", "API", "Facile"),
            ("fastify", "Framework Fastify Node.js rapide", ["js", "api", "backend"], "JavaScript/TypeScript", "API", "Intermediaire"),
            ("laravel", "Framework PHP Laravel MVC", ["php", "backend", "web"], "PHP", "MVC", "Intermediaire"),
            ("symfony", "Framework PHP Symfony modulaire", ["php", "backend", "web"], "PHP", "MVC", "Avance"),
            ("rails", "Ruby on Rails MVC convention-over-config", ["ruby", "backend", "web"], "Ruby", "MVC", "Intermediaire"),
            ("django", "Framework Python Django batteries-included", ["python", "backend", "web"], "Python", "MVC", "Intermediaire"),
            ("cargo", "Projet Rust cargo", ["rust", "backend", "cli"], "Rust", "CLI/Library", "Avance"),
            ("go", "Module Go", ["go", "backend", "cli"], "Go", "CLI/Library", "Facile"),
            ("flutter", "Framework UI Flutter/Dart multi-platforme", ["dart", "mobile", "frontend"], "Dart", "Mobile/Web", "Intermediaire"),
        };
    }
}
