using System.Text.Json;
using Scaffolder;

namespace Scaffolder.Services;

public static class VersionManager
{
    private const string Section = "versions";
    private const string CurrentKey = "current";
    private const string HistoryKey = "history";

    public static void RecordCurrentVersion(string version, string? releaseNotes = null, string? commitSha = null)
    {
        var db = DatabaseService.LoadDatabase();
        var now = DateTime.UtcNow.ToString("o");

        // Update current version
        DatabaseService.SetValue(Section, CurrentKey, version);
        DatabaseService.SetValue(Section, "current.date", now);
        if (releaseNotes != null) DatabaseService.SetValue(Section, "current.notes", releaseNotes);
        if (commitSha != null) DatabaseService.SetValue(Section, "current.sha", commitSha);

        // Add to history
        var history = DatabaseService.GetSection(Section + ".history");
        var entry = $"{version}|{now}|{releaseNotes ?? ""}|{commitSha ?? ""}";
        history[$"v{version}_{now[..10]}"] = entry;

        // Keep last 50 versions
        if (history.Count > 50)
        {
            var sorted = history.OrderBy(kv => kv.Key).Take(history.Count - 50).Select(kv => kv.Key).ToList();
            foreach (var k in sorted) history.Remove(k);
        }

        DatabaseService.SetSection(Section + ".history", history);
    }

    public static string GetCurrentVersion()
    {
        return DatabaseService.GetValue(Section, CurrentKey) ?? UpdateService.CurrentVersion;
    }

    public static VersionInfo? GetVersionInfo(string version)
    {
        version = version ?? throw new ArgumentNullException(nameof(version));
        var history = DatabaseService.GetSection(Section + ".history");
        foreach (var entry in history.Values)
        {
            var parts = entry.Split('|', 4);
            if (parts[0] == version || parts[0] == "v" + version)
            {
                return new VersionInfo(parts[0], parts[1], parts[2], parts[3]);
            }
        }
        return null;
    }

    public static List<VersionInfo> GetHistory(int limit = 20)
    {
        var history = DatabaseService.GetSection(Section + ".history");
        return history.Values
            .Select(v =>
            {
                var parts = v.Split('|', 4);
                return new VersionInfo(parts[0], parts[1], parts[2], parts[3]);
            })
            .OrderByDescending(v => v.Date)
            .Take(limit)
            .ToList();
    }

    public static bool HasUpdate(string latestVersion)
    {
        latestVersion = latestVersion ?? throw new ArgumentNullException(nameof(latestVersion));
        var current = GetCurrentVersion();
        return VersionComparer.Compare(latestVersion, current) > 0;
    }

    public static void MarkUpdateInstalled(string version, string? releaseNotes = null, string? commitSha = null)
    {
        RecordCurrentVersion(version, releaseNotes, commitSha);
        ConfigService.Set(ConfigService.Keys.FirstRun, "false");
    }

    public static void CleanupOldHistory(int keep = 10)
    {
        var history = DatabaseService.GetSection(Section + ".history");
        if (history.Count <= keep) return;

        var sorted = history.OrderBy(kv => kv.Key).Take(history.Count - keep).Select(kv => kv.Key).ToList();
        foreach (var k in sorted) history.Remove(k);
        DatabaseService.SetSection(Section + ".history", history);
    }

    public record VersionInfo(string Version, string Date, string Notes, string CommitSha);
}

public static class VersionComparer
{
    public static int Compare(string v1, string v2)
    {
        v1 = v1 ?? throw new ArgumentNullException(nameof(v1));
        v2 = v2 ?? throw new ArgumentNullException(nameof(v2));
        var p1 = v1.TrimStart('v').Split('.');
        var p2 = v2.TrimStart('v').Split('.');

        for (int i = 0; i < Math.Max(p1.Length, p2.Length); i++)
        {
            int n1 = i < p1.Length ? int.Parse(p1[i]) : 0;
            int n2 = i < p2.Length ? int.Parse(p2[i]) : 0;

            var cmp = n1.CompareTo(n2);
            if (cmp != 0) return cmp;
        }
        return 0;
    }
}