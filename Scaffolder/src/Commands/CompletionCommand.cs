using System.CommandLine;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class CompletionCommand : Command
{
    public CompletionCommand() : base("completion", "Genere les scripts d'auto-completion pour le shell")
    {
        var shellArg = new Argument<string>("shell")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Shell (bash, zsh, fish, powershell)"
        };
        Add(shellArg);
        SetAction((ParseResult pr) => HandleCompletion(pr.GetValue(shellArg)));
    }

    private static int HandleCompletion(string? shell)
    {
        shell = shell?.ToLowerInvariant() ?? DetectShell();

        string script = shell switch
        {
            "bash" => GenerateBash(),
            "zsh" => GenerateZsh(),
            "fish" => GenerateFish(),
            "powershell" or "pwsh" => GeneratePowerShell(),
            _ => ""
        };

        if (string.IsNullOrEmpty(script))
        {
            ConsoleService.Error("Shell non supporte. Choisis : bash, zsh, fish, powershell");
            return 1;
        }

        ConsoleService.Info($"Script d'auto-completion pour {shell} :");
        Console.WriteLine();
        Console.WriteLine(script);
        Console.WriteLine();
        ConsoleService.Info($"Pour installer :");
        ConsoleService.Info($"  scaffold completion {shell} >> ~/.{shell}rc");
        ConsoleService.Info($"  # ou");
        ConsoleService.Info($"  scaffold completion {shell} | source");
        return 0;
    }

    private static string DetectShell()
    {
        var shellVar = Environment.GetEnvironmentVariable("SHELL") ?? "";
        if (shellVar.Contains("zsh")) return "zsh";
        if (shellVar.Contains("fish")) return "fish";
        return "bash";
    }

    private static string GenerateBash() => """
_scaffold_completion() {
    local cur prev words cword
    _init_completion || return

    if [[ $prev == "scaffold" || $prev == "scaff" || $prev == "s" ]]; then
        COMPREPLY=($(compgen -W "new create add config upgrade suggest explain fix completion license env docker github init run build test lint format clean info registry migrate template search compare project batch watch plugin audit stack generate vscode ui bug deploy store update-deps workspace ci doctor --help --version" -- "$cur"))
        return 0
    fi

    case $prev in
        new|create)
            COMPREPLY=($(compgen -W "--name --template --language --output --dry-run --silent --no-git --verbose --fav --auth --db --arch --docker --ci" -- "$cur"))
            ;;
        add)
            COMPREPLY=($(compgen -W "controller entity repository service page migration middleware filter" -- "$cur"))
            ;;
        config)
            COMPREPLY=($(compgen -W "init get set reset profile import export" -- "$cur"))
            ;;
        suggest|fix|doctor|bug)
            COMPREPLY=()
            ;;
        explain)
            COMPREPLY=($(compgen -W "middleware mvc rest docker git jwt orm solid" -- "$cur"))
            ;;
        completion)
            COMPREPLY=($(compgen -W "bash zsh fish powershell" -- "$cur"))
            ;;
        license)
            COMPREPLY=($(compgen -W "mit apache gpl3 bsd2 isc unlicense" -- "$cur"))
            ;;
        env)
            COMPREPLY=($(compgen -W "api web mobile docker" -- "$cur"))
            ;;
        docker)
            COMPREPLY=($(compgen -W "api web full" -- "$cur"))
            ;;
        github)
            COMPREPLY=($(compgen -W "init actions scaffolder" -- "$cur"))
            ;;
        ci)
            COMPREPLY=($(compgen -W "github gitlab jenkins" -- "$cur"))
            ;;
        deploy)
            COMPREPLY=($(compgen -W "vercel railway docker pages" -- "$cur"))
            ;;
        plugin)
            COMPREPLY=($(compgen -W "list add remove info create search" -- "$cur"))
            ;;
        template)
            COMPREPLY=($(compgen -W "publish validate lock unlock history deps stats from-dir" -- "$cur"))
            ;;
        project)
            COMPREPLY=($(compgen -W "doctor upgrade analyze" -- "$cur"))
            ;;
        workspace)
            COMPREPLY=($(compgen -W "init add list" -- "$cur"))
            ;;
    esac
}

complete -F _scaffold_completion scaffold scaff s
""";

    private static string GenerateZsh() => """
#compdef scaffold scaff s

_scaffold_completion() {
    local -a commands
    commands=(
        'new:Crée un nouveau projet'
        'create:Alias de new'
        'add:Genere un composant individuel'
        'config:Gère la configuration'
        'upgrade:Met à jour Scaffolder'
        'doctor:Diagnostique l\'environnement'
        'suggest:Suggère un template'
        'explain:Explique un concept'
        'fix:Aide à corriger une erreur'
        'completion:Génère les scripts'
        'license:Genere une licence'
        'env:Genere un fichier .env'
        'docker:Genere Dockerfile'
        'github:Integration GitHub'
        'init:Initialisation complete'
        'run:Lance le projet'
        'build:Compile le projet'
        'test:Lance les tests'
        'lint:Exécute le linter'
        'format:Formate le code'
        'clean:Nettoie le projet'
        'info:Affiche les infos'
        'registry:Gestion du registry'
        'migrate:Guide de migration'
        'template:Gestion des templates'
        'search:Recherche avancee'
        'compare:Comparaison'
        'project:Diagnostics projet'
        'batch:Generation multiple'
        'watch:Surveillance auto'
        'plugin:Gestion plugins'
        'audit:Securite'
        'stack:Stack fullstack'
        'generate:Generation IA'
        'vscode:Integration VS Code'
        'ui:Interface web'
        'bug:Signaler un bug'
        'deploy:Deploiement'
        'update-deps:Mise a jour deps'
        'workspace:Monorepo'
        'ci:CI/CD'
    )

    _arguments \
        '--help[Affiche l\'aide]' \
        '--version[Affiche la version]' \
        '*:: :->subcmd'

    case $state in
        subcmd)
            _describe 'command' commands
            ;;
    esac
}

_scaffold_new() {
    _arguments \
        '--name=[Nom du projet]' \
        '--template=[Template]' \
        '--language=[Langage]' \
        '--output=[Dossier de sortie]' \
        '--dry-run[Previsualisation]' \
        '--silent[Mode silencieux]' \
        '--no-git[Pas Git]' \
        '--verbose[Mode verbeux]' \
        '--fav[Template favori]' \
        '--auth=[Authentification]' \
        '--db=[Base de donnees]' \
        '--arch=[Architecture]' \
        '--docker[Dockerfile]' \
        '--ci[CI/CD]'
}

_scaffold_add() {
    _arguments \
        ':type:(controller entity repository service page migration middleware filter)' \
        ':name:'
}

compdef _scaffold_completion scaffold scaff s
""";

    private static string GenerateFish() => """
function _scaffold_completion
    set -l cmds new create add config upgrade doctor suggest explain fix completion license env docker github init run build test lint format clean info registry migrate template search compare project batch watch plugin audit stack generate vscode ui bug deploy store update-deps workspace ci

    switch (commandline -op)[1]
        case scaffold scaff s
            for cmd in $cmds
                complete -f -c scaffold -n "not __fish_seen_subcommand_from $cmds" -a $cmd
            end
        case new create
            complete -f -c scaffold -n "__fish_seen_subcommand_from new create" -a "--name" -d "Nom du projet"
            complete -f -c scaffold -n "__fish_seen_subcommand_from new create" -a "--template" -d "Template"
            complete -f -c scaffold -n "__fish_seen_subcommand_from new create" -a "--language" -d "Langage"
            complete -f -c scaffold -n "__fish_seen_subcommand_from new create" -a "--output" -d "Dossier"
            complete -f -c scaffold -n "__fish_seen_subcommand_from new create" -a "--dry-run" -d "Previsualisation"
            complete -f -c scaffold -n "__fish_seen_subcommand_from new create" -a "--silent" -d "Mode silencieux"
            complete -f -c scaffold -n "__fish_seen_subcommand_from new create" -a "--no-git" -d "Pas Git"
            complete -f -c scaffold -n "__fish_seen_subcommand_from new create" -a "--auth" -d "Auth"
            complete -f -c scaffold -n "__fish_seen_subcommand_from new create" -a "--db" -d "BDD"
            complete -f -c scaffold -n "__fish_seen_subcommand_from new create" -a "--arch" -d "Architecture"
            complete -f -c scaffold -n "__fish_seen_subcommand_from new create" -a "--docker" -d "Docker"
            complete -f -c scaffold -n "__fish_seen_subcommand_from new create" -a "--ci" -d "CI/CD"
        case add
            complete -f -c scaffold -n "__fish_seen_subcommand_from add" -a "controller entity repository service page migration middleware filter"
        case config
            complete -f -c scaffold -n "__fish_seen_subcommand_from config" -a "init get set reset profile import export"
        case completion
            complete -f -c scaffold -n "__fish_seen_subcommand_from completion" -a "bash zsh fish powershell"
        case license
            complete -f -c scaffold -n "__fish_seen_subcommand_from license" -a "mit apache gpl3 bsd2 isc unlicense"
        case ci
            complete -f -c scaffold -n "__fish_seen_subcommand_from ci" -a "github gitlab jenkins"
        case plugin
            complete -f -c scaffold -n "__fish_seen_subcommand_from plugin" -a "list add remove info create search"
        case template
            complete -f -c scaffold -n "__fish_seen_subcommand_from template" -a "publish validate lock unlock history deps stats from-dir"
    end
end

_scaffold_completion
""";

    private static string GeneratePowerShell() => """
Register-ArgumentCompleter -Native -CommandName scaffold -ScriptBlock {
    param($wordToComplete, $commandAst, $cursorPosition)
    $commands = @('new', 'create', 'add', 'config', 'upgrade', 'doctor', 'suggest', 'explain', 'fix',
                  'completion', 'license', 'env', 'docker', 'github', 'init', 'run', 'build', 'test',
                  'lint', 'format', 'clean', 'info', 'registry', 'migrate', 'template', 'search',
                  'compare', 'project', 'batch', 'watch', 'plugin', 'audit', 'stack', 'generate',
                  'vscode', 'ui', 'bug', 'deploy', 'store', 'update-deps', 'workspace', 'ci')

    switch ($commandAst.CommandElements[1].Value) {
        'new' {
            @('--name', '--template', '--language', '--output', '--dry-run', '--silent', '--no-git',
              '--verbose', '--fav', '--auth', '--db', '--arch', '--docker', '--ci')
        }
        'create' {
            @('--name', '--template', '--language', '--output', '--dry-run', '--silent', '--no-git',
              '--verbose', '--fav', '--auth', '--db', '--arch', '--docker', '--ci')
        }
        'add' {
            @('controller', 'entity', 'repository', 'service', 'page', 'migration', 'middleware', 'filter')
        }
        'config' {
            @('init', 'get', 'set', 'reset', 'profile', 'import', 'export')
        }
        'completion' {
            @('bash', 'zsh', 'fish', 'powershell')
        }
        'license' {
            @('mit', 'apache', 'gpl3', 'bsd2', 'isc', 'unlicense')
        }
        'ci' {
            @('github', 'gitlab', 'jenkins')
        }
        'plugin' {
            @('list', 'add', 'remove', 'info', 'create', 'search')
        }
        'template' {
            @('publish', 'validate', 'lock', 'unlock', 'history', 'deps', 'stats', 'from-dir')
        }
        default {
            $commands | Where-Object { $_ -like "$wordToComplete*" }
        }
    }
}
""";
}
