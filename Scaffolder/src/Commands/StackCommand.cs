using System.CommandLine;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class StackCommand : Command
{
    public StackCommand() : base("stack", "Genere un projet fullstack (frontend + backend + db)")
    {
        var nameOpt = new Option<string>("--name") { Description = "Nom du projet" };
        var frontendOpt = new Option<string>("--frontend")
        {
            Description = "Frontend (react, vue, svelte, next, nuxt)"
        };
        var backendOpt = new Option<string>("--backend")
        {
            Description = "Backend (webapi, fastapi, express, laravel, rails)"
        };
        var dbOpt = new Option<string>("--db")
        {
            Description = "Base de donnees (postgres, mysql, sqlite, mongodb)"
        };
        var dirOpt = new Option<DirectoryInfo?>("--output")
        {
            Description = "Dossier de sortie"
        };

        Add(nameOpt);
        Add(frontendOpt);
        Add(backendOpt);
        Add(dbOpt);
        Add(dirOpt);

        this.SetAction(async (ParseResult pr) => await HandleStackAsync(
            pr.GetValue(nameOpt), pr.GetValue(frontendOpt),
            pr.GetValue(backendOpt), pr.GetValue(dbOpt),
            pr.GetValue(dirOpt)));
    }

    private static async Task<int> HandleStackAsync(string? name, string? frontend, string? backend, string? db, DirectoryInfo? output)
    {
        // If any arg missing, show interactive wizard
        var needsWizard = string.IsNullOrWhiteSpace(name)
            || string.IsNullOrWhiteSpace(frontend)
            || string.IsNullOrWhiteSpace(backend)
            || string.IsNullOrWhiteSpace(db);

        if (needsWizard)
        {
            ConsoleService.Info("=== Assistant Stack Fullstack ===");
            Console.WriteLine();

            if (string.IsNullOrWhiteSpace(name))
            {
                name = ConsoleService.Prompt("Nom du projet", "mon-app");
                if (string.IsNullOrWhiteSpace(name)) name = "mon-app";
            }

            var frontends = new[] { "react", "vue", "svelte", "next", "nuxt", "none" };
            frontend ??= ConsoleService.Select("Frontend", frontends);

            var backends = new[] { "webapi", "fastapi", "express", "laravel", "rails", "none" };
            backend ??= ConsoleService.Select("Backend", backends);

            var dbs = new[] { "postgres", "mysql", "sqlite", "mongodb", "none" };
            db ??= ConsoleService.Select("Base de donnees", dbs);
        }

        ConsoleService.ShowLogo();
        Console.WriteLine();
        ConsoleService.Info($"Generation de la stack '{name}'");
        Console.WriteLine($"  Frontend : {frontend ?? "aucun"}");
        Console.WriteLine($"  Backend  : {backend ?? "aucun"}");
        Console.WriteLine($"  DB       : {db ?? "aucune"}");
        Console.WriteLine();

        var baseDir = output?.FullName ?? Directory.GetCurrentDirectory();
        var success = true;

        // Generate frontend
        if (!string.IsNullOrWhiteSpace(frontend) && frontend != "none")
        {
            var frontendDir = Path.Combine(baseDir, name, "frontend");
            var frontendTemplate = frontend switch
            {
                "react" => "npm react",
                "vue" => "npm vue",
                "svelte" => "npm svelte",
                "next" => "npm next",
                "nuxt" => "npm nuxt",
                _ => "npm vite"
            };

            ConsoleService.Info($"Generation du frontend ({frontend})...");
            var result = await ProcessService.RunAsync("dotnet",
                $"run --project src/Scaffolder.csproj -- new --template={frontendTemplate.Split(' ')[1]} --name={name}-frontend --output={frontendDir} --no-git --silent",
                Directory.GetCurrentDirectory());

            if (result.ExitCode == 0)
                ConsoleService.Success($"  Frontend {frontend} genere");
            else
            {
                ConsoleService.Error($"  Echec frontend {frontend}");
                success = false;
            }
        }

        // Generate backend
        if (!string.IsNullOrWhiteSpace(backend) && backend != "none")
        {
            var backendDir = Path.Combine(baseDir, name, "backend");
            var backendTemplate = backend switch
            {
                "webapi" => "dotnet webapi",
                "fastapi" => "python",
                "express" => "npm vite",
                "laravel" => "composer laravel",
                "rails" => "rails",
                _ => "dotnet webapi"
            };

            ConsoleService.Info($"Generation du backend ({backend})...");
            var result = await ProcessService.RunAsync("dotnet",
                $"run --project src/Scaffolder.csproj -- new --template={backendTemplate.Split(' ')[^1]} --name={name}-backend --output={backendDir} --no-git --silent",
                Directory.GetCurrentDirectory());

            if (result.ExitCode == 0)
                ConsoleService.Success($"  Backend {backend} genere");
            else
            {
                ConsoleService.Error($"  Echec backend {backend}");
                success = false;
            }
        }

        // Generate docker-compose with DB
        if (!string.IsNullOrWhiteSpace(db) && db != "none")
        {
            ConsoleService.Info($"Configuration de la base ({db})...");
            var dockerDir = Path.Combine(baseDir, name, "docker");
            Directory.CreateDirectory(dockerDir);

            var dbConfig = db switch
            {
                "postgres" => """
  db:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: ${DB_NAME:-app}
      POSTGRES_USER: ${DB_USER:-app}
      POSTGRES_PASSWORD: ${DB_PASS:-secret}
    ports:
      - "5432:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data
""",
                "mysql" => """
  db:
    image: mysql:8
    environment:
      MYSQL_DATABASE: ${DB_NAME:-app}
      MYSQL_USER: ${DB_USER:-app}
      MYSQL_PASSWORD: ${DB_PASS:-secret}
      MYSQL_ROOT_PASSWORD: ${DB_ROOT_PASS:-root}
    ports:
      - "3306:3306"
    volumes:
      - mysqldata:/var/lib/mysql
""",
                "mongodb" => """
  db:
    image: mongo:7
    environment:
      MONGO_INITDB_DATABASE: ${DB_NAME:-app}
    ports:
      - "27017:27017"
    volumes:
      - mongodata:/data/db
""",
                _ => ""
            };

            var compose = $"""
services:
{dbConfig}
volumes:
  {(db == "postgres" ? "pgdata:" : db == "mysql" ? "mysqldata:" : "mongodata:")}
""";

            File.WriteAllText(Path.Combine(dockerDir, "docker-compose.yml"), compose);
            File.WriteAllText(Path.Combine(dockerDir, ".env.example"), $"""
DB_NAME=app
DB_USER=app
DB_PASS=secret
DB_HOST=localhost
DB_PORT={(db == "postgres" ? "5432" : db == "mysql" ? "3306" : "27017")}
""");
            ConsoleService.Success($"  DB {db} configuree");
        }

        // Create root README
        var rootDir = Path.Combine(baseDir, name);
        Directory.CreateDirectory(rootDir);
        File.WriteAllText(Path.Combine(rootDir, "README.md"), $"""
# {name}

Stack fullstack : {frontend ?? "?"} + {backend ?? "?"} + {db ?? "?"}

## Structure
- `frontend/` — Interface utilisateur
- `backend/` — API
- `docker/` — Configuration Docker

## Demarrage
```bash
cd frontend && npm install && npm run dev
cd backend && dotnet run
docker compose -f docker/docker-compose.yml up -d
```
""");

        Console.WriteLine();
        if (success)
            ConsoleService.Success($"Stack '{name}' generee avec succes !");
        else
            ConsoleService.Warning("Stack generee avec des erreurs (voir ci-dessus).");

        ConsoleService.Info($"  cd {Path.Combine(baseDir, name)}");
        return success ? 0 : 1;
    }
}
