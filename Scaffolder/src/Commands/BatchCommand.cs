using System.CommandLine;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class BatchCommand : Command
{
    public BatchCommand() : base("batch", "Genere plusieurs projets depuis un fichier YAML")
    {
        var fileArg = new Argument<FileInfo?>("file")
        {
            Description = "Fichier YAML de description des projets",
            Arity = ArgumentArity.ZeroOrOne
        };
        Add(fileArg);

        SetAction((ParseResult pr) => HandleBatch(pr.GetValue(fileArg)));
    }

    private static int HandleBatch(FileInfo? file)
    {
        var path = file?.FullName;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            // Generate example file
            ConsoleService.Info("Aucun fichier fourni. Genere un exemple...");
            var example = @"# scaffold batch example
# Genere plusieurs projets en 1 commande
projects:
  - name: mon-api
    template: webapi
    lang: csharp
    features: [docker, ci]

  - name: mon-frontend
    template: react
    lang: typescript
    features: [docker]

  - name: ma-base
    template: classlib
    lang: csharp
    features: []
";
            var examplePath = Path.Combine(Directory.GetCurrentDirectory(), "scaffold-batch.yml");
            File.WriteAllText(examplePath, example);
            ConsoleService.Success($"Fichier exemple cree : {examplePath}");
            ConsoleService.Info("");
            ConsoleService.Info("Modifie-le puis relance :");
            ConsoleService.Info($"  scaffold batch {examplePath}");
            return 0;
        }

        ConsoleService.Info($"Lecture du fichier : {path}");

        try
        {
            var lines = File.ReadAllLines(path);
            var projects = ParseProjects(lines);

            if (projects.Count == 0)
            {
                ConsoleService.Error("Aucun projet trouve dans le fichier.");
                ConsoleService.Info("Format attendu :");
                ConsoleService.Info("  projects:");
                ConsoleService.Info("    - name: mon-projet");
                ConsoleService.Info("      template: webapi");
                ConsoleService.Info("      lang: csharp");
                ConsoleService.Info("      features: [docker, ci]");
                return 1;
            }

            var total = projects.Count;
            var ok = 0;

            for (var i = 0; i < projects.Count; i++)
            {
                var p = projects[i];
                ConsoleService.Info($"\n[{i + 1}/{total}] Generation de '{p.Name}' ({p.Template})...");

                var args = $"new --template={p.Template} --name={p.Name}";
                if (!string.IsNullOrWhiteSpace(p.Lang))
                    args += $" --lang={p.Lang}";
                if (p.Features.Contains("docker"))
                    args += " --docker";
                if (p.Features.Contains("ci"))
                    args += " --ci";
                if (p.Features.Contains("no-git"))
                    args += " --no-git";

                // Execute scaffold new
                var result = ProcessService.RunAsync("dotnet", $"run --project src/Scaffolder.csproj -- {args}",
                    Directory.GetCurrentDirectory()).Result;

                if (result.ExitCode == 0)
                {
                    ConsoleService.Success($"  ✅ {p.Name} genere");
                    ok++;
                }
                else
                {
                    ConsoleService.Error($"  ❌ {p.Name} : echec");
                }
            }

            Console.WriteLine();
            ConsoleService.Success($"Termine : {ok}/{total} projets generes.");
            return ok == total ? 0 : 1;
        }
        catch (Exception ex)
        {
            ConsoleService.Error($"Erreur : {ex.Message}");
            return 1;
        }
    }

    private static List<(string Name, string Template, string? Lang, string[] Features)> ParseProjects(string[] lines)
    {
        var projects = new List<(string Name, string Template, string? Lang, string[] Features)>();
        (string Name, string Template, string? Lang, string[] Features)? current = null;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("- name:"))
            {
                if (current.HasValue)
                {
                    if (!string.IsNullOrWhiteSpace(current.Value.Name))
                        projects.Add(current.Value);
                }
                var name = trimmed["- name:".Length..].Trim().Trim('"');
                current = (name, "", null, []);
            }
            else if (current.HasValue)
            {
                if (trimmed.StartsWith("template:"))
                    current = (current.Value.Name, trimmed["template:".Length..].Trim().Trim('"'), current.Value.Lang, current.Value.Features);
                else if (trimmed.StartsWith("lang:"))
                    current = (current.Value.Name, current.Value.Template, trimmed["lang:".Length..].Trim().Trim('"'), current.Value.Features);
                else if (trimmed.StartsWith("features:"))
                {
                    var featuresPart = trimmed["features:".Length..].Trim();
                    var features = featuresPart.Trim('[', ']').Split(',')
                        .Select(f => f.Trim().Trim('"')).Where(f => f.Length > 0).ToArray();
                    current = (current.Value.Name, current.Value.Template, current.Value.Lang, features);
                }
            }
        }

        if (current.HasValue && !string.IsNullOrWhiteSpace(current.Value.Name))
            projects.Add(current.Value);

        return projects;
    }
}
