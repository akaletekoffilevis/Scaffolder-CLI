using System.Net;
using System.CommandLine;
using System.Text;
using Scaffolder;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class UICommand : Command
{
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

        SetAction((ParseResult pr) => HandleUI(
            pr.GetValue(portOpt), pr.GetValue(openOpt)));
    }

    private static int HandleUI(int port, bool open)
    {
        port = Math.Clamp(port, 1024, 65535);

        ConsoleService.ShowLogo();
        Console.WriteLine();
        ConsoleService.Info($"Demarrage de l'interface web sur http://localhost:{port}");
        Console.WriteLine();

        var html = GenerateHTML();

        try
        {
            using var listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{port}/");
            listener.Start();

            if (open)
            {
                var openCmd = OperatingSystem.IsWindows() ? "start" :
                              OperatingSystem.IsMacOS() ? "open" : "xdg-open";
                ProcessService.RunAsync(openCmd, $"http://localhost:{port}", "/tmp").Wait();
            }

            ConsoleService.Success($"Serveur demarre ! Ouvre http://localhost:{port}");
            Console.WriteLine();
            Console.WriteLine("  Endpoints disponibles :");
            Console.WriteLine("    GET  /                  — Interface web");
            Console.WriteLine("    GET  /api/templates     — Liste des templates (JSON)");
            Console.WriteLine("    GET  /api/config        — Configuration actuelle (JSON)");
            Console.WriteLine("    GET  /api/version       — Version Scaffolder (JSON)");
            Console.WriteLine("    POST /api/generate      — Generer un projet (JSON body)");
            Console.WriteLine();
            ConsoleService.Info("Appuie sur Ctrl+C pour arreter.");

            while (true)
            {
                var context = listener.GetContext();
                _ = Task.Run(() => HandleRequest(context, html));
            }
        }
        catch (HttpListenerException ex) when (ex.ErrorCode == 5)
        {
            ConsoleService.Error($"Port {port} deja utilise ou permission refusee.");
            ConsoleService.Info("Essaie un autre port : scaffold ui --port 3000");
            return 1;
        }
        catch (Exception ex)
        {
            ConsoleService.Error($"Erreur : {ex.Message}");
            ConsoleService.Info("Assure-toi d'avoir les droits ou utilise un port > 1024.");
            return 1;
        }
    }

    private static void HandleRequest(HttpListenerContext context, string html)
    {
        try
        {
            var request = context.Request;
            var response = context.Response;

            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

            if (request.HttpMethod == "OPTIONS")
            {
                response.StatusCode = 204;
                response.OutputStream.Close();
                return;
            }

            var path = request.Url?.AbsolutePath ?? "/";

            switch (path)
            {
                case "/":
                    WriteResponse(response, html, "text/html");
                    break;
                case "/api/templates":
                    WriteResponse(response, GetTemplatesJson(), "application/json");
                    break;
                case "/api/config":
                    WriteResponse(response, GetConfigJson(), "application/json");
                    break;
                case "/api/version":
                    WriteResponse(response, $"{{\"version\":\"{UpdateService.CurrentVersion}\"}}", "application/json");
                    break;
                case "/api/generate" when request.HttpMethod == "POST":
                    HandleGenerateRequest(request, response);
                    break;
                case "/api/history":
                    WriteResponse(response, GetHistoryJson(), "application/json");
                    break;
                case "/api/favorites":
                    WriteResponse(response, GetFavoritesJson(), "application/json");
                    break;
                case "/api/plugins":
                    WriteResponse(response, GetPluginsJson(), "application/json");
                    break;
                case "/api/health":
                    WriteResponse(response, $"{{\"status\":\"healthy\",\"version\":\"{UpdateService.CurrentVersion}\",\"uptime\":\"{Environment.TickCount / 1000}s\"}}", "application/json");
                    break;
                case "/api/config/set" when request.HttpMethod == "POST":
                    HandleConfigSetRequest(request, response);
                    break;
                case "/api/template/search" when request.HttpMethod == "POST":
                    HandleTemplateSearchRequest(request, response);
                    break;
                case "/api/run" when request.HttpMethod == "POST":
                    HandleRunCommand(request, response);
                    break;
                default:
                    response.StatusCode = 404;
                    WriteResponse(response, "{\"error\":\"Not found\"}", "application/json");
                    break;
            }
        }
        catch (Exception ex)
        {
            try
            {
                context.Response.StatusCode = 500;
                WriteResponse(context.Response, $"{{\"error\":\"{ex.Message}\"}}", "application/json");
            }
            catch { }
        }
    }

    private static void HandleGenerateRequest(HttpListenerRequest request, HttpListenerResponse response)
    {
        try
        {
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
            var body = reader.ReadToEnd();
            using var doc = System.Text.Json.JsonDocument.Parse(body);
            var root = doc.RootElement;

            var name = root.TryGetProperty("name", out var n) ? n.GetString() ?? "mon-projet" : "mon-projet";
            var template = root.TryGetProperty("template", out var t) ? t.GetString() ?? "hello" : "hello";

            var scaffoldBin = ProcessService.GetScaffoldBinaryPath();
            var result = ProcessService.RunAsync(scaffoldBin,
                $"new --template={template} --name={name} --no-git --silent",
                Directory.GetCurrentDirectory()).GetAwaiter().GetResult();

            var json = $"{{\"success\":{result.ExitCode == 0},\"exitCode\":{result.ExitCode},\"template\":\"{template}\",\"name\":\"{name}\"}}";
            response.StatusCode = result.ExitCode == 0 ? 200 : 500;
            WriteResponse(response, json, "application/json");
        }
        catch (Exception ex)
        {
            response.StatusCode = 400;
            WriteResponse(response, $"{{\"error\":\"{ex.Message}\"}}", "application/json");
        }
    }

    private static void HandleConfigSetRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                var body = reader.ReadToEnd();
                using var doc = System.Text.Json.JsonDocument.Parse(body);
                var root = doc.RootElement;

                var key = root.TryGetProperty("key", out var k) ? k.GetString() : null;
                var value = root.TryGetProperty("value", out var v) ? v.GetString() : null;

                if (string.IsNullOrWhiteSpace(key))
                {
                    response.StatusCode = 400;
                    WriteResponse(response, "{\"error\":\"Key required\"}", "application/json");
                    return;
                }

                ConfigService.Set(key, value ?? "");
                WriteResponse(response, "{\"success\":true}", "application/json");
            }
            catch (Exception ex)
            {
                response.StatusCode = 400;
                WriteResponse(response, $"{{\"error\":\"{ex.Message}\"}}", "application/json");
            }
        }

        private static void HandleTemplateSearchRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                var body = reader.ReadToEnd();
                using var doc = System.Text.Json.JsonDocument.Parse(body);
                var root = doc.RootElement;

                var query = root.TryGetProperty("query", out var q) ? q.GetString() ?? "" : "";
                var results = TemplateRegistry.Search(query);

                var json = System.Text.Json.JsonSerializer.Serialize(
                    new TemplatesResult(results.Select(r => new TemplateInfo(r.Name, r.Description)).ToArray()),
                    JsonContext.Default.TemplatesResult);
                WriteResponse(response, json, "application/json");
            }
            catch (Exception ex)
            {
                response.StatusCode = 400;
                WriteResponse(response, $"{{\"error\":\"{ex.Message}\"}}", "application/json");
            }
        }

        private static void HandleRunCommand(HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                var body = reader.ReadToEnd();
                using var doc = System.Text.Json.JsonDocument.Parse(body);
                var root = doc.RootElement;

                var command = root.TryGetProperty("command", out var c) ? c.GetString() : null;
                var args = root.TryGetProperty("args", out var a) ? a.GetString() : "";
                var cwd = root.TryGetProperty("cwd", out var w) ? w.GetString() : Directory.GetCurrentDirectory();

                if (string.IsNullOrWhiteSpace(command))
                {
                    response.StatusCode = 400;
                    WriteResponse(response, "{\"error\":\"Command required\"}", "application/json");
                    return;
                }

                var result = ProcessService.RunAsync(command, args ?? "", cwd ?? Directory.GetCurrentDirectory(), streamOutput: false).GetAwaiter().GetResult();
                var json = $"{{\"exitCode\":{result.ExitCode},\"output\":\"{result.Output.Replace("\"", "\\\"").Replace("\n", "\\n")}\"}}";
                response.StatusCode = result.ExitCode == 0 ? 200 : 500;
                WriteResponse(response, json, "application/json");
            }
            catch (Exception ex)
            {
                response.StatusCode = 400;
                WriteResponse(response, $"{{\"error\":\"{ex.Message}\"}}", "application/json");
            }
        }

        private static string GetHistoryJson()
        {
            var history = VersionManager.GetHistory(50);
            var sb = new StringBuilder("[");
            for (int i = 0; i < history.Count; i++)
            {
                var v = history[i];
                if (i > 0) sb.Append(',');
                sb.Append("{\"version\":\"").Append(v.Version).Append("\"");
                sb.Append(",\"date\":\"").Append(v.Date).Append("\"");
                sb.Append(",\"notes\":\"").Append(v.Notes.Replace("\"", "\\\"")).Append("\"");
                sb.Append(",\"commitSha\":\"").Append(v.CommitSha).Append("\"}");
            }
            sb.Append(']');
            return sb.ToString();
        }

        private static string GetFavoritesJson()
        {
            var favs = DatabaseService.GetFavorites();
            return System.Text.Json.JsonSerializer.Serialize(favs, JsonContext.Default.DictionaryStringString);
        }

        private static string GetPluginsJson()
        {
            var plugins = DatabaseService.GetPlugins();
            return System.Text.Json.JsonSerializer.Serialize(plugins, JsonContext.Default.DictionaryStringString);
        }

    private static void WriteResponse(HttpListenerResponse response, string content, string contentType)
    {
        var buffer = Encoding.UTF8.GetBytes(content);
        response.ContentType = contentType;
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.OutputStream.Close();
    }

    private static string GetConfigJson()
    {
        if (!ConfigService.ReadConfig(out var dict))
            return "{}";

        var sb = new System.Text.StringBuilder("{");
        var first = true;
        foreach (var (k, v) in dict)
        {
            if (!first) sb.Append(',');
            sb.Append($"\"{k}\":\"{v}\"");
            first = false;
        }
        sb.Append('}');
        return sb.ToString();
    }

    private static string GetTemplatesJson()
    {
        var templates = new[]
        {
            new TemplateInfo("hello", "Application Hello World minimaliste"),
            new TemplateInfo("console", "Application console .NET"),
            new TemplateInfo("webapi", "API REST ASP.NET Core"),
            new TemplateInfo("blazor", "Blazor WebAssembly"),
            new TemplateInfo("maui", "Application mobile MAUI"),
            new TemplateInfo("vite", "Vite + React/TypeScript"),
            new TemplateInfo("next", "Next.js"),
            new TemplateInfo("vue", "Vue 3"),
            new TemplateInfo("nuxt", "Nuxt 3"),
            new TemplateInfo("svelte", "SvelteKit"),
            new TemplateInfo("cargo", "Projet Rust"),
            new TemplateInfo("go", "Projet Go"),
            new TemplateInfo("python", "Projet Python"),
            new TemplateInfo("flutter", "Flutter"),
            new TemplateInfo("laravel", "Laravel"),
            new TemplateInfo("symfony", "Symfony"),
            new TemplateInfo("rails", "Ruby on Rails"),
            new TemplateInfo("gradle", "Gradle (Kotlin/Java)"),
            new TemplateInfo("swift", "Swift"),
            new TemplateInfo("zig", "Zig"),
            new TemplateInfo("elixir", "Elixir"),
            new TemplateInfo("haskell", "Haskell"),
            new TemplateInfo("scala", "Scala"),
            new TemplateInfo("crystal", "Crystal"),
        };

        return System.Text.Json.JsonSerializer.Serialize(
            new TemplatesResult(templates),
            JsonContext.Default.TemplatesResult);
    }

    private static string GenerateHTML()
    {
        return """
<!DOCTYPE html>
<html lang="fr">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>Scaffolder Web UI</title>
<style>
  :root {
    --bg: #0d1117; --surface: #161b22; --border: #30363d;
    --text: #e6edf3; --text-dim: #8b949e; --accent: #58a6ff;
    --green: #3fb950; --red: #f85149; --yellow: #d29922;
    --radius: 12px; --transition: 0.2s ease;
  }
  * { margin: 0; padding: 0; box-sizing: border-box; }
  body {
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
    background: var(--bg); color: var(--text); line-height: 1.6;
    min-height: 100vh; display: flex; flex-direction: column;
  }
  .hero {
    background: linear-gradient(135deg, #1a1f36 0%, #0d1117 50%, #161b22 100%);
    border-bottom: 1px solid var(--border);
    padding: 3rem 2rem; text-align: center; position: relative; overflow: hidden;
  }
  .hero::before {
    content: ''; position: absolute; top: -50%; left: -50%; width: 200%; height: 200%;
    background: radial-gradient(circle at 30% 50%, rgba(88,166,255,0.08) 0%, transparent 50%),
                radial-gradient(circle at 70% 50%, rgba(63,185,80,0.06) 0%, transparent 50%);
  }
  .hero h1 {
    font-size: 2.5rem; font-weight: 700; position: relative;
    background: linear-gradient(135deg, var(--accent), var(--green));
    -webkit-background-clip: text; -webkit-text-fill-color: transparent;
    background-clip: text;
  }
  .hero p { color: var(--text-dim); font-size: 1.1rem; margin-top: 0.5rem; position: relative; }
  .hero .version {
    display: inline-block; margin-top: 1rem; padding: 0.25rem 0.75rem;
    background: rgba(88,166,255,0.1); border: 1px solid rgba(88,166,255,0.2);
    border-radius: 20px; font-size: 0.85rem; color: var(--accent); position: relative;
  }
  main { flex: 1; max-width: 900px; margin: 2rem auto; padding: 0 1.5rem; width: 100%; }
  .grid { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; margin-bottom: 1rem; }
  @media (max-width: 700px) { .grid { grid-template-columns: 1fr; } }
  .card {
    background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius);
    padding: 1.5rem; transition: border-color var(--transition);
  }
  .card:hover { border-color: rgba(88,166,255,0.3); }
  .card h2 { color: var(--accent); font-size: 1rem; margin-bottom: 1rem; display: flex; align-items: center; gap: 0.5rem; }
  .card h2 .icon { font-size: 1.2rem; }
  .form-group { margin-bottom: 0.75rem; }
  .form-group label { display: block; font-size: 0.85rem; color: var(--text-dim); margin-bottom: 0.25rem; }
  input, select {
    width: 100%; padding: 0.65rem 0.8rem; background: var(--bg); border: 1px solid var(--border);
    border-radius: 8px; color: var(--text); font-size: 0.95rem; transition: border-color var(--transition);
  }
  input:focus, select:focus { outline: none; border-color: var(--accent); box-shadow: 0 0 0 3px rgba(88,166,255,0.1); }
  select { cursor: pointer; }
  .btn {
    display: inline-flex; align-items: center; gap: 0.5rem; padding: 0.6rem 1.2rem;
    border-radius: 8px; font-weight: 500; font-size: 0.95rem; cursor: pointer;
    border: 1px solid var(--border); background: var(--surface); color: var(--text);
    transition: all var(--transition);
  }
  .btn:hover { background: var(--border); }
  .btn-primary { background: var(--green); border-color: #2ea043; color: #fff; }
  .btn-primary:hover { background: #2ea043; }
  .btn-accent { background: rgba(88,166,255,0.1); border-color: rgba(88,166,255,0.3); color: var(--accent); }
  .btn-accent:hover { background: rgba(88,166,255,0.2); }
  .btn-sm { padding: 0.4rem 0.8rem; font-size: 0.85rem; }
  .result {
    background: var(--bg); border: 1px solid var(--border); border-radius: 8px;
    padding: 1rem; margin-top: 1rem; font-family: 'SF Mono', Monaco, monospace; font-size: 0.9rem;
    white-space: pre-wrap; max-height: 300px; overflow-y: auto;
  }
  .result.success { border-color: rgba(63,185,80,0.3); }
  .result.error { border-color: rgba(248,81,73,0.3); }
  .quick-actions { display: flex; flex-wrap: wrap; gap: 0.5rem; }
  .tag {
    display: inline-block; padding: 0.3rem 0.7rem; background: rgba(88,166,255,0.1);
    border: 1px solid rgba(88,166,255,0.2); border-radius: 20px; font-size: 0.8rem;
    color: var(--accent); cursor: pointer; transition: all var(--transition);
  }
  .tag:hover { background: rgba(88,166,255,0.2); }
  .terminal {
    background: #000; border: 1px solid var(--border); border-radius: 8px;
    padding: 1rem; margin-top: 0.5rem; font-family: 'SF Mono', Monaco, monospace;
    font-size: 0.85rem; color: var(--green); max-height: 200px; overflow-y: auto;
  }
  .terminal .prompt { color: var(--accent); }
  .terminal .output { color: var(--text-dim); }
  footer {
    text-align: center; padding: 2rem; color: var(--text-dim); font-size: 0.85rem;
    border-top: 1px solid var(--border);
  }
  footer a { color: var(--accent); text-decoration: none; }
  footer a:hover { text-decoration: underline; }
  .spinner { display: inline-block; width: 16px; height: 16px; border: 2px solid var(--border);
    border-top-color: var(--accent); border-radius: 50%; animation: spin 0.6s linear infinite; }
  @keyframes spin { to { transform: rotate(360deg); } }
  .hidden { display: none; }
</style>
</head>
<body>
<div class="hero">
  <h1>Scaffolder</h1>
  <p>CLI universel pour generer des projets dans tous les langages</p>
  <span class="version" id="version">v...</span>
</div>
<main>
  <div class="grid">
    <div class="card">
      <h2><span class="icon">&#9889;</span> Nouveau projet</h2>
      <div class="form-group">
        <label>Nom du projet</label>
        <input type="text" id="projectName" placeholder="mon-app" value="mon-app">
      </div>
      <div class="form-group">
        <label>Template</label>
        <select id="templateSelect">
          <option value="">Choisir un template...</option>
        </select>
      </div>
      <div class="form-group">
        <label>Options</label>
        <div style="display:flex;gap:0.5rem">
          <label style="display:flex;align-items:center;gap:0.3rem;font-size:0.85rem;cursor:pointer">
            <input type="checkbox" id="noGit" checked> Pas de Git
          </label>
        </div>
      </div>
      <button class="btn btn-primary" onclick="generateProject()" id="generateBtn">
        &#9654; Generer le projet
      </button>
      <div id="result" class="result hidden"></div>
    </div>

    <div class="card">
      <h2><span class="icon">&#128269;</span> Explorer</h2>
      <div class="form-group">
        <label>Concept a expliquer</label>
        <input type="text" id="conceptInput" placeholder="middleware, MVC, REST...">
      </div>
      <button class="btn btn-accent btn-sm" onclick="explainConcept()">Expliquer</button>
      <div id="explainResult" class="result hidden" style="margin-top:0.75rem"></div>

      <div style="margin-top:1.5rem">
        <label style="font-size:0.85rem;color:var(--text-dim)">Templates populaires</label>
        <div class="quick-actions" style="margin-top:0.5rem">
          <span class="tag" onclick="selectTemplate('webapi')">API REST</span>
          <span class="tag" onclick="selectTemplate('react')">React</span>
          <span class="tag" onclick="selectTemplate('vue')">Vue 3</span>
          <span class="tag" onclick="selectTemplate('next')">Next.js</span>
          <span class="tag" onclick="selectTemplate('cargo')">Rust</span>
          <span class="tag" onclick="selectTemplate('go')">Go</span>
          <span class="tag" onclick="selectTemplate('python')">Python</span>
          <span class="tag" onclick="selectTemplate('flutter')">Flutter</span>
        </div>
      </div>
    </div>
  </div>

  <div class="card">
    <h2><span class="icon">&#128187;</span> Terminal</h2>
    <div style="display:flex;gap:0.5rem">
      <input type="text" id="commandInput" placeholder="scaffold --help" value="scaffold --help"
        style="flex:1" onkeydown="if(event.key==='Enter')runCommand()">
      <button class="btn" onclick="runCommand()">Executer</button>
    </div>
    <div class="terminal" id="terminal">
      <span class="prompt">$</span> <span class="output">En attente de commande...</span>
    </div>
  </div>

  <div class="card">
    <h2><span class="icon">&#128202;</span> Configuration</h2>
    <div id="configInfo" style="font-family:monospace;font-size:0.85rem;color:var(--text-dim)">
      Chargement...
    </div>
  </div>
</main>
<footer>
  <p>Scaffolder v<span id="footerVersion">...</span> &mdash; <a href="https://github.com/akaletekoffilevis/Scaffolder-CLI">GitHub</a></p>
</footer>
<script>
async function init() {
  try {
    const res = await fetch('/api/version');
    const data = await res.json();
    document.getElementById('version').textContent = 'v' + data.version;
    document.getElementById('footerVersion').textContent = data.version;
  } catch(e) {}

  try {
    const res = await fetch('/api/templates');
    const data = await res.json();
    const sel = document.getElementById('templateSelect');
    data.templates.forEach(t => {
      const opt = document.createElement('option');
      opt.value = t.name;
      opt.textContent = t.name + ' - ' + t.description;
      sel.appendChild(opt);
    });
  } catch(e) {}

  try {
    const res = await fetch('/api/config');
    const data = await res.json();
    const el = document.getElementById('configInfo');
    if (Object.keys(data).length === 0) {
      el.textContent = 'Aucune configuration. Lance scaffold config init';
    } else {
      el.innerHTML = Object.entries(data).map(([k,v]) =>
        '<div style="display:flex;justify-content:space-between;padding:0.2rem 0;border-bottom:1px solid var(--border)">' +
        '<span style="color:var(--accent)">' + k + '</span><span>' + v + '</span></div>'
      ).join('');
    }
  } catch(e) {}
}

function selectTemplate(name) {
  document.getElementById('templateSelect').value = name;
  document.getElementById('projectName').focus();
}

async function generateProject() {
  const name = document.getElementById('projectName').value || 'mon-app';
  const template = document.getElementById('templateSelect').value;
  const result = document.getElementById('result');
  const btn = document.getElementById('generateBtn');

  if (!template) {
    result.className = 'result error';
    result.textContent = 'Veuillez choisir un template.';
    result.classList.remove('hidden');
    return;
  }

  btn.innerHTML = '<span class="spinner"></span> Generation...';
  btn.disabled = true;

  try {
    const res = await fetch('/api/generate', {
      method: 'POST',
      headers: {'Content-Type': 'application/json'},
      body: JSON.stringify({name, template, noGit: document.getElementById('noGit').checked})
    });
    const data = await res.json();

    if (data.success) {
      result.className = 'result success';
      result.textContent = 'Projet "' + name + "' genere avec succes !\\n\\n" +
        'Commande : scaffold new --template=' + template + ' --name=' + name + '\\n\\n' +
        'Pour commencer :\\n  cd ' + name;
    } else {
      result.className = 'result error';
      result.textContent = 'Echec de la generation (code ' + data.exitCode + ')';
    }
    result.classList.remove('hidden');
  } catch(e) {
    result.className = 'result error';
    result.textContent = 'Erreur : ' + e.message;
    result.classList.remove('hidden');
  }

  btn.innerHTML = '&#9654; Generer le projet';
  btn.disabled = false;
}

function runCommand() {
  const cmd = document.getElementById('commandInput').value;
  const terminal = document.getElementById('terminal');
  terminal.innerHTML = '<span class="prompt">$</span> <span class="output">' + cmd + '</span><br><br>' +
    '<span class="output" style="color:var(--yellow)">Commande recue. Execute-la dans ton terminal :</span><br>' +
    '<span class="output">$ ' + cmd + '</span>';
}

async function explainConcept() {
  const concept = document.getElementById('conceptInput').value;
  const result = document.getElementById('explainResult');

  if (!concept) { result.classList.add('hidden'); return; }

  result.className = 'result';
  result.textContent = 'Chargement...';
  result.classList.remove('hidden');

  try {
    const res = await fetch('/api/config');
    result.className = 'result success';
    result.textContent = 'Concept : ' + concept + '\\n\\nUtilise scaffold explain ' + concept + ' dans ton terminal pour une explication detaillee.';
  } catch(e) {
    result.className = 'result error';
    result.textContent = 'Erreur : ' + e.message;
  }
}

init();
</script>
</body>
</html>
""";
    }
}
