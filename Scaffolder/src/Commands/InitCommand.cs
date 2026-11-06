using System.CommandLine;
using System.Text;
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
            Description = "Langage du projet (detecte automatiquement si absent)"
        };
        var ciOpt = new Option<bool>("--ci")
        {
            Description = "Genere aussi le workflow CI/CD"
        };
        var noDockerOpt = new Option<bool>("--no-docker")
        {
            Description = "Ne pas generer Docker"
        };
        var noGitOpt = new Option<bool>("--no-git")
        {
            Description = "Ne pas initialiser Git"
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
        Add(noGitOpt);
        Add(gitUrlOpt);

        SetAction((ParseResult pr) => HandleInit(
            pr.GetValue(nameOpt), pr.GetValue(typeOpt),
            pr.GetValue(langOpt), pr.GetValue(ciOpt),
            pr.GetValue(noDockerOpt), pr.GetValue(noGitOpt), pr.GetValue(gitUrlOpt)));
    }

    private static int HandleInit(
        string? name, string? type, string? lang, bool ci, bool noDocker, bool noGit, string? gitUrl)
    {
        var baseDir = Directory.GetCurrentDirectory();

        if (!string.IsNullOrWhiteSpace(gitUrl))
        {
            ConsoleService.Info($"Clonage du depot : {gitUrl}");

            var repoName = InferRepoName(gitUrl);
            var destination = Path.Combine(baseDir, repoName);

            if (Directory.Exists(destination) && Directory.EnumerateFileSystemEntries(destination).Any())
            {
                ConsoleService.Error($"Le dossier '{destination}' existe deja et n'est pas vide.");
                return 1;
            }

            var (ok, message) = GitService.CloneAsync(gitUrl, destination, depth: 0).GetAwaiter().GetResult();
            if (!ok)
            {
                ConsoleService.Error($"Echec du clonage : {message}");
                return 1;
            }

            ConsoleService.Success($"Depot clone dans {destination}");
            baseDir = destination;
        }

        var cwd = baseDir;
        name ??= new DirectoryInfo(cwd).Name;
        type = (type ?? "api").ToLowerInvariant();
        lang ??= DetectLanguage(cwd);

        if (type is not ("api" or "web" or "full"))
        {
            ConsoleService.Warning($"Type '{type}' inconnu, utilisation de 'api'.");
            type = "api";
        }

        ConsoleService.ShowLogo();
        Console.WriteLine();
        ConsoleService.Info($"Initialisation de '{name}' ({type}, {lang})");
        Console.WriteLine();

        if (!noDocker)
        {
            ConsoleService.Info("Generation des fichiers Docker...");
            GenerateDocker(type, lang, cwd, name);
        }

        if (ci)
        {
            ConsoleService.Info("Generation du workflow CI/CD...");
            CiGenerator.WriteForProject(cwd, NormalizeLanguage(lang));
            ConsoleService.Success("  Workflow CI/CD genere.");
        }

        if (!noGit)
        {
            ConsoleService.Info("Initialisation Git...");
            GitService.InitWithCommitAsync(cwd).GetAwaiter().GetResult();
        }

        ConsoleService.Success($"Projet '{name}' initialise !");
        ConsoleService.Info($"  {cwd}");
        return 0;
    }

    private static string InferRepoName(string gitUrl)
    {
        var trimmed = gitUrl.TrimEnd('/', '\\');
        var last = trimmed.Split('/', '\\').LastOrDefault() ?? "repo";
        if (last.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            last = last[..^4];

        var invalid = Path.GetInvalidFileNameChars();
        var clean = new string(last.Select(c => invalid.Contains(c) ? '-' : c).ToArray());
        return string.IsNullOrWhiteSpace(clean) ? "repo" : clean;
    }

    private static string NormalizeLanguage(string lang) => lang.ToLowerInvariant() switch
    {
        "c#" or "csharp" or "dotnet" => "dotnet",
        "js" or "javascript" or "ts" or "typescript" or "npm" => "node",
        "rust" or "cargo" => "rust",
        "go" or "golang" => "go",
        "py" or "python" => "python",
        _ => lang.ToLowerInvariant()
    };

    private static void GenerateDocker(string type, string lang, string dir, string name)
    {
        var dockerDir = Path.Combine(dir, "docker");
        Directory.CreateDirectory(dockerDir);

        var normalized = NormalizeLanguage(lang);

        if (type == "full")
        {
            // Le compose "full" reference deux images distinctes : on cree les deux Dockerfiles.
            File.WriteAllText(
                Path.Combine(dockerDir, "Dockerfile.api"),
                DockerfileFor(normalized, "api", name));
            File.WriteAllText(
                Path.Combine(dockerDir, "Dockerfile.web"),
                DockerfileFor("node", "web", name));
        }
        else
        {
            File.WriteAllText(
                Path.Combine(dockerDir, "Dockerfile"),
                DockerfileFor(type == "web" ? "node" : normalized, type, name));
        }

        File.WriteAllText(Path.Combine(dockerDir, ".dockerignore"), GenerateDockerignore());
        File.WriteAllText(Path.Combine(dockerDir, "docker-compose.yml"), GenerateCompose(type, normalized, name));

        ConsoleService.Success("  Fichiers Docker generes.");
    }

    private static string DockerfileFor(string lang, string type, string name)
    {
        if (type == "web" || lang == "node")
        {
            return """
                FROM node:22-alpine AS build
                WORKDIR /app
                COPY package*.json ./
                RUN npm ci
                COPY . .
                RUN npm run build

                FROM nginx:alpine
                COPY --from=build /app/dist /usr/share/nginx/html
                EXPOSE 80
                """;
        }

        return lang switch
        {
            "python" => """
                FROM python:3.12-slim
                WORKDIR /app
                COPY requirements.txt ./
                RUN pip install --no-cache-dir -r requirements.txt
                COPY . .
                EXPOSE 8000
                CMD ["python", "-m", "uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]
                """,

            "rust" => """
                FROM rust:1.83 AS build
                WORKDIR /app
                COPY . .
                RUN cargo build --release

                FROM debian:bookworm-slim
                WORKDIR /app
                COPY --from=build /app/target/release/app ./app
                EXPOSE 8080
                CMD ["./app"]
                """,

            "go" => """
                FROM golang:1.23 AS build
                WORKDIR /app
                COPY go.* ./
                RUN go mod download
                COPY . .
                RUN CGO_ENABLED=0 go build -o /app/app .

                FROM gcr.io/distroless/static
                COPY --from=build /app/app /app
                EXPOSE 8080
                ENTRYPOINT ["/app"]
                """,

            _ => $"""
                FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
                WORKDIR /app
                COPY *.csproj ./
                RUN dotnet restore
                COPY . .
                RUN dotnet publish -c Release -o out /p:UseAppHost=false

                FROM mcr.microsoft.com/dotnet/aspnet:10.0
                WORKDIR /app
                COPY --from=build /app/out .
                EXPOSE 8080
                ENV ASPNETCORE_URLS=http://+:8080
                ENTRYPOINT ["dotnet", "{name}.dll"]
                """
        };
    }

    private static string GenerateDockerignore() => """
        **/.git
        **/.github
        **/node_modules/
        **/bin/
        **/obj/
        **/target/
        **/__pycache__/
        **/.venv/
        **/dist/
        **/build/
        """;

    private static string GenerateCompose(string type, string lang, string name)
    {
        if (type == "full")
        {
            return $$"""
                services:
                  api:
                    build:
                      context: ..
                      dockerfile: docker/Dockerfile.api
                    ports:
                      - "8080:8080"
                    environment:
                      - ASPNETCORE_ENVIRONMENT=Development
                    depends_on:
                      - db

                  web:
                    build:
                      context: ..
                      dockerfile: docker/Dockerfile.web
                    ports:
                      - "80:80"
                    depends_on:
                      - api

                  db:
                    image: postgres:16-alpine
                    environment:
                      POSTGRES_DB: {{name}}
                      POSTGRES_USER: postgres
                      POSTGRES_PASSWORD: postgres
                    volumes:
                      - db-data:/var/lib/postgresql/data
                    ports:
                      - "5432:5432"

                volumes:
                  db-data:
                """;
        }

        return $$"""
            services:
              app:
                build:
                  context: ..
                  dockerfile: docker/Dockerfile
                ports:
                  - "8080:8080"
                environment:
                  - ASPNETCORE_ENVIRONMENT=Development
                depends_on:
                  - db

              db:
                image: postgres:16-alpine
                environment:
                  POSTGRES_DB: {{name}}
                  POSTGRES_USER: postgres
                  POSTGRES_PASSWORD: postgres
                volumes:
                  - db-data:/var/lib/postgresql/data

            volumes:
              db-data:
            """;
    }

    private static string DetectLanguage(string dir)
    {
        return LanguageDetector.Detect(dir) switch
        {
            "dotnet" => "dotnet",
            "node" or "deno" => "node",
            "rust" => "rust",
            "go" => "go",
            "python" => "python",
            _ => "dotnet"
        };
    }
}
