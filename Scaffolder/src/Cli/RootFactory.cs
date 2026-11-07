using System.CommandLine;
using Scaffolder.Commands;

namespace Scaffolder.Cli;

/// <summary>Construit l'arbre de commandes complet. Partagé par le CLI, le TUI et le web.</summary>
public static class RootFactory
{
    public static RootCommand Create()
    {
        return new RootCommand("Scaffolder — CLI universel pour générer des projets")
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
            new AiCommand(),
            new TuiCommand()
        };
    }
}
