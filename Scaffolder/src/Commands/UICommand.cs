using System.CommandLine;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Scaffolder.Services;
using Scaffolder.Web;

namespace Scaffolder.Commands;

/// <summary>
/// Interface web locale : toutes les actions du CLI depuis le navigateur.
/// Sécurité : écoute en local uniquement, liste blanche de commandes (aucun shell,
/// aucune commande libre), et rejet des requêtes venant d'une autre origine.
/// </summary>
public class UICommand : Command
{
    private static readonly Regex NamePattern = new(@"^[A-Za-z0-9][A-Za-z0-9._-]{0,99}$", RegexOptions.Compiled);
    private static readonly Regex AnsiPattern = new(@"\x1B\[[0-9;?]*[A-Za-z]", RegexOptions.Compiled);
    private static readonly Regex TemplatePattern = new(@"^[a-z0-9][a-z0-9+._-]{0,99}$", RegexOptions.Compiled);

    private static readonly string[] ConfigKeys =
    [
        ConfigService.Keys.ApiKey, ConfigService.Keys.Provider, ConfigService.Keys.Model,
        ConfigService.Keys.Author, ConfigService.Keys.Company, ConfigService.Keys.License,
        ConfigService.Keys.Framework, ConfigService.Keys.DefaultAuth, ConfigService.Keys.DefaultDb,
        ConfigService.Keys.DefaultArch, ConfigService.Keys.Fav
    ];

    private static readonly string[] Providers = ["openai", "claude", "gemini", "grok", "mistral", "deepseek"];

    private static int _port;

    public UICommand() : base("ui", "Lance l'interface web locale de Scaffolder")
    {
        var portOpt = new Option<int>("--port")
        {
            Description = "Port du serveur web",
            DefaultValueFactory = _ => 8080
        };
        var openOpt = new Option<bool>("--open")
        {
            Description = "Ouvre le navigateur automatiquement"
        };

        Add(portOpt);
        Add(openOpt);

        SetAction((ParseResult pr) => HandleUI(pr.GetValue(portOpt), pr.GetValue(openOpt)));
    }

    private static int HandleUI(int port, bool open)
    {
        port = Math.Clamp(port, 1024, 65535);
        _port = port;

        ConsoleService.ShowLogo();
        Console.WriteLine();
        ConsoleService.Info($"Demarrage de l'interface web sur http://localhost:{port}");
        Console.WriteLine();

        try
        {
            using var listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{port}/");
            listener.Start();

            if (open)
                OpenBrowser($"http://localhost:{port}");

            ConsoleService.Success($"Serveur demarre ! Ouvre http://localhost:{port}");
            Console.WriteLine();
            Console.WriteLine("  Pages : tableau de bord, nouveau projet, stack, ajout de composant,");
            Console.WriteLine("          templates, configuration, assistant IA, statistiques.");
            Console.WriteLine();
            ConsoleService.Info("Appuie sur Ctrl+C pour arreter.");

            while (true)
            {
                var context = listener.GetContext();
                _ = Task.Run(() => HandleRequestAsync(context));
            }
        }
        catch (HttpListenerException ex) when (ex.ErrorCode == 5 || ex.ErrorCode == 32)
        {
            ConsoleService.Error($"Port {port} deja utilise ou permission refusee.");
            ConsoleService.Info($"Essaie un autre port : scaffold ui --port {port + 1}");
            return 1;
        }
        catch (Exception ex)
        {
            ConsoleService.Error($"Erreur : {ex.Message}");
            return 1;
        }
    }

    private static void OpenBrowser(string url)
    {
        var (file, args) = OperatingSystem.IsWindows()
            ? ("cmd", new List<string> { "/c", "start", "", url })
            : OperatingSystem.IsMacOS()
                ? ("open", new List<string> { url })
                : ("xdg-open", new List<string> { url });

        _ = ProcessService.RunResultAsync(file, args, Directory.GetCurrentDirectory());
    }

    private static async Task HandleRequestAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            var path = request.Url?.AbsolutePath ?? "/";
            var isPost = request.HttpMethod == "POST";

            if (!IsLocalHost(request.Headers["Host"]))
            {
                await WriteJsonAsync(response, 403, "{\"error\":\"Hote non autorise\"}");
                return;
            }

            if (isPost && !IsSameOrigin(request.Headers["Origin"]))
            {
                await WriteJsonAsync(response, 403, "{\"error\":\"Origine non autorisee\"}");
                return;
            }

