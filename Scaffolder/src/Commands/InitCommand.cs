using System.CommandLine;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class InitCommand : Command
{
    public InitCommand() : base("init", "Initialise un projet Scaffolder complet (Docker, CI, GitHub, Git)")
    {
        var nameOpt = new Option<string>("--name")
        {
            Description = "Nom du projet"
        };
        var typeOpt = new Option<string>("--type")
        {
            Description = "Type de projet (api, web, full)"
        };
        var langOpt = new Option<string>("--language")
        {
            Description = "Langage du projet"
        };
        var ciOpt = new Option<bool>("--ci")
        {
            Description = "Genere aussi CI/CD"
        };
        var noDockerOpt = new Option<bool>("--no-docker")
        {
            Description = "Ne pas generer Docker"
        };
        var gitUrlOpt = new Option<string>("--git")
        {
            Description = "URL du depot Git a cloner (ex: https://github.com/user/repo.git)"
        };
        Add(nameOpt);
        Add(typeOpt);
        Add(langOpt);
        Add(ciOpt);
        Add(noDockerOpt);
        Add(gitUrlOpt);
        SetAction((ParseResult pr) => HandleInit(
            pr.GetValue(nameOpt), pr.GetValue(typeOpt),
            pr.GetValue(langOpt), pr.GetValue(ciOpt),
            pr.GetValue(noDockerOpt), pr.GetValue(gitUrlOpt)));
    }

    private static int HandleInit(string? name, string? type, string? lang, bool ci, bool noDocker, string? gitUrl)
    {
        var cwd = Directory.GetCurrentDirectory();

        // If --git is provided, clone the repo first
        if (!string.IsNullOrWhiteSpace(gitUrl))
        {
            ConsoleService.Info($"Clonage du depot : {gitUrl}");
            var cloneResult = ProcessService.RunAsync("git", $"clone {gitUrl}", Directory.GetParent(cwd)?.FullName ?? cwd).Result;
            if (cloneResult.ExitCode != 0)
            {
                ConsoleService.Error("Echec du clonage.");
                return 1;
            }

            var repoName = Path.GetFileNameWithoutExtension(gitUrl) ?? "repo";
            if (gitUrl.EndsWith(".git"))
                repoName = Path.GetFileNameWithoutExtension(gitUrl);
            cwd = Path.Combine(Directory.GetParent(cwd)?.FullName ?? cwd, repoName);
            if (!Directory.Exists(cwd))
            {
                ConsoleService.Error($"Dossier '{repoName}' introuvable apres clonage.");
                return 1;
            }

            ConsoleService.Success($"Depot clone dans {cwd}");
        }

        name ??= new DirectoryInfo(cwd).Name;
        type ??= "api";
        lang ??= DetectLanguage(cwd);

        ConsoleService.ShowLogo();
        Console.WriteLine();
        ConsoleService.Info($"Initialisation de '{name}' ({type}, {lang})");
        Console.WriteLine();

        if (!noDocker)
        {
            ConsoleService.Info("Generation des fichiers Docker...");
            GenerateDocker(type, cwd);
        }

        if (ci)
        {
            ConsoleService.Info("Generation du workflow CI/CD...");
            GenerateCI(lang, cwd);
        }

        ConsoleService.Info("Initialisation Git...");
        GitInit(cwd);

        ConsoleService.Success($"Projet '{name}' initialise !");
        ConsoleService.Info($"  {cwd}");
        return 0;
    }

    private static void GenerateDocker(string type, string dir)
    {
        var dockerDir = Path.Combine(dir, "docker");
        Directory.CreateDirectory(dockerDir);

        File.WriteAllText(Path.Combine(dockerDir, "Dockerfile"), GenerateDockerfile(type));
        File.WriteAllText(Path.Combine(dockerDir, ".dockerignore"), GenerateDockerignore());
        File.WriteAllText(Path.Combine(dockerDir, "docker-compose.yml"), GenerateCompose(type));
        ConsoleService.Success("  Fichiers Docker generes.");
    }

    private static string GenerateDockerfile(string type) => type switch
    {
        "web" => """
FROM node:22-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build
FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
""",
        _ => """
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app
COPY *.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o out
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out .
EXPOSE 8080
ENTRYPOINT ["dotnet", "app.dll"]
"""
    };

    private static string GenerateDockerignore() => """
**/.git
**/node_modules/
**/bin/
**/obj/
**/target/
**/__pycache__/
""";

    private static string GenerateCompose(string type) => type == "full" ? """
services:
  api:
    build:
      context: .
      dockerfile: docker/Dockerfile.api
    ports:
      - "8080:8080"
  web:
    build:
      context: .
      dockerfile: docker/Dockerfile.web
    ports:
      - "80:80"
  db:
    image: postgres:16-alpine
    environment:
      POSTGRES_PASSWORD: secret
""" : """
services:
  app:
    build:
      context: .
      dockerfile: docker/Dockerfile
    ports:
      - "8080:8080"
  db:
    image: postgres:16-alpine
    environment:
      POSTGRES_PASSWORD: secret
""";

    private static void GenerateCI(string lang, string dir)
    {
        var workflow = lang switch
        {
            "node" or "npm" => GenerateNodeWorkflow(),
            "python" => GeneratePythonWorkflow(),
            "rust" or "cargo" => GenerateRustWorkflow(),
            "go" or "golang" => GenerateGoWorkflow(),
            _ => GenerateDotnetWorkflow()
        };

        var workflowsDir = Path.Combine(dir, ".github", "workflows");
        Directory.CreateDirectory(workflowsDir);
        File.WriteAllText(Path.Combine(workflowsDir, "ci.yml"), workflow.TrimStart('\n'));
        ConsoleService.Success("  Workflow CI/CD genere.");
    }

    private static string GenerateDotnetWorkflow() => """
name: CI
on: [push, pull_request]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: "9.0" }
      - run: dotnet restore && dotnet build && dotnet test
""";

    private static string GenerateNodeWorkflow() => """
name: CI
on: [push, pull_request]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with: { node-version: "22" }
      - run: npm ci && npm run build && npm test
""";

    private static string GeneratePythonWorkflow() => """
name: CI
on: [push, pull_request]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-python@v5
        with: { python-version: "3.12" }
      - run: pip install -r requirements.txt && pytest
""";

    private static string GenerateRustWorkflow() => """
name: CI
on: [push, pull_request]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions-rust-lang/setup-rust-toolchain@v1
      - run: cargo build && cargo test && cargo clippy
""";

    private static string GenerateGoWorkflow() => """
name: CI
on: [push, pull_request]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-go@v5
        with: { go-version: "1.26" }
      - run: go build ./... && go test ./...
""";

    private static string DetectLanguage(string dir)
    {
        if (Directory.GetFiles(dir, "*.csproj").Length > 0) return "dotnet";
        if (File.Exists(Path.Combine(dir, "package.json"))) return "node";
        if (File.Exists(Path.Combine(dir, "Cargo.toml"))) return "rust";
        if (File.Exists(Path.Combine(dir, "go.mod"))) return "go";
        return "dotnet";
    }

    private static void GitInit(string dir)
    {
        if (Directory.Exists(Path.Combine(dir, ".git")))
        {
            ConsoleService.Info("  Git deja initialise.");
            return;
        }

        ProcessService.RunAsync("git", "init", workingDirectory: dir, streamOutput: false)
            .GetAwaiter().GetResult();
        ProcessService.RunAsync("git", "add .", workingDirectory: dir, streamOutput: false)
            .GetAwaiter().GetResult();
        ProcessService.RunAsync("git", "commit -m \"Initial commit with Scaffolder\" --allow-empty",
            workingDirectory: dir, streamOutput: false).GetAwaiter().GetResult();
        ConsoleService.Success("  Depot Git initialise.");
    }
}
