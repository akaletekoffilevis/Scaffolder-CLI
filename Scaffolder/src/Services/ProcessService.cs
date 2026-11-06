using System.Diagnostics;
using System.Text;

namespace Scaffolder.Services;

/// <summary>Resultat detaille de l'execution d'un processus.</summary>
public sealed record ProcessResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    TimeSpan Duration,
    bool TimedOut)
{
    /// <summary>Sortie combinee (stdout + stderr), comme l'ancienne API.</summary>
    public string Output => (StandardOutput + StandardError).Trim();

    public bool Success => ExitCode == 0;
}

public static class ProcessService
{
    /// <summary>Delai maximal par defaut avant d'arreter un processus.</summary>
    public static TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Execute un processus. Les arguments sont decoupes de maniere sure (jamais via un shell).
    /// </summary>
    public static async Task<(int ExitCode, string Output)> RunAsync(
        string fileName, string arguments, string workingDirectory = "",
        bool streamOutput = false)
    {
        var result = await RunResultAsync(
            fileName, Tokenize(arguments), workingDirectory, streamOutput,
            DefaultTimeout, CancellationToken.None).ConfigureAwait(false);
        return (result.ExitCode, result.Output);
    }

    /// <summary>Execute un processus avec une liste d'arguments (recommande : aucune injection possible).</summary>
    public static Task<ProcessResult> RunResultAsync(
        string fileName, IEnumerable<string> arguments, string workingDirectory = "",
        bool streamOutput = false, TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(arguments);

        return RunCoreAsync(
            fileName,
            arguments as IList<string> ?? arguments.ToList(),
            workingDirectory,
            streamOutput,
            timeout ?? DefaultTimeout,
            cancellationToken);
    }

    private static async Task<ProcessResult> RunCoreAsync(
        string fileName, IList<string> arguments, string workingDirectory,
        bool streamOutput, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory)
                ? Environment.CurrentDirectory
                : workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (var arg in arguments)
            psi.ArgumentList.Add(arg);

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        var stopwatch = Stopwatch.StartNew();

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null) return;
            lock (stdout) stdout.AppendLine(e.Data);
            if (streamOutput) ConsoleService.WriteCmdLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null) return;
            lock (stderr) stderr.AppendLine(e.Data);
            if (streamOutput) ConsoleService.WriteCmdLine(e.Data, ConsoleColor.Red);
        };

        try
        {
            process.Start();
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or FileNotFoundException)
        {
            stopwatch.Stop();
            return new ProcessResult(127, "", $"Commande introuvable : {fileName} ({ex.Message})", stopwatch.Elapsed, false);
        }

        // Ferme stdin : evite qu'un programme attende une saisie indefiniment.
        try { process.StandardInput.Close(); } catch { /* ignore */ }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var timedOut = false;

        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            await process.WaitForExitAsync(linkedCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            timedOut = !cancellationToken.IsCancellationRequested;
            TryKill(process);
            try
            {
                await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch { /* ignore */ }
        }

        // Laisse les evenements de sortie se vider.
        try { process.WaitForExit(); } catch { /* ignore */ }

        stopwatch.Stop();

        string outText, errText;
        lock (stdout) outText = stdout.ToString();
        lock (stderr) errText = stderr.ToString();

        if (timedOut)
            errText += $"\nDelai depasse ({timeout.TotalSeconds:F0}s) : le processus a ete arrete.";

        var exitCode = timedOut ? 124 : SafeExitCode(process);

        return new ProcessResult(exitCode, outText.TrimEnd(), errText.TrimEnd(), stopwatch.Elapsed, timedOut);
    }

    private static int SafeExitCode(Process process)
    {
        try { return process.ExitCode; }
        catch { return -1; }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch { /* ignore */ }
    }

    /// <summary>
    /// Indique si une commande est disponible dans le PATH (recherche directe, sans lancer de processus).
    /// </summary>
    public static bool CommandExists(string command)
    {
        if (string.IsNullOrWhiteSpace(command)) return false;

        if (command.Contains(Path.DirectorySeparatorChar) ||
            command.Contains(Path.AltDirectorySeparatorChar))
        {
            return File.Exists(command);
        }

        foreach (var candidate in CandidatePaths(command))
        {
            if (File.Exists(candidate)) return true;
        }

        return false;
    }

    /// <summary>Retourne le chemin absolu d'une commande, ou null si introuvable.</summary>
    public static string? FindCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command)) return null;

        if (command.Contains(Path.DirectorySeparatorChar) ||
            command.Contains(Path.AltDirectorySeparatorChar))
        {
            return File.Exists(command) ? Path.GetFullPath(command) : null;
        }

        return CandidatePaths(command).FirstOrDefault(File.Exists);
    }

    private static IEnumerable<string> CandidatePaths(string command)
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        var directories = path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

        var names = new List<string> { command };

        if (OperatingSystem.IsWindows())
        {
            var pathext = (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.CMD;.BAT;.COM")
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (!Path.HasExtension(command))
                names.AddRange(pathext.Select(ext => command + ext.ToLowerInvariant()));
        }

        foreach (var dir in directories)
        {
            foreach (var name in names)
            {
                string candidate;
                try { candidate = Path.Combine(dir.Trim('"'), name); }
                catch { continue; }
                yield return candidate;
            }
        }
    }

    /// <summary>
    /// Decoupe une ligne d'arguments en respectant les guillemets simples/doubles et les echappements.
    /// Aucun shell n'est invoque.
    /// </summary>
    public static List<string> Tokenize(string arguments)
    {
        var result = new List<string>();
        if (string.IsNullOrEmpty(arguments)) return result;

        var current = new StringBuilder();
        var quote = '\0';
        var hasContent = false;

        for (var i = 0; i < arguments.Length; i++)
        {
            var c = arguments[i];

            if (quote != '\0')
            {
                if (c == quote)
                {
                    quote = '\0';
                    hasContent = true;
                }
                else if (c == '\\' && quote == '"' && i + 1 < arguments.Length &&
                         (arguments[i + 1] == '"' || arguments[i + 1] == '\\'))
                {
                    current.Append(arguments[++i]);
                    hasContent = true;
                }
                else
                {
                    current.Append(c);
                    hasContent = true;
                }

                continue;
            }

            if (c is '"' or '\'')
            {
                quote = c;
                hasContent = true;
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                if (hasContent || current.Length > 0)
                {
                    result.Add(current.ToString());
                    current.Clear();
                    hasContent = false;
                }
                continue;
            }

            if (c == '\\' && i + 1 < arguments.Length && char.IsWhiteSpace(arguments[i + 1]))
            {
                current.Append(arguments[++i]);
                hasContent = true;
                continue;
            }

            current.Append(c);
            hasContent = true;
        }

        if (hasContent || current.Length > 0)
            result.Add(current.ToString());

        return result;
    }

    /// <summary>Echappe un argument pour l'affichage dans un terminal (pas pour l'execution).</summary>
    public static string Quote(string value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";
        if (value.All(c => !char.IsWhiteSpace(c) && c is not '"' and not '\'' and not '`' and not '$'))
            return value;
        return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }

    public static string GetScaffoldBinaryPath()
    {
        var binaryName = OperatingSystem.IsWindows() ? "scaffold.exe" : "scaffold";

        if (!string.IsNullOrEmpty(Environment.ProcessPath) && File.Exists(Environment.ProcessPath))
            return Environment.ProcessPath;

        return FindCommand(binaryName) ?? binaryName;
    }
}
