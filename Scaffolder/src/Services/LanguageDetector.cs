namespace Scaffolder.Services;

public static class LanguageDetector
{
    private static readonly (string File, string Language)[] Indicators =
    [
        ("*.csproj", "dotnet"),
        ("*.sln", "dotnet"),
        ("global.json", "dotnet"),
        ("package.json", "node"),
        ("tsconfig.json", "node"),
        ("Cargo.toml", "rust"),
        ("go.mod", "go"),
        ("requirements.txt", "python"),
        ("pyproject.toml", "python"),
        ("setup.py", "python"),
        ("Pipfile", "python"),
        ("Gemfile", "ruby"),
        ("*.sbt", "scala"),
        ("build.gradle", "jvm"),
        ("build.gradle.kts", "jvm"),
        ("pubspec.yaml", "dart"),
        ("Package.swift", "swift"),
        ("build.zig", "zig"),
        ("mix.exs", "elixir"),
        ("cabal.project", "haskell"),
        ("shard.yml", "crystal"),
        ("*.csproj", "csharp"),
        ("*.fsproj", "fsharp"),
    ];

    public static string Detect(string directory)
    {
        if (!Directory.Exists(directory))
            return "unknown";

        foreach (var (file, language) in Indicators)
        {
            var matches = Directory.GetFiles(directory, file, SearchOption.TopDirectoryOnly);
            if (matches.Length > 0)
                return language;
        }

        return "unknown";
    }

    public static string DetectAndDescribe(string directory)
    {
        var lang = Detect(directory);
        return lang switch
        {
            "dotnet" or "csharp" or "fsharp" => "Projet .NET",
            "node" => "Projet Node.js",
            "rust" => "Projet Rust",
            "go" => "Projet Go",
            "python" => "Projet Python",
            "ruby" => "Projet Ruby",
            "scala" => "Projet Scala",
            "jvm" => "Projet JVM",
            "dart" => "Projet Dart/Flutter",
            "swift" => "Projet Swift",
            "zig" => "Projet Zig",
            "elixir" => "Projet Elixir",
            "haskell" => "Projet Haskell",
            "crystal" => "Projet Crystal",
            _ => "Type inconnu"
        };
    }
}
