namespace Scaffolder.Services;

public static class Paths
{
    private static readonly string HomeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public static readonly string ConfigDir = Path.Combine(HomeDir, ".scaffolder");
    public static readonly string ConfigFile = Path.Combine(ConfigDir, "config.json");
    public static readonly string DatabaseFile = Path.Combine(ConfigDir, "scaffolder.scaffolder-cli");
    public static readonly string PluginsDir = Path.Combine(ConfigDir, "plugins");
    public static readonly string RegistryDir = Path.Combine(ConfigDir, "registry");

    public static string GetPluginDir(string pluginName) => Path.Combine(PluginsDir, pluginName);
    public static string GetRegistryTemplateDir(string templateName) => Path.Combine(RegistryDir, templateName);
}
