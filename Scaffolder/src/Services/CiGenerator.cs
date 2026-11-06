namespace Scaffolder.Services;

/// <summary>Genere une configuration CI GitHub Actions adaptee au langage detecte.</summary>
public static class CiGenerator
{
    public static void WriteForProject(string directory, string language)
    {
        var workflowsDir = Path.Combine(directory, ".github", "workflows");
        var path = Path.Combine(workflowsDir, "ci.yml");
        if (File.Exists(path)) return;

        var workflow = language switch
        {
            "dotnet" => DotnetWorkflow(),
            "node" or "deno" => NodeWorkflow(),
            "rust" => RustWorkflow(),
            "go" => GoWorkflow(),
            "python" => PythonWorkflow(),
            "jvm" or "scala" => JvmWorkflow(),
            "php" => PhpWorkflow(),
            "ruby" => RubyWorkflow(),
            "dart" => DartWorkflow(),
            _ => null
        };

        if (workflow == null)
        {
            ConsoleService.Debug($"Aucun workflow CI pour le langage '{language}'.");
            return;
        }

        try
        {
            Directory.CreateDirectory(workflowsDir);
            File.WriteAllText(path, workflow);
            ConsoleService.Debug("Workflow CI genere (.github/workflows/ci.yml).");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ConsoleService.Debug($"Workflow CI non ecrit : {ex.Message}");
        }
    }

    private const string Header = """
        name: CI

        on:
          push:
            branches: [main]
          pull_request:

        jobs:
          build:
            runs-on: ubuntu-latest
            steps:
              - uses: actions/checkout@v4

        """;

    private static string DotnetWorkflow() => Header + """
              - uses: actions/setup-dotnet@v4
                with:
                  dotnet-version: '10.0.x'
              - run: dotnet restore
              - run: dotnet build --no-restore -c Release
              - run: dotnet test --no-build -c Release
        """;

    private static string NodeWorkflow() => Header + """
              - uses: actions/setup-node@v4
                with:
                  node-version: '22'
                  cache: npm
              - run: npm ci
              - run: npm run build --if-present
              - run: npm test --if-present
        """;

    private static string RustWorkflow() => Header + """
              - uses: dtolnay/rust-toolchain@stable
              - run: cargo fmt --check
              - run: cargo clippy -- -D warnings
              - run: cargo test
        """;

    private static string GoWorkflow() => Header + """
              - uses: actions/setup-go@v5
                with:
                  go-version: 'stable'
              - run: go vet ./...
              - run: go test ./...
        """;

    private static string PythonWorkflow() => Header + """
              - uses: actions/setup-python@v5
                with:
                  python-version: '3.12'
              - run: pip install -r requirements.txt || pip install -e .
              - run: python -m pytest || echo "Aucun test configure"
        """;

    private static string JvmWorkflow() => Header + """
              - uses: actions/setup-java@v4
                with:
                  distribution: temurin
                  java-version: '21'
              - run: ./gradlew build --no-daemon
        """;

    private static string PhpWorkflow() => Header + """
              - uses: shivammathur/setup-php@v2
                with:
                  php-version: '8.3'
              - run: composer install --no-interaction --prefer-dist
              - run: vendor/bin/phpunit || echo "Aucun test configure"
        """;

    private static string RubyWorkflow() => Header + """
              - uses: ruby/setup-ruby@v1
                with:
                  ruby-version: '3.3'
                  bundler-cache: true
              - run: bundle exec rake test || echo "Aucun test configure"
        """;

    private static string DartWorkflow() => Header + """
              - uses: dart-lang/setup-dart@v1
              - run: dart pub get
              - run: dart analyze
              - run: dart test || echo "Aucun test configure"
        """;
}
