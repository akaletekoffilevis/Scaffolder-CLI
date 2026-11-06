using System.Globalization;
using Scaffolder;

namespace Scaffolder.Services;

public static class VersionManager
{
    private const string Section = "versions";
    private const string CurrentKey = "current";
    private const string HistoryKey = "history";

    public static void RecordCurrentVersion(string version, string? releaseNotes = null, string? commitSha = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        var now = DateTime.UtcNow.ToString("o");

        DatabaseService.SetValue(Section, CurrentKey, version);
        DatabaseService.SetValue(Section, "current.date", now);
        if (releaseNotes != null) DatabaseService.SetValue(Section, "current.notes", releaseNotes);
        if (commitSha != null) DatabaseService.SetValue(Section, "current.sha", commitSha);

        var history = DatabaseService.GetSection(Section + "." + HistoryKey);
        var entry = string.Join('|', version, now, releaseNotes ?? "", commitSha ?? "");
        history[$"v{version}_{now[..10]}"] = entry;

        if (history.Count > 50)
        {
            var oldest = history.OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .Take(history.Count - 50)
                .Select(kv => kv.Key)
                .ToList();
            foreach (var k in oldest) history.Remove(k);
        }

        DatabaseService.SetSection(Section + "." + HistoryKey, history);
    }

    public static string GetCurrentVersion()
    {
        return DatabaseService.GetValue(Section, CurrentKey) ?? UpdateService.CurrentVersion;
    }

    public static VersionInfo? GetVersionInfo(string version)
    {
        ArgumentNullException.ThrowIfNull(version);

        var history = DatabaseService.GetSection(Section + "." + HistoryKey);
        foreach (var entry in history.Values)
        {
            var parsed = ParseEntry(entry);
            if (parsed == null) continue;

            if (string.Equals(parsed.Version, version, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(parsed.Version, "v" + version, StringComparison.OrdinalIgnoreCase))
            {
                return parsed;
            }
        }
        return null;
    }

    public static List<VersionInfo> GetHistory(int limit = 20)
    {
        var history = DatabaseService.GetSection(Section + "." + HistoryKey);

        return history.Values
            .Select(ParseEntry)
            .OfType<VersionInfo>()
            .OrderByDescending(v => v.Date, StringComparer.Ordinal)
            .Take(Math.Max(0, limit))
            .ToList();
    }

    public static bool HasUpdate(string latestVersion)
    {
        ArgumentNullException.ThrowIfNull(latestVersion);
        return VersionComparer.Compare(latestVersion, GetCurrentVersion()) > 0;
    }

    public static void MarkUpdateInstalled(string version, string? releaseNotes = null, string? commitSha = null)
    {
        RecordCurrentVersion(version, releaseNotes, commitSha);
        ConfigService.Set(ConfigService.Keys.FirstRun, "false");
    }

    public static void CleanupOldHistory(int keep = 10)
    {
        var history = DatabaseService.GetSection(Section + "." + HistoryKey);
        if (history.Count <= keep) return;

        var oldest = history.OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Take(history.Count - keep)
            .Select(kv => kv.Key)
            .ToList();
        foreach (var k in oldest) history.Remove(k);
        DatabaseService.SetSection(Section + "." + HistoryKey, history);
    }

    private static VersionInfo? ParseEntry(string entry)
    {
        var parts = entry.Split('|', 4);
        if (parts.Length < 2) return null;
        return new VersionInfo(
            parts[0],
            parts[1],
            parts.Length > 2 ? parts[2] : "",
            parts.Length > 3 ? parts[3] : "");
    }

    public record VersionInfo(string Version, string Date, string Notes, string CommitSha);
}

/// <summary>
/// Comparaison de versions tolerant les prefixes "v", les prereleases (1.2.0-beta.2)
/// et les suffixes de build. Ne leve jamais d'exception sur une entree invalide.
/// </summary>
public static class VersionComparer
{
    public static int Compare(string v1, string v2)
    {
        ArgumentNullException.ThrowIfNull(v1);
        ArgumentNullException.ThrowIfNull(v2);

        var a = Parse(v1);
        var b = Parse(v2);

        var cmp = a.Major.CompareTo(b.Major);
        if (cmp != 0) return cmp;

        cmp = a.Minor.CompareTo(b.Minor);
        if (cmp != 0) return cmp;

        cmp = a.Patch.CompareTo(b.Patch);
        if (cmp != 0) return cmp;

        return ComparePrerelease(a.Prerelease, b.Prerelease);
    }

    public static bool IsNewer(string candidate, string current) => Compare(candidate, current) > 0;

    public static bool TryParse(string? value, out VersionParts parts)
    {
        parts = default;
        if (string.IsNullOrWhiteSpace(value)) return false;

        try
        {
            parts = Parse(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static VersionParts Parse(string value)
    {
        var text = value.Trim();

        if (text.StartsWith('v') || text.StartsWith('V'))
            text = text[1..];

        // Ignore les metadonnees de build : 1.2.3+build.4
        var plus = text.IndexOf('+');
        if (plus >= 0) text = text[..plus];

        string? prerelease = null;
        var dash = text.IndexOf('-');
        if (dash >= 0)
        {
            prerelease = text[(dash + 1)..];
            text = text[..dash];
        }

        var numbers = text.Split('.', StringSplitOptions.RemoveEmptyEntries);
        return new VersionParts(
            ParseComponent(numbers, 0),
            ParseComponent(numbers, 1),
            ParseComponent(numbers, 2),
            string.IsNullOrWhiteSpace(prerelease) ? null : prerelease);
    }

    private static int ParseComponent(string[] parts, int index)
    {
        if (index >= parts.Length) return 0;

        var digits = new string(parts[index].TakeWhile(char.IsDigit).ToArray());
        return int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;
    }

    private static int ComparePrerelease(string? a, string? b)
    {
        // Une version sans prerelease est plus recente que la meme version en prerelease.
        if (a == null && b == null) return 0;
        if (a == null) return 1;
        if (b == null) return -1;

        var idsA = a.Split('.');
        var idsB = b.Split('.');

        for (var i = 0; i < Math.Max(idsA.Length, idsB.Length); i++)
        {
            if (i >= idsA.Length) return -1;
            if (i >= idsB.Length) return 1;

            var numericA = int.TryParse(idsA[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out var na);
            var numericB = int.TryParse(idsB[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out var nb);

            int cmp;
            if (numericA && numericB)
                cmp = na.CompareTo(nb);
            else if (numericA)
                cmp = -1; // les identifiants numeriques precedent les alphanumeriques
            else if (numericB)
                cmp = 1;
            else
                cmp = string.Compare(idsA[i], idsB[i], StringComparison.OrdinalIgnoreCase);

            if (cmp != 0) return cmp;
        }

        return 0;
    }

    public readonly record struct VersionParts(int Major, int Minor, int Patch, string? Prerelease);
}
