using System.Text.Json;
using Scaffolder;

namespace Scaffolder.Services;

public static class ConfigService
{
    public static class Keys
    {
        public const string ApiKey = "apiKey";
        public const string Provider = "provider";
        public const string Model = "model";
        public const string Theme = "theme";
        public const string Experience = "experience";
        public const string FirstRun = "firstRun";
        public const string Fav = "fav";
        public const string Profile = "profile";
        public const string RegistryUrl = "registry.url";
        public const string BugEmail = "bugEmail";
        public const string Author = "author";
        public const string Company = "company";
        public const string Framework = "framework";
        public const string License = "license";
        public const string DefaultAuth = "defaultAuth";
        public const string DefaultDb = "defaultDb";
        public const string DefaultArch = "defaultArch";

        public static string LockKey(string template) => $"lock.{template}";
    }

    public static string? Get(string key)
    {
        return DatabaseService.GetValue("settings", key);
    }

    public static void Set(string key, string value)
    {
        DatabaseService.SetValue("settings", key, value);
    }

    public static bool ReadConfig(out Dictionary<string, string> dict)
    {
        dict = DatabaseService.GetSection("settings");
        return dict.Count > 0;
    }

    public static void SaveConfig(Dictionary<string, string> dict)
    {
        DatabaseService.SetSection("settings", dict);
    }
}
