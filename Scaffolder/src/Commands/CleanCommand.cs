using System.CommandLine;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class CleanCommand : Command
{
    private static readonly string[] DirsToClean = ["node_modules", "bin", "obj", "__pycache__", ".next", "dist", "build", "target"];
    private static readonly string[] FilesToClean = ["package-lock.json", "yarn.lock", "pnpm-lock.yaml"];

    public CleanCommand() : base("clean", "Nettoie les fichiers generes (node_modules, bin, obj...)")
    {
        var allOpt = new Option<bool>("--all")
        {
            Description = "Nettoie tout (y compris verrous)"
        };
        Add(allOpt);
        SetAction((ParseResult pr) => HandleClean(pr.GetValue(allOpt)));
    }

    private static int HandleClean(bool all)
    {
        var cwd = Directory.GetCurrentDirectory();
        var cleaned = 0;

        foreach (var dir in DirsToClean)
        {
            var path = Path.Combine(cwd, dir);
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
                ConsoleService.Info($"  Supprime : {dir}/");
                cleaned++;
            }
        }

        if (all)
        {
            foreach (var file in FilesToClean)
            {
                var path = Path.Combine(cwd, file);
                if (File.Exists(path))
                {
                    File.Delete(path);
                    ConsoleService.Info($"  Supprime : {file}");
                    cleaned++;
                }
            }
        }

        if (cleaned == 0)
            ConsoleService.Info("Rien a nettoyer.");
        else
            ConsoleService.Success($"Nettoyage termine. {cleaned} elements supprimes.");

        return 0;
    }
}
