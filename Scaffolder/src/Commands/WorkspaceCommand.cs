using System.CommandLine;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class WorkspaceCommand : Command
{
    public WorkspaceCommand() : base("workspace", "Cree et gere des workspaces multi-projets (monorepo)")
    {
        var initCmd = new Command("init", "Initialise un workspace monorepo");
        var nameArg = new Argument<string>("name") { Description = "Nom du workspace" };
        var projectsOpt = new Option<string>("--projects")
        {
            Description = "Liste des sous-projets (ex: frontend+vite,backend+webapi)"
        };
        var managerOpt = new Option<string>("--manager")
        {
            Description = "Gestionnaire de paquets (npm, dotnet, cargo)"
        };
        initCmd.Add(nameArg);
        initCmd.Add(projectsOpt);
        initCmd.Add(managerOpt);
        initCmd.SetAction((ParseResult pr) => HandleInit(
            pr.GetValue(nameArg)!,
            pr.GetValue(projectsOpt),
            pr.GetValue(managerOpt)));

        var addCmd = new Command("add", "Ajoute un sous-projet au workspace");
        var addNameArg = new Argument<string>("name") { Description = "Nom du sous-projet" };
        var addTemplateArg = new Argument<string>("template") { Description = "Template (vite, webapi, etc.)" };
        addCmd.Add(addNameArg);
        addCmd.Add(addTemplateArg);
        addCmd.SetAction((ParseResult pr) => HandleAdd(
            pr.GetValue(addNameArg)!, pr.GetValue(addTemplateArg)!));

        var listCmd = new Command("list", "Liste les sous-projets du workspace");
        listCmd.SetAction(_ => HandleList());

        Add(initCmd);
        Add(addCmd);
        Add(listCmd);

        SetAction(_ =>
        {
            ConsoleService.Info("Sous-commandes : init, add, list");
            return 0;
        });
    }

    private static int HandleInit(string name, string? projects, string? manager)
    {
        var cwd = Path.Combine(Directory.GetCurrentDirectory(), name);
        if (Directory.Exists(cwd))
        {
            ConsoleService.Error($"Le dossier '{name}' existe deja.");
            return 1;
        }

        Directory.CreateDirectory(cwd);
        manager ??= DetectBestManager();

        ConsoleService.Info($"Creation du workspace '{name}'...");
        ConsoleService.Info($"  Gestionnaire : {manager}");
        Console.WriteLine();

        // Create workspace root files
        switch (manager)
        {
            case "npm":
                File.WriteAllText(Path.Combine(cwd, "package.json"), $$"""
{
  "name": "{{name}}",
  "private": true,
  "workspaces": []
}
""");
                break;
            case "dotnet":
                File.WriteAllText(Path.Combine(cwd, $"{name}.sln"), "");
                break;
            case "cargo":
                File.WriteAllText(Path.Combine(cwd, "Cargo.toml"), """
[workspace]
members = []
""");
                break;
        }

        // Create gitignore
        File.WriteAllText(Path.Combine(cwd, ".gitignore"), """
node_modules/
dist/
bin/
obj/
target/
.DS_Store
*.log
""");

        // Init git
        ProcessService.RunAsync("git", "init", cwd).Wait();
        ConsoleService.Success("Workspace initialise.");

        // Add subprojects if specified
        if (!string.IsNullOrWhiteSpace(projects))
        {
            Console.WriteLine();
            ConsoleService.Info("Ajout des sous-projets...");
            Console.WriteLine();

            foreach (var proj in projects.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var parts = proj.Split('+');
                var projName = parts[0];
                var projTemplate = parts.Length > 1 ? parts[1] : "hello";
                var projDir = Path.Combine(cwd, projName);

                ConsoleService.Info($"  {projName} ({projTemplate})");

                var (code, _, _) = NewCommand.GenerateProjectStatic(projName, projTemplate, projDir, null);
                if (code == 0)
                {
                    // Update workspace config
                    UpdateWorkspaceConfig(cwd, manager, projName);
                }
            }
        }

        Console.WriteLine();
        ConsoleService.Info($"Pour commencer : cd {name}");
        if (manager == "npm")
            ConsoleService.Info($"  npm install (pour installer toutes les dependances)");

        return 0;
    }

    private static int HandleAdd(string name, string template)
    {
        var cwd = Directory.GetCurrentDirectory();
        var workspaceConfig = FindWorkspaceRoot(cwd);
        if (workspaceConfig == null)
        {
            ConsoleService.Error("Aucun workspace trouve. Execute 'scaffold workspace init' d'abord.");
            return 1;
        }

        var manager = DetectBestManager(workspaceConfig);
        var projDir = Path.Combine(workspaceConfig, name);

        if (Directory.Exists(projDir))
        {
            ConsoleService.Error($"Le sous-projet '{name}' existe deja.");
            return 1;
        }

        ConsoleService.Info($"Ajout de '{name}' ({template}) au workspace...");

        var (code, _, _) = NewCommand.GenerateProjectStatic(name, template, projDir, null);
        if (code == 0)
        {
            UpdateWorkspaceConfig(workspaceConfig, manager, name);
            ConsoleService.Success($"Sous-projet '{name}' ajoute au workspace.");
            return 0;
        }

        return code;
    }

    private static int HandleList()
    {
        var cwd = Directory.GetCurrentDirectory();
        var workspaceRoot = FindWorkspaceRoot(cwd);
        if (workspaceRoot == null)
        {
            ConsoleService.Warning("Aucun workspace trouve dans ce dossier.");
            ConsoleService.Info("Execute 'scaffold workspace init' pour en creer un.");
            return 1;
        }

        ConsoleService.Info($"Workspace : {new DirectoryInfo(workspaceRoot).Name}");
        Console.WriteLine();

        var subdirs = Directory.GetDirectories(workspaceRoot)
            .Where(d =>
            {
                var name = Path.GetFileName(d);
                return !name.StartsWith('.') && name != "node_modules" && name != "bin" && name != "obj";
            })
            .ToList();

        if (subdirs.Count == 0)
        {
            ConsoleService.Warning("Aucun sous-projet trouve.");
            ConsoleService.Info("Ajoute un projet : scaffold workspace add <name> <template>");
            return 1;
        }

        foreach (var dir in subdirs)
        {
            var name = Path.GetFileName(dir);
            var hasPkg = File.Exists(Path.Combine(dir, "package.json"));
            var hasCsproj = Directory.GetFiles(dir, "*.csproj").Length > 0;
            var hasCargo = File.Exists(Path.Combine(dir, "Cargo.toml"));
            var icon = hasPkg ? "⬡" : hasCsproj ? "◆" : hasCargo ? "🦀" : "📁";
            var type = hasPkg ? "Node.js" : hasCsproj ? ".NET" : hasCargo ? "Rust" : "inconnu";
            Console.WriteLine($"  {icon} {name,-20} {type}");
        }

        return 0;
    }

    private static string? FindWorkspaceRoot(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, ".scaffolder-workspace")))
                return dir.FullName;
            if (dir.GetFiles("*.sln").Length > 0 && File.Exists(Path.Combine(dir.FullName, ".scaffolder-workspace")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }

    private static string DetectBestManager(string? workspaceDir = null)
    {
        if (workspaceDir != null)
        {
            if (File.Exists(Path.Combine(workspaceDir, "package.json"))) return "npm";
            if (Directory.GetFiles(workspaceDir, "*.sln").Length > 0) return "dotnet";
            if (File.Exists(Path.Combine(workspaceDir, "Cargo.toml"))) return "cargo";
        }

        if (ProcessService.CommandExists("npm")) return "npm";
        if (ProcessService.CommandExists("dotnet")) return "dotnet";
        if (ProcessService.CommandExists("cargo")) return "cargo";
        return "npm";
    }

    private static void UpdateWorkspaceConfig(string workspaceDir, string manager, string projectName)
    {
        // Create workspace marker
        File.WriteAllText(Path.Combine(workspaceDir, ".scaffolder-workspace"), "workspace");

        switch (manager)
        {
            case "npm":
                var pkgPath = Path.Combine(workspaceDir, "package.json");
                if (File.Exists(pkgPath))
                {
                    var pkg = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                        File.ReadAllText(pkgPath), JsonContext.Default.DictionaryStringObject);
                    if (pkg != null)
                    {
                        if (!pkg.ContainsKey("workspaces") || pkg["workspaces"] is not System.Text.Json.JsonElement { ValueKind: System.Text.Json.JsonValueKind.Array })
                        {
                            pkg["workspaces"] = new[] { projectName };
                            File.WriteAllText(pkgPath,
                                System.Text.Json.JsonSerializer.Serialize(pkg, JsonContext.Default.DictionaryStringObject));
                        }
                    }
                }
                break;

            case "dotnet":
                var slnFiles = Directory.GetFiles(workspaceDir, "*.sln");
                if (slnFiles.Length > 0)
                {
                    var csprojFiles = Directory.GetFiles(
                        Path.Combine(workspaceDir, projectName), "*.csproj", SearchOption.TopDirectoryOnly);
                    if (csprojFiles.Length > 0)
                    {
                        ProcessService.RunAsync("dotnet", $"sln add \"{csprojFiles[0]}\"", workspaceDir).Wait();
                    }
                }
                break;

            case "cargo":
                var cargoPath = Path.Combine(workspaceDir, "Cargo.toml");
                if (File.Exists(cargoPath))
                {
                    var content = File.ReadAllText(cargoPath);
                    if (!content.Contains($"\"{projectName}\""))
                    {
                        content = content.Replace("members = [", $"members = [\"{projectName}\",");
                        File.WriteAllText(cargoPath, content);
                    }
                }
                break;
        }
    }
}
