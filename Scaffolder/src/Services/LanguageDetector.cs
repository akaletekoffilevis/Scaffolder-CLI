namespace Scaffolder.Services;

/// <summary>
/// Detection du type de projet a partir des fichiers presents dans le dossier courant
/// (et ses parents proches). Aucun outil externe requis.
/// </summary>
public static class LanguageDetector
{
    private sealed record Indicator(string Pattern, string Language, string Description, int Priority);

    // Ordre = priorite (les fichiers de projet specifiques d'abord, les fichiers ambigus ensuite).
    private static readonly Indicator[] Indicators =
    [
        new("*.csproj", "dotnet", "Projet .NET", 100),
        new("*.fsproj", "dotnet", "Projet .NET (F#)", 100),
        new("*.sln", "dotnet", "Solution .NET", 90),
        new("global.json", "dotnet", "Projet .NET", 80),

        new("package.json", "node", "Projet Node.js", 100),
        new("deno.json", "deno", "Projet Deno", 95),
        new("deno.jsonc", "deno", "Projet Deno", 95),
        new("bun.lockb", "node", "Projet Bun/Node.js", 90),
        new("tsconfig.json", "node", "Projet TypeScript", 80),

        new("Cargo.toml", "rust", "Projet Rust", 100),
        new("go.mod", "go", "Projet Go", 100),

        new("pyproject.toml", "python", "Projet Python", 100),
        new("requirements.txt", "python", "Projet Python", 95),
        new("setup.py", "python", "Projet Python", 95),
        new("Pipfile", "python", "Projet Python", 95),
        new("manage.py", "python", "Projet Django", 90),

        new("composer.json", "php", "Projet PHP", 100),
        new("Gemfile", "ruby", "Projet Ruby", 100),
        new("mix.exs", "elixir", "Projet Elixir", 100),
        new("shard.yml", "crystal", "Projet Crystal", 100),

        new("build.sbt", "scala", "Projet Scala", 100),
        new("*.sbt", "scala", "Projet Scala", 90),
        new("pom.xml", "jvm", "Projet Maven (JVM)", 100),
        new("build.gradle", "jvm", "Projet Gradle (JVM)", 100),
        new("build.gradle.kts", "jvm", "Projet Gradle (JVM)", 100),
        new("settings.gradle", "jvm", "Projet Gradle (JVM)", 80),

        new("pubspec.yaml", "dart", "Projet Dart/Flutter", 100),
        new("Package.swift", "swift", "Projet Swift", 100),
        new("*.xcodeproj", "swift", "Projet Xcode", 90),
        new("build.zig", "zig", "Projet Zig", 100),
        new("build.zig.zon", "zig", "Projet Zig", 100),

        new("cabal.project", "haskell", "Projet Haskell", 100),
        new("*.cabal", "haskell", "Projet Haskell", 95),
        new("stack.yaml", "haskell", "Projet Haskell (Stack)", 90),

        new("CMakeLists.txt", "cpp", "Projet C/C++ (CMake)", 100),
        new("Makefile", "make", "Projet Make", 40),
    ];

    /// <summary>Detecte le type de projet. Remonte jusqu'a 5 dossiers parents si necessaire.</summary>
    public static string Detect(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            return "unknown";

        var current = new DirectoryInfo(Path.GetFullPath(directory));

        for (var depth = 0; depth < 5 && current != null; depth++)
        {
            var detected = DetectInDirectory(current.FullName);
            if (detected != "unknown") return detected;
            current = current.Parent;
        }

        return "unknown";
    }

    /// <summary>Detecte uniquement dans le dossier donne (sans remonter).</summary>
    public static string DetectInDirectory(string directory)
    {
        if (!Directory.Exists(directory)) return "unknown";

        foreach (var indicator in Indicators.OrderByDescending(i => i.Priority))
        {
            try
            {
                if (Directory.EnumerateFiles(directory, indicator.Pattern, SearchOption.TopDirectoryOnly).Any())
                    return indicator.Language;
            }
            catch (IOException)
            {
                // Dossier inaccessible : on continue.
            }
        }

        return "unknown";
    }

    public static string DetectAndDescribe(string directory)
    {
        var lang = Detect(directory);
        return Describe(lang);
    }

    public static string Describe(string language) => language switch
    {
        "dotnet" => "Projet .NET",
        "node" => "Projet Node.js",
        "deno" => "Projet Deno",
        "rust" => "Projet Rust",
        "go" => "Projet Go",
        "python" => "Projet Python",
        "php" => "Projet PHP",
        "ruby" => "Projet Ruby",
        "scala" => "Projet Scala",
        "jvm" => "Projet JVM",
        "dart" => "Projet Dart/Flutter",
        "swift" => "Projet Swift",
        "zig" => "Projet Zig",
        "elixir" => "Projet Elixir",
        "haskell" => "Projet Haskell",
        "crystal" => "Projet Crystal",
        "cpp" => "Projet C/C++",
        "make" => "Projet Make",
        _ => "Type inconnu"
    };

    /// <summary>Retourne tous les types detectes (utile pour les monorepos).</summary>
    public static IReadOnlyList<string> DetectAll(string directory)
    {
        if (!Directory.Exists(directory)) return [];

        var found = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var indicator in Indicators)
        {
            try
            {
                if (Directory.EnumerateFiles(directory, indicator.Pattern, SearchOption.TopDirectoryOnly).Any())
                    found.Add(indicator.Language);
            }
            catch (IOException) { /* ignore */ }
        }

        return found.ToList();
    }

    /// <summary>Cherche le dossier racine du projet (premier dossier contenant un marqueur fort).</summary>
    public static string? FindProjectRoot(string directory)
    {
        if (!Directory.Exists(directory)) return null;

        var current = new DirectoryInfo(Path.GetFullPath(directory));
        for (var depth = 0; depth < 8 && current != null; depth++)
        {
            if (DetectInDirectory(current.FullName) != "unknown")
                return current.FullName;
            current = current.Parent;
        }

        return null;
    }
}