            if (request.HttpMethod == "GET" && path == "/")
            {
                await WriteTextAsync(response, 200, UiPage.Html, "text/html; charset=utf-8");
                return;
            }
            var (status, json) = (isPost, path) switch
            {
                (false, "/api/version") => (200, VersionJson()),
                (false, "/api/templates") => await RunRawJsonAsync(["list", "--json"]),
                (false, "/api/doctor") => await RunRawJsonAsync(["doctor", "--json"]),
                (false, "/api/stats") => await RunRawJsonAsync(["stats", "--json"]),
                (false, "/api/commands") => (200, CommandsCatalogJson()),
                (false, "/api/config") => (200, ConfigJson()),
                (true, "/api/config") => await SetConfigAsync(await ReadBodyAsync(request)),
                (true, "/api/new") => await NewProjectAsync(await ReadBodyAsync(request)),
                (true, "/api/stack") => await StackAsync(await ReadBodyAsync(request)),
                (true, "/api/add") => await AddComponentAsync(await ReadBodyAsync(request)),
                (true, "/api/ai") => await AskAiAsync(await ReadBodyAsync(request)),
                (true, "/api/run-command") => await RunCommandAsync(await ReadBodyAsync(request)),
                _ => (404, "{\"error\":\"Introuvable\"}")
            };
            await WriteJsonAsync(response, status, json);
        }
        catch (JsonException)
        {
            await SafeWriteAsync(response, 400, "{\"error\":\"JSON invalide\"}");
        }
        catch (Exception ex)
        {
            await SafeWriteAsync(response, 500, Fail(ex.Message));
        }
    }

    private static bool IsLocalHost(string? host) =>
        host is not null && (host.StartsWith($"localhost:{_port}", StringComparison.OrdinalIgnoreCase)
                             || host.StartsWith($"127.0.0.1:{_port}", StringComparison.OrdinalIgnoreCase));

    private static bool IsSameOrigin(string? origin) =>
        origin is not null && (origin == $"http://localhost:{_port}" || origin == $"http://127.0.0.1:{_port}");

    private static async Task<string> ReadBodyAsync(HttpListenerRequest request)
    {
        using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
        var buffer = new char[64 * 1024];
        var read = await reader.ReadBlockAsync(buffer, 0, buffer.Length);
        return new string(buffer, 0, read);
    }

    private static string? Str(JsonElement root, string name) =>
        root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()?.Trim() : null;

    private static bool Flag(JsonElement root, string name) =>
        root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.True;

    private static bool ValidName(string? value) => value is not null && NamePattern.IsMatch(value);

    private static bool ValidTemplate(string? value) => value is not null && TemplatePattern.IsMatch(value);

    private static string Fail(string message) => new JsonObject { ["error"] = message }.ToJsonString();

    private static async Task<(int, string)> RunScaffoldAsync(IEnumerable<string> args, string workingDirectory = "")
    {
        var (file, cliArgs) = ProcessService.ScaffoldCommand(args);
        var result = await ProcessService.RunResultAsync(
            file, cliArgs, workingDirectory,
            streamOutput: false, timeout: TimeSpan.FromMinutes(20));

        var body = new JsonObject
        {
            ["exitCode"] = result.ExitCode,
            ["output"] = AnsiPattern.Replace(result.Output, "")
        }.ToJsonString();
        return (result.Success ? 200 : 500, body);
    }

    /// <summary>Renvoie tel quel le JSON produit par le CLI (lecture seule).</summary>
    private static async Task<(int, string)> RunRawJsonAsync(IEnumerable<string> args)
    {
        var (file, cliArgs) = ProcessService.ScaffoldCommand(args);
        var result = await ProcessService.RunResultAsync(
            file, cliArgs, "",
            streamOutput: false, timeout: TimeSpan.FromMinutes(2));

        var output = result.Output.Trim();
        if (output.StartsWith('{') || output.StartsWith('['))
            return (200, output);

        return (500, Fail(string.IsNullOrEmpty(output) ? "Commande échouée" : AnsiPattern.Replace(output, "")));
    }

    private static string VersionJson()
    {
        var providers = new JsonArray(Providers.Select(p => (JsonNode?)JsonValue.Create(p)).ToArray());
        return new JsonObject
        {
            ["version"] = UpdateService.CurrentVersion,
            ["cwd"] = Directory.GetCurrentDirectory(),
            ["providers"] = providers
        }.ToJsonString();
    }

    private static string ConfigJson()
    {
        var values = new JsonObject();
        foreach (var key in ConfigKeys)
        {
            var value = ConfigService.Get(key);
            // La clé API n'est jamais renvoyée en clair au navigateur.
            var shown = key == ConfigService.Keys.ApiKey
                ? (string.IsNullOrEmpty(value) ? "" : "••••" + value[^Math.Min(4, value.Length)..])
                : value ?? "";
            values[key] = shown;
        }
        return values.ToJsonString();
    }

    private static async Task<(int, string)> SetConfigAsync(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var key = Str(root, "key");
        var value = Str(root, "value") ?? "";

        if (key is null || !ConfigKeys.Contains(key))
            return (400, Fail("Clé de configuration non autorisée"));

        if (key == ConfigService.Keys.Provider && !Providers.Contains(value))
            return (400, Fail("Fournisseur inconnu"));

        if (key == ConfigService.Keys.ApiKey && value.Length == 0)
            return (400, Fail("Clé API vide"));

        await Task.Yield();
        ConfigService.Set(key, value);
        return (200, "{\"ok\":true}");
    }

    private static async Task<(int, string)> NewProjectAsync(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var template = Str(root, "template");
        var name = Str(root, "name");
        if (!ValidTemplate(template)) return (400, Fail("Template invalide"));
        if (!ValidName(name)) return (400, Fail("Nom de projet invalide (lettres, chiffres, '.', '_', '-')"));

        var output = Str(root, "output");
        if (string.IsNullOrWhiteSpace(output))
            output = Path.Combine(Directory.GetCurrentDirectory(), name!);

        var args = new List<string>
        {
            "new",
            $"--template={template}",
            $"--name={name}",
            $"--output={output}",
            "--silent"
        };

        if (Str(root, "language") is { Length: > 0 } language) args.Add($"--language={language}");
        if (!Flag(root, "git")) args.Add("--no-git");
        if (Flag(root, "docker")) args.Add("--docker");
        if (Flag(root, "ci")) args.Add("--ci");
        if (Str(root, "auth") is { Length: > 0 } auth) args.Add($"--auth={auth}");
        if (Str(root, "db") is { Length: > 0 } db) args.Add($"--db={db}");
        if (Str(root, "arch") is { Length: > 0 } arch) args.Add($"--arch={arch}");

        return await RunScaffoldAsync(args);
    }

    private static async Task<(int, string)> StackAsync(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var name = Str(root, "name");
        if (!ValidName(name)) return (400, Fail("Nom de projet invalide"));

        var output = Str(root, "output");
        if (string.IsNullOrWhiteSpace(output))
            output = Directory.GetCurrentDirectory();

        var args = new List<string> { "stack", $"--name={name}", $"--output={output}" };
        if (Str(root, "frontend") is { Length: > 0 } fe) args.Add($"--frontend={fe}");
        if (Str(root, "backend") is { Length: > 0 } be) args.Add($"--backend={be}");
        if (Str(root, "db") is { Length: > 0 } db) args.Add($"--db={db}");

        return await RunScaffoldAsync(args);
    }

    private static async Task<(int, string)> AddComponentAsync(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var type = Str(root, "type");
        var name = Str(root, "name");
        var dir = Str(root, "dir");

        if (!ValidTemplate(type)) return (400, Fail("Type de composant invalide"));
        if (!ValidName(name)) return (400, Fail("Nom de composant invalide"));
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
            return (400, Fail("Dossier du projet introuvable"));

        return await RunScaffoldAsync(["add", type!, name!], dir);
    }

    private static async Task<(int, string)> AskAiAsync(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var prompt = Str(doc.RootElement, "prompt");
        if (string.IsNullOrWhiteSpace(prompt) || prompt.Length > 2000)
            return (400, Fail("Question vide ou trop longue"));

        return await RunScaffoldAsync(["ai", "ask", prompt]);
    }

    private static string CommandsCatalogJson()
    {
        var arr = new System.Text.Json.Nodes.JsonArray();
        foreach (var e in CliCatalog.All)
        {
            var paramsArr = new System.Text.Json.Nodes.JsonArray();
            foreach (var p in e.Parameters)
            {
                paramsArr.Add(new System.Text.Json.Nodes.JsonObject
                {
                    ["token"] = p.Token,
                    ["description"] = p.Description,
                    ["isArgument"] = p.IsArgument,
                    ["isFlag"] = p.IsFlag,
                    ["required"] = p.Required,
                    ["multiple"] = p.Multiple
                });
            }
            arr.Add(new System.Text.Json.Nodes.JsonObject
            {
                ["path"] = e.Path,
                ["description"] = e.Description,
                ["parameters"] = paramsArr
            });
        }
        return arr.ToJsonString();
    }

    private static async Task<(int, string)> RunCommandAsync(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var path = Str(root, "path");
        if (string.IsNullOrWhiteSpace(path)) return (400, Fail("Chemin de commande requis"));

        var entry = CliCatalog.All.FirstOrDefault(e => e.Path == path);
        if (entry is null) return (400, Fail("Commande inconnue"));

        var args = entry.Path.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (root.TryGetProperty("values", out var vals) && vals.ValueKind == JsonValueKind.Object)
        {
            foreach (var p in entry.Parameters)
            {
                if (!vals.TryGetProperty(p.Token, out var v)) continue;
                if (p.IsFlag)
                {
                    if (v.ValueKind == JsonValueKind.True) args.Add(p.Token);
                    continue;
                }
                var value = v.ValueKind == JsonValueKind.String ? v.GetString()?.Trim() : null;
                if (string.IsNullOrEmpty(value)) continue;
                args.Add(p.Token);
                args.Add(value);
            }
        }

        return await RunScaffoldAsync(args);
    }

    private static async Task WriteJsonAsync(HttpListenerResponse response, int status, string json) =>
        await WriteTextAsync(response, status, json, "application/json; charset=utf-8");

    private static async Task WriteTextAsync(HttpListenerResponse response, int status, string text, string contentType)
    {
        response.StatusCode = status;
        response.ContentType = contentType;
        response.Headers["X-Content-Type-Options"] = "nosniff";
        response.Headers["Cache-Control"] = "no-store";
        var bytes = Encoding.UTF8.GetBytes(text);
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes);
        response.OutputStream.Close();
    }

    private static async Task SafeWriteAsync(HttpListenerResponse response, int status, string json)
    {
        try { await WriteJsonAsync(response, status, json); }
        catch { }
    }
}
