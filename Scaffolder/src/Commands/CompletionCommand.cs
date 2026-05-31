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

    if [[ $prev == "scaffold" || $prev == "s" ]]; then
        COMPREPLY=($(compgen -W "new config upgrade suggest explain fix completion run build test lint format clean info --help --version" -- "$cur"))
        return 0
    fi

    case $prev in
        new)
            COMPREPLY=($(compgen -W "--name --template --language --output --dry-run --silent --no-git" -- "$cur"))
            ;;
        config)
            COMPREPLY=($(compgen -W "init get set reset" -- "$cur"))
            ;;
        suggest|fix)
            COMPREPLY=()
            ;;
        explain)
            COMPREPLY=($(compgen -W "middleware mvc rest docker git jwt orm solid" -- "$cur"))
            ;;
        completion)
            COMPREPLY=($(compgen -W "bash zsh fish powershell" -- "$cur"))
            ;;
    esac
}

complete -F _scaffold_completion scaffold s
""";

    private static string GenerateZsh() => """
#compdef scaffold s

_scaffold_completion() {
    local -a commands
    commands=(
        'new:Crée un nouveau projet'
        'config:Gère la configuration'
        'upgrade:Met à jour Scaffolder'
        'suggest:Suggère un template'
        'explain:Explique un concept'
        'fix:Aide à corriger une erreur'
        'completion:Génère les scripts'
        'run:Lance le projet'
        'build:Compile le projet'
        'test:Lance les tests'
        'lint:Exécute le linter'
        'format:Formate le code'
        'clean:Nettoie le projet'
        'info:Affiche les infos'
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
        '--dry-run[Prévisualisation]' \
        '--silent[Mode silencieux]' \
        '--no-git[Pas Git]'
}

compdef _scaffold_completion scaffold s
""";

    private static string GenerateFish() => """
function _scaffold_completion
    set -l cmds new config upgrade suggest explain fix completion run build test lint format clean info

    switch (commandline -op)[1]
        case scaffold s
            for cmd in $cmds
                complete -f -c scaffold -n "not __fish_seen_subcommand_from $cmds" -a $cmd
            end
        case new
            complete -f -c scaffold -n "__fish_seen_subcommand_from new" -a "--name" -d "Nom du projet"
            complete -f -c scaffold -n "__fish_seen_subcommand_from new" -a "--template" -d "Template"
            complete -f -c scaffold -n "__fish_seen_subcommand_from new" -a "--language" -d "Langage"
            complete -f -c scaffold -n "__fish_seen_subcommand_from new" -a "--output" -d "Dossier"
            complete -f -c scaffold -n "__fish_seen_subcommand_from new" -a "--dry-run" -d "Prévisualisation"
            complete -f -c scaffold -n "__fish_seen_subcommand_from new" -a "--silent" -d "Mode silencieux"
            complete -f -c scaffold -n "__fish_seen_subcommand_from new" -a "--no-git" -d "Pas Git"
        case config
            complete -f -c scaffold -n "__fish_seen_subcommand_from config" -a "init get set reset"
        case completion
            complete -f -c scaffold -n "__fish_seen_subcommand_from completion" -a "bash zsh fish powershell"
    end
end

_scaffold_completion
""";

    private static string GeneratePowerShell() => """
Register-ArgumentCompleter -Native -CommandName scaffold -ScriptBlock {
    param($wordToComplete, $commandAst, $cursorPosition)
    $commands = @('new', 'config', 'upgrade', 'suggest', 'explain', 'fix',
                  'completion', 'run', 'build', 'test', 'lint', 'format', 'clean', 'info')

    switch ($commandAst.CommandElements[1].Value) {
        'new' {
            @('--name', '--template', '--language', '--output', '--dry-run', '--silent', '--no-git')
        }
        'config' {
            @('init', 'get', 'set', 'reset')
        }
        'completion' {
            @('bash', 'zsh', 'fish', 'powershell')
        }
        default {
            $commands | Where-Object { $_ -like "$wordToComplete*" }
        }
    }
}
""";
}
