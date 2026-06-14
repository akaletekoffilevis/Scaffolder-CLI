using System.CommandLine;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class BugCommand : Command
{
    public BugCommand() : base("bug", "Signale un bug par e-mail ou GitHub")
    {
        var descArg = new Argument<string>("description")
        {
            Description = "Description du bug",
            Arity = ArgumentArity.ZeroOrOne
        };
        Add(descArg);
        SetAction((ParseResult pr) => Handle(pr.GetValue(descArg)));
    }

    private static int Handle(string? description)
    {
        var email = "koffilevis21@gmail.com";
        var subject = Uri.EscapeDataString("[Scaffolder Bug] Rapport");
        var body = Uri.EscapeDataString(
            $"Description : {description ?? "(non fournie)"}\n\n" +
            $"--- Informations système ---\n" +
            $"OS: {RuntimeInformation.OSDescription}\n" +
            $"Architecture: {RuntimeInformation.OSArchitecture}\n" +
            $".NET: {RuntimeInformation.FrameworkDescription}\n" +
            $"Version: 2.1.0\n");

        var mailto = $"mailto:{email}?subject={subject}&body={body}";

        ConsoleService.Info("Signaler un bug :");
        Console.WriteLine();
        Console.WriteLine($"  Email  : {email}");
        Console.WriteLine($"  Sujet  : [Scaffolder Bug] Rapport");
        Console.WriteLine();
        ConsoleService.Info("Ouverture du client mail...");

        try
        {
            var psi = new ProcessStartInfo { FileName = mailto, UseShellExecute = true };
            Process.Start(psi);
            ConsoleService.Success("Client mail ouvert.");
        }
        catch
        {
            ConsoleService.Info("Impossible d'ouvrir le client mail automatiquement.");
            ConsoleService.Info("Envoie manuellement à " + email);
            Console.WriteLine();
            Console.WriteLine($"  mailto:{email}?subject={subject}&body={body}");
        }

        Console.WriteLine();
        ConsoleService.Info("Alternative : ouvre une issue GitHub directement :");
        var issueUrl = "https://github.com/akaletekoffilevis/Scaffolder-CLI/issues/new?labels=bug&template=bug_report.md";
        Console.WriteLine($"  {issueUrl}");

        return 0;
    }
}
