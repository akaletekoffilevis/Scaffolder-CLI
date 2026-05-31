using System.CommandLine;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class UpgradeCommand : Command
{
    public UpgradeCommand() : base("upgrade", "Met à jour Scaffolder vers la dernière version")
    {
        SetAction(HandleUpgrade);
    }

    private static async Task<int> HandleUpgrade(ParseResult pr)
    {
        ConsoleService.Info("Recherche de la derniere version disponible...");

        var (latestVersion, downloadUrl) = await UpdateService.CheckForUpdateAsync();

        if (latestVersion == null)
        {
            ConsoleService.Warning("Impossible de verifier les mises a jour. Verifie ta connexion Internet.");
            return 1;
        }

        var current = UpdateService.CurrentVersion;
        ConsoleService.Info($"Version actuelle : v{current}");
        ConsoleService.Info($"Derniere version : v{latestVersion}");

        if (current == latestVersion)
        {
            ConsoleService.Success("Tu as deja la derniere version !");
            return 0;
        }

        ConsoleService.Warning($"Nouvelle version disponible : v{latestVersion}");
        ConsoleService.Info("Telechargement et installation de la mise a jour...");

        var success = await UpdateService.DownloadAndInstallAsync(downloadUrl!);

        if (success)
        {
            ConsoleService.Success("Mise a jour installee avec succes ! Redemarre Scaffolder pour utiliser la nouvelle version.");
            return 0;
        }

        ConsoleService.Error("Echec de la mise a jour. Essaie de telecharger manuellement depuis GitHub.");
        return 1;
    }
}
