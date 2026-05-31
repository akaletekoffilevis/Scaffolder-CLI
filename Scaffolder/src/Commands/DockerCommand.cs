using System.CommandLine;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class DockerCommand : Command
{
    public DockerCommand() : base("docker", "Genere Dockerfile et docker-compose.yml")
    {
        var typeOpt = new Option<string>("--type")
        {
            Description = "Type de projet (api, web, mobile, full)"
        };
        var outputOpt = new Option<DirectoryInfo?>("--output")
        {
            Description = "Dossier de sortie"
        };
        Add(typeOpt);
        Add(outputOpt);
        SetAction((ParseResult pr) => HandleDocker(
            pr.GetValue(typeOpt), pr.GetValue(outputOpt)));
    }

    private static int HandleDocker(string? type, DirectoryInfo? output)
    {
        var outputDir = output?.FullName ?? Directory.GetCurrentDirectory();
        Directory.CreateDirectory(outputDir);
        type = type?.ToLowerInvariant() ?? "api";

        switch (type)
        {
            case "api":
                WriteFile(outputDir, "Dockerfile", GenerateApiDockerfile());
                WriteFile(outputDir, ".dockerignore", GenerateDockerignore());
                WriteFile(outputDir, "docker-compose.yml", GenerateComposeApi());
                break;
            case "web":
                WriteFile(outputDir, "Dockerfile", GenerateWebDockerfile());
                WriteFile(outputDir, ".dockerignore", GenerateDockerignore());
                WriteFile(outputDir, "docker-compose.yml", GenerateComposeWeb());
                break;
            case "full":
                WriteFile(outputDir, "Dockerfile.api", GenerateApiDockerfile());
                WriteFile(outputDir, "Dockerfile.web", GenerateWebDockerfile());
                WriteFile(outputDir, ".dockerignore", GenerateDockerignore());
                WriteFile(outputDir, "docker-compose.yml", GenerateComposeFull());
                break;
            default:
                ConsoleService.Error($"Type inconnu : {type}. Choisis : api, web, full");
                return 1;
        }

        ConsoleService.Success($"Fichiers Docker generes dans {outputDir}");
        return 0;
    }

    private static string GenerateApiDockerfile() => """
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app
COPY *.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .
EXPOSE 8080
ENTRYPOINT ["dotnet", "app.dll"]
""";

    private static string GenerateWebDockerfile() => """
FROM node:22-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:alpine AS runtime
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
""";

    private static string GenerateDockerignore() => """
**/.class
**/.dockerignore
**/.env
**/.git
**/.gitignore
**/.next
**/bin/
**/build/
**/dist/
**/node_modules/
**/obj/
**/target/
**/__pycache__/
""";

    private static string GenerateComposeApi() => """
version: "3.9"

services:
  api:
    build: .
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__Default=Host=db;Database=app;Username=user;Password=secret
    depends_on:
      - db
      - redis

  db:
    image: postgres:16-alpine
    environment:
      POSTGRES_USER: user
      POSTGRES_PASSWORD: secret
      POSTGRES_DB: app
    ports:
      - "5432:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"

volumes:
  pgdata:
""";

    private static string GenerateComposeWeb() => """
version: "3.9"

services:
  web:
    build: .
    ports:
      - "80:80"
""";

    private static string GenerateComposeFull() => """
version: "3.9"

services:
  api:
    build:
      context: .
      dockerfile: Dockerfile.api
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
    depends_on:
      - db
      - redis

  web:
    build:
      context: .
      dockerfile: Dockerfile.web
    ports:
      - "80:80"
    depends_on:
      - api

  db:
    image: postgres:16-alpine
    environment:
      POSTGRES_USER: user
      POSTGRES_PASSWORD: secret
      POSTGRES_DB: app
    ports:
      - "5432:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"

volumes:
  pgdata:
""";

    public static void GenerateDockerfile(string cwd, string type = "web")
    {
        type = type.ToLowerInvariant();
        var dockerfile = type == "api" ? GenerateApiDockerfile() : GenerateWebDockerfile();
        File.WriteAllText(Path.Combine(cwd, "Dockerfile"), dockerfile.TrimStart('\n'));
        ConsoleService.Success("Dockerfile genere.");
    }

    private static void WriteFile(string dir, string file, string content)
    {
        File.WriteAllText(Path.Combine(dir, file), content.TrimStart('\n'));
        ConsoleService.Info($"  Cree : {file}");
    }
}
