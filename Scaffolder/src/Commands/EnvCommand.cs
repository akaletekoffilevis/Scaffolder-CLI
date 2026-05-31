using System.CommandLine;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class EnvCommand : Command
{
    public EnvCommand() : base("env", "Genere des fichiers .env et .env.example")
    {
        var typeOpt = new Option<string>("--type")
        {
            Description = "Type de projet (api, web, mobile, docker)"
        };
        var outputOpt = new Option<DirectoryInfo?>("--output")
        {
            Description = "Dossier de sortie"
        };
        Add(typeOpt);
        Add(outputOpt);
        SetAction((ParseResult pr) => HandleEnv(
            pr.GetValue(typeOpt), pr.GetValue(outputOpt)));
    }

    private static int HandleEnv(string? type, DirectoryInfo? output)
    {
        var outputDir = output?.FullName ?? Directory.GetCurrentDirectory();
        type = type?.ToLowerInvariant() ?? DetectProjectType(outputDir);

        var (envExample, envContent) = type switch
        {
            "api" => (GenerateApiEnv(), GenerateApiEnvExample()),
            "web" => (GenerateWebEnv(), GenerateWebEnvExample()),
            "mobile" => (GenerateMobileEnv(), GenerateMobileEnvExample()),
            "docker" => (GenerateDockerEnv(), GenerateDockerEnvExample()),
            _ => (GenerateApiEnv(), GenerateApiEnvExample())
        };

        File.WriteAllText(Path.Combine(outputDir, ".env.example"), envExample);
        ConsoleService.Success(".env.example cree");

        if (!File.Exists(Path.Combine(outputDir, ".env")))
        {
            File.WriteAllText(Path.Combine(outputDir, ".env"), envContent);
            ConsoleService.Success(".env cree");
        }
        else
        {
            ConsoleService.Info(".env existe deja, ignore.");
        }

        WriteGitignore(outputDir);
        return 0;
    }

    private static string DetectProjectType(string dir)
    {
        if (File.Exists(Path.Combine(dir, "docker-compose.yml")) || File.Exists(Path.Combine(dir, "Dockerfile")))
            return "docker";
        if (File.Exists(Path.Combine(dir, "package.json"))) return "web";
        if (Directory.GetFiles(dir, "*.csproj").Length > 0) return "api";
        return "api";
    }

    private static string GenerateApiEnv() => """
# Configuration de l'application
APP_ENV=development
APP_DEBUG=true
APP_PORT=3000

# Base de donnees
DB_CONNECTION=postgresql
DB_HOST=localhost
DB_PORT=5432
DB_DATABASE=app
DB_USERNAME=user
DB_PASSWORD=secret

# JWT / Auth
JWT_SECRET=change-me-in-production
JWT_EXPIRES_IN=7d

# Redis / Cache
REDIS_HOST=localhost
REDIS_PORT=6379
REDIS_PASSWORD=
""";

    private static string GenerateApiEnvExample() => """
# Copie ce fichier en .env et modifie les valeurs
APP_ENV=development
APP_PORT=3000
DB_HOST=localhost
DB_DATABASE=app
JWT_SECRET=change-me
""";

    private static string GenerateWebEnv() => """
# Application web
NODE_ENV=development
VITE_API_URL=http://localhost:3000
VITE_APP_NAME=MyApp
NEXT_PUBLIC_API_URL=http://localhost:3000
""";

    private static string GenerateWebEnvExample() => """
NODE_ENV=development
VITE_API_URL=http://localhost:3000
""";

    private static string GenerateMobileEnv() => """
# Application mobile
API_URL=http://localhost:3000
APP_NAME=MyApp
""";

    private static string GenerateMobileEnvExample() => """
API_URL=http://localhost:3000
""";

    private static string GenerateDockerEnv() => """
# Docker
COMPOSE_PROJECT_NAME=myapp
POSTGRES_USER=user
POSTGRES_PASSWORD=secret
POSTGRES_DB=app
REDIS_PASSWORD=secret
""";

    private static string GenerateDockerEnvExample() => """
COMPOSE_PROJECT_NAME=myapp
POSTGRES_USER=user
POSTGRES_PASSWORD=secret
""";

    private static void WriteGitignore(string dir)
    {
        var gitignorePath = Path.Combine(dir, ".gitignore");
        if (File.Exists(gitignorePath))
        {
            var content = File.ReadAllText(gitignorePath);
            if (!content.Contains(".env"))
            {
                File.AppendAllText(gitignorePath, "\n# Env files\n.env\n.env.local\n.env.*.local\n");
                ConsoleService.Info(".gitignore mis a jour avec .env");
            }
        }
    }
}
