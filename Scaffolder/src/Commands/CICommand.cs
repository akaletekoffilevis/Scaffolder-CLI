using System.CommandLine;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class CICommand : Command
{
    public CICommand() : base("ci", "Genere des fichiers CI/CD (GitHub Actions, GitLab CI, Jenkins)")
    {
        var providerOpt = new Option<string>("--provider")
        {
            Description = "Fournisseur CI (github, gitlab, jenkins)"
        };
        var langOpt = new Option<string>("--language")
        {
            Description = "Langage du projet (auto-detecte si omis)"
        };
        var outputOpt = new Option<DirectoryInfo?>("--output")
        {
            Description = "Dossier de sortie"
        };

        Add(providerOpt);
        Add(langOpt);
        Add(outputOpt);

        SetAction((ParseResult pr) => HandleCI(
            pr.GetValue(providerOpt), pr.GetValue(langOpt), pr.GetValue(outputOpt)));
    }

    private static int HandleCI(string? provider, string? language, DirectoryInfo? output)
    {
        var cwd = output?.FullName ?? Directory.GetCurrentDirectory();
        var lang = language?.ToLowerInvariant() ?? DetectLanguage(cwd);

        provider ??= DetectCIProvider(cwd);

        ConsoleService.Info("Generation de la configuration CI/CD...");
        ConsoleService.SummaryLine("Provider", provider);
        ConsoleService.SummaryLine("Langage", lang);
        Console.WriteLine();

        return provider.ToLowerInvariant() switch
        {
            "github" or "gh" or "github-actions" => GenerateGitHubActions(cwd, lang),
            "gitlab" or "gl" => GenerateGitLabCI(cwd, lang),
            "jenkins" or "jenkinsfile" => GenerateJenkinsfile(cwd, lang),
            _ => GenerateGitHubActions(cwd, lang)
        };
    }

    private static string DetectCIProvider(string cwd)
    {
        if (Directory.Exists(Path.Combine(cwd, ".github")))
            return "github";
        if (File.Exists(Path.Combine(cwd, ".gitlab-ci.yml")))
            return "gitlab";
        if (File.Exists(Path.Combine(cwd, "Jenkinsfile")))
            return "jenkins";
        return "github";
    }

    private static string DetectLanguage(string dir)
    {
        if (Directory.GetFiles(dir, "*.csproj").Length > 0) return "dotnet";
        if (File.Exists(Path.Combine(dir, "package.json"))) return "node";
        if (File.Exists(Path.Combine(dir, "Cargo.toml"))) return "rust";
        if (File.Exists(Path.Combine(dir, "go.mod"))) return "go";
        if (File.Exists(Path.Combine(dir, "requirements.txt")) || File.Exists(Path.Combine(dir, "pyproject.toml"))) return "python";
        if (File.Exists(Path.Combine(dir, "Gemfile"))) return "ruby";
        if (File.Exists(Path.Combine(dir, "build.gradle")) || File.Exists(Path.Combine(dir, "build.gradle.kts"))) return "jvm";
        return "dotnet";
    }

    private static int GenerateGitHubActions(string cwd, string lang)
    {
        var dir = Path.Combine(cwd, ".github", "workflows");
        Directory.CreateDirectory(dir);

        var workflow = lang switch
        {
            "dotnet" => GenerateDotnetGH(),
            "node" or "npm" => GenerateNodeGH(),
            "python" => GeneratePythonGH(),
            "rust" or "cargo" => GenerateRustGH(),
            "go" or "golang" => GenerateGoGH(),
            "ruby" => GenerateRubyGH(),
            "jvm" => GenerateJvmGH(),
            _ => GenerateDotnetGH()
        };

        File.WriteAllText(Path.Combine(dir, "ci.yml"), workflow);
        ConsoleService.Success("GitHub Actions genere : .github/workflows/ci.yml");
        return 0;
    }

    private static int GenerateGitLabCI(string cwd, string lang)
    {
        var yml = lang switch
        {
            "dotnet" => """
            stages:
              - build
              - test

            build:
              stage: build
              image: mcr.microsoft.com/dotnet/sdk:10.0
              script:
                - dotnet restore
                - dotnet build --no-restore
              artifacts:
                paths:
                  - bin/
                  - obj/

            test:
              stage: test
              image: mcr.microsoft.com/dotnet/sdk:10.0
              script:
                - dotnet test --no-build
            """,
            "node" or "npm" => """
            stages:
              - install
              - build
              - test

            install:
              stage: install
              image: node:22
              script:
                - npm ci
              artifacts:
                paths:
                  - node_modules/

            build:
              stage: build
              image: node:22
              script:
                - npm run build
              artifacts:
                paths:
                  - dist/

            test:
              stage: test
              image: node:22
              script:
                - npm test
            """,
            "python" => """
            stages:
              - install
              - test

            install:
              stage: install
              image: python:3.12
              script:
                - pip install -r requirements.txt

            test:
              stage: test
              image: python:3.12
              script:
                - pytest
            """,
            "rust" or "cargo" => """
            stages:
              - build
              - test

            build:
              stage: build
              image: rust:latest
              script:
                - cargo build --release
              artifacts:
                paths:
                  - target/

            test:
              stage: test
              image: rust:latest
              script:
                - cargo test
                - cargo clippy
            """,
            "go" or "golang" => """
            stages:
              - build
              - test

            build:
              stage: build
              image: golang:1.22
              script:
                - go build ./...

            test:
              stage: test
              image: golang:1.22
              script:
                - go test ./...
            """,
            _ => """
            stages:
              - build

            build:
              stage: build
              image: ubuntu:latest
              script:
                - echo "Build step"
            """
        };

        File.WriteAllText(Path.Combine(cwd, ".gitlab-ci.yml"), yml);
        ConsoleService.Success("GitLab CI genere : .gitlab-ci.yml");
        return 0;
    }

    private static int GenerateJenkinsfile(string cwd, string lang)
    {
        var pipeline = lang switch
        {
            "dotnet" => """
            pipeline {
                agent any
                stages {
                    stage('Restore') {
                        steps {
                            sh 'dotnet restore'
                        }
                    }
                    stage('Build') {
                        steps {
                            sh 'dotnet build --no-restore'
                        }
                    }
                    stage('Test') {
                        steps {
                            sh 'dotnet test --no-build'
                        }
                    }
                }
                post {
                    always {
                        cleanWs()
                    }
                }
            }
            """,
            "node" or "npm" => """
            pipeline {
                agent any
                tools { nodejs 'node-22' }
                stages {
                    stage('Install') {
                        steps {
                            sh 'npm ci'
                        }
                    }
                    stage('Build') {
                        steps {
                            sh 'npm run build'
                        }
                    }
                    stage('Test') {
                        steps {
                            sh 'npm test'
                        }
                    }
                }
                post {
                    always {
                        cleanWs()
                    }
                }
            }
            """,
            "python" => """
            pipeline {
                agent any
                stages {
                    stage('Install') {
                        steps {
                            sh 'pip install -r requirements.txt'
                        }
                    }
                    stage('Test') {
                        steps {
                            sh 'pytest'
                        }
                    }
                }
                post {
                    always {
                        cleanWs()
                    }
                }
            }
            """,
            _ => """
            pipeline {
                agent any
                stages {
                    stage('Build') {
                        steps {
                            sh 'echo "Build step"'
                        }
                    }
                }
                post {
                    always {
                        cleanWs()
                    }
                }
            }
            """
        };

        File.WriteAllText(Path.Combine(cwd, "Jenkinsfile"), pipeline);
        ConsoleService.Success("Jenkinsfile genere : Jenkinsfile");
        return 0;
    }

    private static string GenerateDotnetGH() => """
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
          dotnet-version: "10.0"
      - run: dotnet restore
      - run: dotnet build --no-restore
      - run: dotnet test --no-build
""";

    private static string GenerateNodeGH() => """
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

    private static string GeneratePythonGH() => """
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

    private static string GenerateRustGH() => """
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

    private static string GenerateGoGH() => """
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
          go-version: "1.22"
      - run: go build ./...
      - run: go test ./...
""";

    private static string GenerateRubyGH() => """
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
      - uses: ruby/setup-ruby@v1
        with:
          ruby-version: "3.3"
          bundler-cache: true
      - run: bundle exec rspec
""";

    private static string GenerateJvmGH() => """
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
      - uses: actions/setup-java@v4
        with:
          java-version: "21"
          distribution: "temurin"
      - uses: gradle/actions/setup-gradle@v4
      - run: gradle build
      - run: gradle test
""";
}
