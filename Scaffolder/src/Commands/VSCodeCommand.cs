using System.CommandLine;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class VSCodeCommand : Command
{
    public VSCodeCommand() : base("vscode", "Genere une extension VS Code ou configure le projet")
    {
        var initCmd = new Command("init", "Cree un squelette d'extension VS Code");
        var nameArg = new Argument<string>("name") { Description = "Nom de l'extension" };
        initCmd.Add(nameArg);
        initCmd.SetAction((ParseResult pr) => HandleInit(pr.GetValue(nameArg)));
        Add(initCmd);

        var settingsCmd = new Command("settings", "Genere .vscode/settings.json pour le projet");
        settingsCmd.SetAction(_ => HandleSettings());
        Add(settingsCmd);

        var launchCmd = new Command("launch", "Genere .vscode/launch.json pour le projet");
        launchCmd.SetAction(_ => HandleLaunch());
        Add(launchCmd);

        SetAction(_ =>
        {
            ConsoleService.Info("Sous-commandes : init, settings, launch");
            ConsoleService.Info("Exemple : scaffold vscode init mon-extension");
            return 0;
        });
    }

    private static int HandleInit(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            ConsoleService.Error("Usage : scaffold vscode init <nom-extension>");
            return 1;
        }

        var dir = Path.Combine(Directory.GetCurrentDirectory(), name);
        if (Directory.Exists(dir))
        {
            ConsoleService.Error($"Le dossier '{name}' existe deja.");
            return 1;
        }

        Directory.CreateDirectory(dir);
        Directory.CreateDirectory(Path.Combine(dir, "src"));

        // package.json
        File.WriteAllText(Path.Combine(dir, "package.json"), $$"""
{
  "name": "{{name}}",
  "displayName": "{{name}}",
  "description": "Extension VS Code generee par Scaffolder",
  "version": "0.1.0",
  "publisher": "{{Environment.UserName}}",
  "engines": { "vscode": "^1.96.0" },
  "categories": ["Other"],
  "activationEvents": [],
  "main": "./src/extension.js",
  "contributes": {
    "commands": [{
      "command": "{{name}}.hello",
      "title": "Hello from {{name}}"
    }]
  }
}
""");

        // extension.js
        File.WriteAllText(Path.Combine(dir, "src", "extension.js"), $$"""
const vscode = require('vscode');

function activate(context) {
  console.log('Extension "{{name}}" activee !');

  const disposable = vscode.commands.registerCommand('{{name}}.hello', () => {
    vscode.window.showInformationMessage('Hello from {{name}} !');
  });

  context.subscriptions.push(disposable);
}

function deactivate() {}

module.exports = { activate, deactivate };
""");

        // .vscodeignore
        File.WriteAllText(Path.Combine(dir, ".vscodeignore"), """
.vscode/**
.gitignore
node_modules/
src/**
!src/extension.js
""");

        // README
        File.WriteAllText(Path.Combine(dir, "README.md"), $$"""
# {{name}}

Extension VS Code generee par Scaffolder.

## Installation
1. Ouvrir le dossier dans VS Code
2. `npm install`
3. F5 pour lancer le debug
""");

        ConsoleService.Success($"Extension VS Code '{name}' creee.");
        ConsoleService.Info($"  {dir}");
        ConsoleService.Info("  Pour tester : ouvrir le dossier dans VS Code et presser F5");
        return 0;
    }

    private static int HandleSettings()
    {
        var cwd = Directory.GetCurrentDirectory();
        var vscodeDir = Path.Combine(cwd, ".vscode");
        Directory.CreateDirectory(vscodeDir);

        var settings = new Dictionary<string, object>();

        // Detect project type
        var files = Directory.GetFiles(cwd);
        if (files.Any(f => f.EndsWith(".csproj")))
        {
            settings["dotnet.enable"] = true;
            settings["editor.formatOnSave"] = true;
            settings["omnisharp.enableRoslynAnalyzers"] = true;
        }
        else if (files.Any(f => Path.GetFileName(f) == "package.json"))
        {
            settings["editor.formatOnSave"] = true;
            settings["editor.defaultFormatter"] = "esbenp.prettier-vscode";
            settings["eslint.enable"] = true;
            settings["typescript.validate.enable"] = true;
        }
        else if (files.Any(f => Path.GetFileName(f) == "Cargo.toml"))
        {
            settings["rust-analyzer.checkOnSave"] = true;
            settings["editor.formatOnSave"] = true;
        }
        else if (files.Any(f => Path.GetFileName(f) == "go.mod"))
        {
            settings["go.useLanguageServer"] = true;
            settings["editor.formatOnSave"] = true;
        }

        settings["files.exclude"] = new Dictionary<string, bool>
        {
            ["**/bin"] = true,
            ["**/obj"] = true,
            ["**/node_modules"] = true,
            ["**/target"] = true,
        };

        var json = "{\n" + string.Join(",\n", settings.Select(kv =>
        {
            if (kv.Value is bool b)
                return $"  \"{kv.Key}\": {b.ToString().ToLowerInvariant()}";
            if (kv.Value is Dictionary<string, bool> dict)
                return $"  \"{kv.Key}\": {{\n" +
                       string.Join(",\n", dict.Select(d => $"    \"{d.Key}\": {d.Value.ToString().ToLowerInvariant()}")) +
                       "\n  }";
            return $"  \"{kv.Key}\": \"{kv.Value}\"";
        })) + "\n}\n";

        File.WriteAllText(Path.Combine(vscodeDir, "settings.json"), json);
        ConsoleService.Success(".vscode/settings.json genere.");
        return 0;
    }

    private static int HandleLaunch()
    {
        var cwd = Directory.GetCurrentDirectory();
        var vscodeDir = Path.Combine(cwd, ".vscode");
        Directory.CreateDirectory(vscodeDir);

        var files = Directory.GetFiles(cwd);
        string config;

        if (files.Any(f => f.EndsWith(".csproj")))
        {
            config = """
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Lancer le projet",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/bin/Debug/net9.0/scaffold.dll",
      "args": [],
      "cwd": "${workspaceFolder}"
    }
  ]
}
""";
        }
        else if (files.Any(f => Path.GetFileName(f) == "package.json"))
        {
            config = """
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Lancer avec npm",
      "type": "node",
      "request": "launch",
      "runtimeExecutable": "npm",
      "runtimeArgs": ["run", "dev"],
      "cwd": "${workspaceFolder}"
    }
  ]
}
""";
        }
        else
        {
            config = """
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Lancer",
      "type": "node",
      "request": "launch",
      "cwd": "${workspaceFolder}"
    }
  ]
}
""";
        }

        File.WriteAllText(Path.Combine(vscodeDir, "launch.json"), config);
        ConsoleService.Success(".vscode/launch.json genere.");
        return 0;
    }
}
