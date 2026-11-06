using System.CommandLine;
using System.Linq;
using Scaffolder.Commands;
using Scaffolder.Services;

// Les options globales (--yes, --no-color, --verbose, --quiet) sont extraites avant
// l'analyse par System.CommandLine : elles fonctionnent donc sur toutes les commandes.
var scaffoldArgs = AppOptions.Parse(args);

try
{
    DatabaseService.Initialize();
}
catch (Exception ex)
{
    ConsoleService.Warning($"Impossible initialiser la base locale : {ex.Message}");
}

var rootCommand = new RootCommand("Scaffolder — CLI universel pour générer des projets")
{
    new NewCommand(),
    new AddCommand(),
    new VersionCommand(),
    new UpgradeCommand(),
    new ConfigCommand(),
    new DataCommand(),
    new DoctorCommand(),
    new SuggestCommand(),
    new ExplainCommand(),
    new FixCommand(),
    new CompletionCommand(),
    new LicenseCommand(),
    new EnvCommand(),
    new DockerCommand(),
    new GitHubCommand(),
    new InitCommand(),
    new RunCommand(),
    new BuildCommand(),
    new TestCommand(),
    new CleanCommand(),
    new InfoCommand(),
    new LintCommand(),
    new FormatCommand(),
    new RegistryCommand(),
    new MigrateCommand(),
    new TemplateCommand(),
    new SearchCommand(),
    new CompareCommand(),
    new ProjectCommand(),
    new BatchCommand(),
    new WatchCommand(),
    new PluginCommand(),
    new AuditCommand(),
    new StackCommand(),
    new GenerateCommand(),
    new VSCodeCommand(),
    new UICommand(),
    new BugCommand(),
    new DeployCommand(),
    new StoreCommand(),
    new UpdateDepsCommand(),
    new WorkspaceCommand(),
    new CICommand(),
    new ListCommand(),
    new StatsCommand(),
    new AiCommand()
};

rootCommand.Aliases.Add("scaff");
rootCommand.Aliases.Add("s");

var builtinVer = rootCommand.Options.OfType<VersionOption>().FirstOrDefault();
if (builtinVer != null) rootCommand.Options.Remove(builtinVer);
var versionOpt = new Option<bool>("--version", ["-v"]) { Description = "Affiche la version" };
rootCommand.Options.Add(versionOpt);

rootCommand.SetAction((ParseResult pr) =>
{
    ConsoleService.CheckFirstRun();

    if (pr.GetValue(versionOpt))
    {
        Console.WriteLine($"Scaffolder v{UpdateService.CurrentVersion}");
        return 0;
    }

    ConsoleService.ShowLogo();
    Console.WriteLine();
    ConsoleService.Info("Utilise `scaffold --help` pour voir toutes les commandes.");
    ConsoleService.Info("Utilise `scaffold new` pour créer ton premier projet.");
    return 0;
});

try
{
    var config = new InvocationConfiguration();
    return await rootCommand.Parse(scaffoldArgs).InvokeAsync(config);
}
catch (OperationCanceledException)
{
    ConsoleService.Warning("Operation annulee.");
    return 130;
}
catch (Exception ex)
{
    ConsoleService.Error($"Erreur inattendue : {ex.Message}");

    if (AppOptions.Verbose)
    {
        Console.WriteLine();
        Console.WriteLine(ex);
    }
    else
    {
        ConsoleService.Info("Relance avec --verbose pour plus de details.");
    }

    return 1;
}
