using System.CommandLine;
using System.Linq;
using Scaffolder.Commands;
using Scaffolder.Services;

var rootCommand = new RootCommand("Scaffolder — CLI universel pour générer des projets")
{
    new NewCommand(),
    new VersionCommand(),
    new UpgradeCommand(),
    new ConfigCommand(),
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
    new WorkspaceCommand()
};

var builtinVer = rootCommand.Options.OfType<VersionOption>().FirstOrDefault();
if (builtinVer != null) rootCommand.Options.Remove(builtinVer);
var versionOpt = new Option<bool>("--version", new[] { "-v" }) { Description = "Affiche la version" };
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
    pr.RootCommandResult.Command.SetAction((ParseResult _) =>
    {
        ConsoleService.Info("Utilise `scaffold --help` pour voir toutes les commandes.");
        ConsoleService.Info("Utilise `scaffold new` pour créer ton premier projet.");
        return 0;
    });
    return 0;
});

var config = new InvocationConfiguration();
return await rootCommand.Parse(args).InvokeAsync(config);
