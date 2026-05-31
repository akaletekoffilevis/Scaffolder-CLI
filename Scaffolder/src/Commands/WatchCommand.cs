using System.CommandLine;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class WatchCommand : Command
{
    public WatchCommand() : base("watch", "Surveille un dossier et re-genere automatiquement")
    {
        var templateArg = new Argument<string>("template")
        {
            Description = "Template a utiliser pour la regeneration"
        };
        var pathOpt = new Option<DirectoryInfo?>("--path")
        {
            Description = "Dossier a surveiller (defaut: courant)"
        };
        var intervalOpt = new Option<int>("--interval")
        {
            Description = "Intervalle en secondes entre les verifications",
            DefaultValueFactory = _ => 5
        };

        Add(templateArg);
        Add(pathOpt);
        Add(intervalOpt);

        SetAction((ParseResult pr) => HandleWatch(
            pr.GetValue(templateArg), pr.GetValue(pathOpt), pr.GetValue(intervalOpt)));
    }

    private static int HandleWatch(string? template, DirectoryInfo? path, int interval)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            ConsoleService.Error("Usage : scaffold watch <template> [--path <dossier>] [--interval <secondes>]");
            ConsoleService.Info("Exemple : scaffold watch webapi --path ./mon-api --interval 3");
            return 1;
        }

        var cwd = path?.FullName ?? Directory.GetCurrentDirectory();
        interval = Math.Max(1, Math.Min(interval, 60));

        ConsoleService.Info($" Surveillance de : {cwd}");
        ConsoleService.Info($" Template          : {template}");
        ConsoleService.Info($" Intervalle        : {interval}s");
        Console.WriteLine();

        if (!Directory.Exists(cwd))
        {
            ConsoleService.Warning("Le dossier n'existe pas encore. Cree-le ou attends la premiere generation.");
            Directory.CreateDirectory(cwd);
        }

        var files = new Dictionary<string, DateTime>();
        var firstRun = true;

        ConsoleService.Info("Pret. Modifie un fichier pour declencher la regeneration.");
        Console.WriteLine();

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var currentFiles = Directory.GetFiles(cwd, "*", SearchOption.AllDirectories)
                    .Where(f => !f.Contains(".git") && !f.Contains("node_modules") && !f.Contains("bin/") && !f.Contains("obj/"))
                    .ToDictionary(f => f, f => File.GetLastWriteTimeUtc(f));

                var changed = currentFiles.Any(f =>
                    !files.TryGetValue(f.Key, out var time) || time != f.Value);

                if (changed || firstRun)
                {
                    if (!firstRun)
                    {
                        var newFiles = currentFiles.Where(f => !files.ContainsKey(f.Key)).ToList();
                        var modFiles = currentFiles.Where(f =>
                            files.TryGetValue(f.Key, out var t) && t != f.Value).ToList();

                        if (newFiles.Count > 0)
                            ConsoleService.Info($"  Nouveaux fichiers : {string.Join(", ", newFiles.Select(f => Path.GetFileName(f.Key)))}");
                        if (modFiles.Count > 0)
                            ConsoleService.Info($"  Fichiers modifies : {string.Join(", ", modFiles.Select(f => Path.GetFileName(f.Key)))}");

                        ConsoleService.Info("  Regeneration...");
                        var result = ProcessService.RunAsync("dotnet",
                            $"run --project src/Scaffolder.csproj -- new --template={template} --name={Path.GetFileName(cwd)} --no-git",
                            Directory.GetCurrentDirectory()).Result;

                        if (result.ExitCode == 0)
                            ConsoleService.Success("  ✅ Regeneration terminee");
                        else
                            ConsoleService.Error("  ❌ Echec de la regeneration");
                    }

                    files = currentFiles;
                    firstRun = false;
                }

                Thread.Sleep(interval * 1000);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal exit
        }

        ConsoleService.Info(" Surveillance arretee.");
        return 0;
    }
}
