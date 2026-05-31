using System.CommandLine;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class DeployCommand : Command
{
    public DeployCommand() : base("deploy", "Deploie le projet sur Vercel, Railway, ou Docker")
    {
        var targetOpt = new Option<string>("--target")
        {
            Description = "Plateforme de deploiement (vercel, railway, docker)",
            Required = false
        };
        var prodOpt = new Option<bool>("--prod")
        {
            Description = "Deploiement en production"
        };
        Add(targetOpt);
        Add(prodOpt);
        SetAction((ParseResult pr) => Handle(pr.GetValue(targetOpt), pr.GetValue(prodOpt)));
    }

    private static int Handle(string? target, bool prod)
    {
        var cwd = Directory.GetCurrentDirectory();
        target ??= DetectPlatform(cwd);

        if (target == null)
        {
            ConsoleService.Info("Detection automatique de la plateforme...");
            Console.WriteLine();
            target = ConsoleService.Select(
                "  Choisis la plateforme de deploiement :",
                ["vercel", "railway", "docker", "github-pages"]
            );
        }

        Console.WriteLine();
        ConsoleService.Info($"Deploiement vers {target}...");
        Console.WriteLine();

        return target switch
        {
            "vercel" => DeployVercel(cwd, prod),
            "railway" => DeployRailway(cwd),
            "docker" => DeployDocker(cwd),
            "github-pages" => DeployGitHubPages(cwd),
            _ => 1
        };
    }

    private static string? DetectPlatform(string cwd)
    {
        if (File.Exists(Path.Combine(cwd, "vercel.json"))) return "vercel";
        if (File.Exists(Path.Combine(cwd, "railway.json"))) return "railway";
        if (File.Exists(Path.Combine(cwd, "Dockerfile"))) return "docker";
        if (File.Exists(Path.Combine(cwd, "package.json")))
        {
            var pkg = File.ReadAllText(Path.Combine(cwd, "package.json"));
            if (pkg.Contains("next")) return "vercel";
            if (pkg.Contains("vue") || pkg.Contains("nuxt")) return "vercel";
            if (pkg.Contains("svelte")) return "vercel";
        }
        return null;
    }

    private static int DeployVercel(string cwd, bool prod)
    {
        if (!ProcessService.CommandExists("vercel"))
        {
            ConsoleService.Warning("Vercel CLI non installe.");
            ConsoleService.Info("Installation : npm i -g vercel");
            var answer = ConsoleService.Prompt("Installer maintenant ? (o/N)", "N");
            if (answer?.ToLowerInvariant() != "o")
                return 1;
            var install = ProcessService.RunAsync("npm", "install -g vercel", streamOutput: true)
                .GetAwaiter().GetResult();
            if (install.ExitCode != 0)
            {
                ConsoleService.Error("Echec de l'installation de Vercel CLI.");
                return 1;
            }
        }

        var args = prod ? "deploy --prod" : "deploy";
        ConsoleService.Info("Lancement du deploiement Vercel...");
        var result = ProcessService.RunAsync("vercel", args, cwd, streamOutput: true)
            .GetAwaiter().GetResult();
        return result.ExitCode;
    }

    private static int DeployRailway(string cwd)
    {
        if (!ProcessService.CommandExists("railway"))
        {
            ConsoleService.Warning("Railway CLI non installe.");
            ConsoleService.Info("Installation : npm i -g @railway/cli");
            var answer = ConsoleService.Prompt("Installer maintenant ? (o/N)", "N");
            if (answer?.ToLowerInvariant() != "o")
                return 1;
            var install = ProcessService.RunAsync("npm", "install -g @railway/cli", streamOutput: true)
                .GetAwaiter().GetResult();
            if (install.ExitCode != 0)
            {
                ConsoleService.Error("Echec de l'installation de Railway CLI.");
                return 1;
            }
        }

        ConsoleService.Info("Lancement du deploiement Railway...");
        var result = ProcessService.RunAsync("railway", "up", cwd, streamOutput: true)
            .GetAwaiter().GetResult();
        return result.ExitCode;
    }

    private static int DeployDocker(string cwd)
    {
        if (!ProcessService.CommandExists("docker"))
        {
            ConsoleService.Error("Docker n'est pas installe.");
            ConsoleService.Info("Installe Docker : https://docs.docker.com/get-docker/");
            return 1;
        }

        if (!File.Exists(Path.Combine(cwd, "Dockerfile")))
        {
            ConsoleService.Warning("Aucun Dockerfile trouve.");
            var answer = ConsoleService.Prompt("Generer un Dockerfile ? (O/n)", "O");
            if (answer?.ToLowerInvariant() != "n")
            {
                DockerCommand.GenerateDockerfile(cwd);
            }
            else
            {
                ConsoleService.Error("Un Dockerfile est requis pour le deploiement Docker.");
                return 1;
            }
        }

        var projectName = new DirectoryInfo(cwd).Name.ToLowerInvariant().Replace(" ", "-");

        ConsoleService.Info("Construction de l'image Docker...");
        var build = ProcessService.RunAsync("docker",
            $"build -t {projectName} .", cwd, streamOutput: true)
            .GetAwaiter().GetResult();
        if (build.ExitCode != 0) return build.ExitCode;

        ConsoleService.Info("Lancement du conteneur...");
        var run = ProcessService.RunAsync("docker",
            $"run -d -p 3000:3000 --name {projectName} {projectName}", cwd, streamOutput: true)
            .GetAwaiter().GetResult();

        if (run.ExitCode == 0)
        {
            ConsoleService.Success($"Image {projectName} construite et lancee sur le port 3000.");
            ConsoleService.Info("  Pour arreter : docker stop " + projectName);
            ConsoleService.Info("  Pour publier : docker push <user>/" + projectName);
        }

        return run.ExitCode;
    }

    private static int DeployGitHubPages(string cwd)
    {
        ConsoleService.Info("Preparation du deploiement GitHub Pages...");

        // Create GitHub Actions workflow for Pages
        var workflowsDir = Path.Combine(cwd, ".github", "workflows");
        Directory.CreateDirectory(workflowsDir);

        var workflow = """
name: Deploy to GitHub Pages

on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      pages: write
      id-token: write
    environment:
      name: github-pages
      url: ${{ steps.deployment.outputs.page_url }}
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: 22
      - run: npm install && npm run build
      - uses: actions/configure-pages@v5
      - uses: actions/upload-pages-artifact@v3
        with:
          path: dist
      - id: deployment
        uses: actions/deploy-pages@v4
""";
        File.WriteAllText(Path.Combine(workflowsDir, "deploy-pages.yml"), workflow);

        ConsoleService.Success("Workflow GitHub Pages cree !");
        ConsoleService.Info("  .github/workflows/deploy-pages.yml");
        Console.WriteLine();
        ConsoleService.Info("Prochaines etapes :");
        ConsoleService.Info("  1. Active GitHub Pages dans Settings > Pages (branch: gh-pages)");
        ConsoleService.Info("  2. Pousse sur main pour deployer automatiquement");

        return 0;
    }
}
