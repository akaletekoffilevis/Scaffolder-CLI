using System.Text.Json;
using Scaffolder;

namespace Scaffolder.Services;

public static class ConfigService
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".scaffolder");
    private static readonly string ConfigFile = Path.Combine(ConfigDir, "config.json");

    public static class Keys
    {
        public const string ApiKey = "apiKey";
        public const string Provider = "provider";
        public const string Model = "model";
        public const string Theme = "theme";
        public const string Experience = "experience";
        public const string FirstRun = "firstRun";
    }

    public static string? Get(string key)
    {
        if (!ReadConfig(out var dict))
            return null;

        return dict.TryGetValue(key, out var val) ? val : null;
    }

    public static void Set(string key, string value)
    {
        if (!ReadConfig(out var dict))
            dict = new Dictionary<string, string>();

        dict[key] = value;
        SaveConfig(dict);
    }

    private static bool ReadConfig(out Dictionary<string, string> dict)
    {
        dict = [];
        if (!File.Exists(ConfigFile))
            return false;

        try
        {
            var json = File.ReadAllText(ConfigFile);
            dict = JsonSerializer.Deserialize(json, JsonContext.Default.DictionaryStringString) ?? [];
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void SaveConfig(Dictionary<string, string> dict)
    {
        Directory.CreateDirectory(ConfigDir);
        var json = JsonSerializer.Serialize(dict, JsonContext.Default.DictionaryStringString);
        File.WriteAllText(ConfigFile, json);
    }
}
