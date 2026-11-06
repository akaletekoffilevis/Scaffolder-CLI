using System.CommandLine;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class UpgradeCommand : Command
{
    public UpgradeCommand() : base("upgrade", "Met à jour Scaffolder vers la dernière version")
    {
        var checkOpt = new Option<bool>("--check") { Description = "Verifie seulement sans installer" };
        var historyOpt = new Option<bool>("--history") { Description = "Affiche l'historique des versions" };
        var forceOpt = new Option<bool>("--force") { Description = "Force la mise a jour meme si meme version" };
        var versionOpt = new Option<string>("--version") { Description = "Version specifique a installer" };

        Add(checkOpt);
        Add(historyOpt);
        Add(forceOpt);
        Add(versionOpt);

        SetAction(async (ParseResult pr) =>
        {
            var checkOnly = pr.GetValue(checkOpt);
            var showHistory = pr.GetValue(historyOpt);
            var force = pr.GetValue(forceOpt);
            var targetVersion = pr.GetValue(versionOpt);

            if (showHistory)
                return HandleHistory();

            return await HandleUpgrade(checkOnly, force, targetVersion);
        });
    }

    private static int HandleHistory()
    {
        ConsoleService.ShowBanner("Historique des versions", "scaffold upgrade --history");
        Console.WriteLine();

        var history = VersionManager.GetHistory(20);
        if (history.Count == 0)
        {
            ConsoleService.Info("Aucun historique disponible.");
            return 0;
        }

        var rows = history.Select(v => new[]
        {
            v.Version,
            DateTime.Parse(v.Date).ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
            v.Notes.Length > 40 ? v.Notes[..40] + "..." : v.Notes,
            v.CommitSha.Length > 8 ? v.CommitSha[..8] : v.CommitSha
        }).ToArray();

        ConsoleService.ShowTable(["Version", "Date", "Notes", "Commit"], rows);
        Console.WriteLine();
        ConsoleService.Info("Version actuelle : v" + VersionManager.GetCurrentVersion());
        return 0;
    }

    private static async Task<int> HandleUpgrade(bool checkOnly, bool force, string? targetVersion)
    {
        ConsoleService.Info("Recherche de la derniere version disponible...");

        var (latestVersion, downloadUrl) = await UpdateService.CheckForUpdateAsync();

        if (latestVersion == null)
        {
            ConsoleService.Warning("Impossible de verifier les mises a jour. Verifie ta connexion Internet.");
            return 1;
        }

        var current = UpdateService.CurrentVersion;
        ConsoleService.ShowSection("Etat des versions");
        ConsoleService.SummaryLine("Actuelle", "v" + current);
        ConsoleService.SummaryLine("Derniere", "v" + latestVersion);
        ConsoleService.SummaryLine("Statut", current == latestVersion ? "A jour" : "Mise a jour disponible");
        Console.WriteLine();

        if (!force && current == latestVersion && string.IsNullOrEmpty(targetVersion))
        {
            ConsoleService.Success("Tu as deja la derniere version !");
            return 0;
        }

        var installVersion = targetVersion ?? latestVersion;
        if (!force && targetVersion == null && VersionManager.HasUpdate(latestVersion) == false)
        {
            ConsoleService.Success("Aucune mise a jour necessaire.");
            return 0;
        }

        ConsoleService.ShowSection($"Mise a jour vers v{installVersion}");
        ConsoleService.SummaryLine("Depuis", "v" + current);
        ConsoleService.SummaryLine("Vers", "v" + installVersion);
        Console.WriteLine();

        if (checkOnly)
        {
            ConsoleService.Info("Mode --check : aucune installation.");
            return 0;
        }

        var confirm = ConsoleService.Confirm("[green]Installer la mise a jour ?[/]", true);
        if (!confirm)
        {
            ConsoleService.Warning("Mise a jour annulee.");
            return 0;
        }

        ConsoleService.Info("Telechargement et installation...");

        var success = targetVersion == null
            ? await UpdateService.DownloadAndInstallAsync(downloadUrl!)
            : await UpdateService.DownloadAndInstallAsync(
                $"https://github.com/akaletekoffilevis/Scaffolder-CLI/releases/download/v{installVersion}/scaffold-{RuntimeInfo.RID}.tar.gz");

        if (success)
        {
            VersionManager.MarkUpdateInstalled(installVersion);
            ConsoleService.Success($"Mise a jour v{installVersion} installee avec succes !");
            ConsoleService.Info("Redemarre Scaffolder pour utiliser la nouvelle version.");
            return 0;
        }

        ConsoleService.Error("Echec de la mise a jour. Essaie de telecharger manuellement depuis GitHub.");
        return 1;
    }
}
