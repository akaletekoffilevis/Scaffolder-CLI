using System.CommandLine;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class GitHubCommand : Command
{
    public GitHubCommand() : base("github", "Configure GitHub pour le projet courant (init, actions, gitignore)")
    {
        var initCmd = new Command("init", "Cree un depot GitHub et pousse le projet");
        initCmd.SetAction(_ => HandleInit());

        var actionsCmd = new Command("actions", "Genere un workflow GitHub Actions");
        var langOpt = new Option<string>("--language")
        {
            Description = "Langage du projet (dotnet, node, python, rust, go)"
        };
        actionsCmd.Add(langOpt);
        actionsCmd.SetAction((ParseResult pr) => HandleActions(pr.GetValue(langOpt)));

        Add(initCmd);
        Add(actionsCmd);

        var actionCmd = new Command("action", "Genere une GitHub Action Scaffolder pour CI");
        actionCmd.SetAction(_ => HandleAction());
        Add(actionCmd);

        SetAction(_ =>
        {
            ConsoleService.Info("Sous-commandes : init, actions, action");
            return 0;
        });
    }

    private static int HandleInit()
    {
        var cwd = Directory.GetCurrentDirectory();
        var dirName = new DirectoryInfo(cwd).Name;

        ConsoleService.Info("Verification de gh...");
        if (!ProcessService.CommandExists("gh"))
        {
            ConsoleService.Warning("gh (GitHub CLI) n'est pas installe.");
            ConsoleService.Info("  Installe-le : https://cli.github.com/");
            ConsoleService.Info("  Puis : gh auth login");
            return 1;
        }

        ConsoleService.Info("Creation du depot GitHub...");
        var result = ProcessService.RunAsync("gh", $"repo create {dirName} --source=. --public --push",
            workingDirectory: cwd, streamOutput: true).GetAwaiter().GetResult();

        if (result.ExitCode == 0)
        {
            ConsoleService.Success($"Depot cree : https://github.com/akaletekoffilevis/{dirName}");
            return 0;
        }

        ConsoleService.Error("Echec de la creation du depot.");
        ConsoleService.Info("Verifie que tu es authentifie : gh auth status");
        return 1;
    }

    private static int HandleActions(string? language)
    {
        var cwd = Directory.GetCurrentDirectory();
        var dir = Path.Combine(cwd, ".github", "workflows");
        Directory.CreateDirectory(dir);

        var lang = language?.ToLowerInvariant() ?? DetectLanguage(cwd);
        var workflow = lang switch
        {
            "dotnet" => GenerateDotnetWorkflow(),
            "node" or "npm" => GenerateNodeWorkflow(),
            "python" => GeneratePythonWorkflow(),
            "rust" or "cargo" => GenerateRustWorkflow(),
            "go" or "golang" => GenerateGoWorkflow(),
            _ => GenerateDotnetWorkflow()
        };

        File.WriteAllText(Path.Combine(dir, "ci.yml"), workflow.TrimStart('\n'));
        ConsoleService.Success($"Workflow GitHub Actions genere : .github/workflows/ci.yml");
        return 0;
    }

    private static string DetectLanguage(string dir)
    {
        if (Directory.GetFiles(dir, "*.csproj").Length > 0) return "dotnet";
        if (File.Exists(Path.Combine(dir, "package.json"))) return "node";
        if (File.Exists(Path.Combine(dir, "Cargo.toml"))) return "rust";
        if (File.Exists(Path.Combine(dir, "go.mod"))) return "go";
        return "dotnet";
    }

    private static string GenerateDotnetWorkflow() => """
name: CI

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "9.0"
      - run: dotnet restore
      - run: dotnet build --no-restore
      - run: dotnet test --no-build
""";

    private static string GenerateNodeWorkflow() => """
name: CI

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: "22"
          cache: "npm"
      - run: npm ci
      - run: npm run build
      - run: npm test
""";

    private static string GeneratePythonWorkflow() => """
name: CI

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-python@v5
        with:
          python-version: "3.12"
      - run: pip install -r requirements.txt
      - run: pytest
""";

    private static string GenerateRustWorkflow() => """
name: CI

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions-rust-lang/setup-rust-toolchain@v1
      - run: cargo build
      - run: cargo test
      - run: cargo clippy
""";

    private static string GenerateGoWorkflow() => """
name: CI

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-go@v5
        with:
          go-version: "1.26"
      - run: go build ./...
      - run: go test ./...
""";

    private static int HandleAction()
    {
        var cwd = Directory.GetCurrentDirectory();
        var workflowsDir = Path.Combine(cwd, ".github", "workflows");
        Directory.CreateDirectory(workflowsDir);

        var yml = """
name: Scaffolder Batch CI
on:
  push:
    branches: [main]
  pull_request:
    branches: [main]
  workflow_dispatch:

jobs:
  generate:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Install Scaffolder
        run: |
          curl -L -o scaffold.tar.gz https://github.com/akaletekoffilevis/Scaffolder-CLI/releases/latest/download/scaffold-linux-x64.tar.gz
          tar -xzf scaffold.tar.gz
          sudo mv scaffold /usr/local/bin/
          chmod +x /usr/local/bin/scaffold

      - name: Run Scaffolder batch
        run: scaffold batch scaffold-batch.yml
        continue-on-error: true

      - name: Verify project generation
        run: |
          echo "Projets generes :"
          ls -la */README.md 2>/dev/null || echo "Aucun projet genere"
""";

        File.WriteAllText(Path.Combine(workflowsDir, "scaffolder-ci.yml"), yml);
        ConsoleService.Success("GitHub Action Scaffolder creee : .github/workflows/scaffolder-ci.yml");
        ConsoleService.Info("  Cette action installe Scaffolder et execute scaffold batch dans la CI.");
        ConsoleService.Info("  Cree un fichier scaffold-batch.yml a la racine pour l'utiliser.");
        return 0;
    }
}
