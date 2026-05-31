using System.Net;
using System.CommandLine;
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

        // Generate HTML
        var html = GenerateHTML();

        // Start HTTP server
        try
        {
            using var listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{port}/");
            listener.Start();

            if (open)
            {
                ProcessService.RunAsync("xdg-open", $"http://localhost:{port}", "/tmp").Wait();
            }

            ConsoleService.Success($"Serveur demarre ! Ouvre http://localhost:{port}");
            Console.WriteLine();
            Console.WriteLine("  Commandes disponibles :");
            Console.WriteLine("    http://localhost:{0}/          — Interface web", port);
            Console.WriteLine("    http://localhost:{0}/api/templates — API JSON", port);
            Console.WriteLine();
            ConsoleService.Info("Appuie sur Ctrl+C pour arreter.");

            while (true)
            {
                var context = listener.GetContext();
                var request = context.Request;
                var response = context.Response;

                var path = request.Url?.AbsolutePath ?? "/";

                if (path == "/api/templates")
                {
                    // Return templates as JSON
                    var templates = GetTemplatesJson();
                    var buffer = System.Text.Encoding.UTF8.GetBytes(templates);
                    response.ContentType = "application/json";
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                }
                else
                {
                    // Return HTML
                    var buffer = System.Text.Encoding.UTF8.GetBytes(html);
                    response.ContentType = "text/html";
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                }

                response.OutputStream.Close();
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
  * { margin: 0; padding: 0; box-sizing: border-box; }
  body {
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
    background: #0d1117; color: #c9d1d9; line-height: 1.6;
    min-height: 100vh; display: flex; flex-direction: column;
  }
  header {
    background: linear-gradient(135deg, #58a6ff, #3fb950);
    padding: 2rem; text-align: center;
  }
  header h1 { font-size: 2rem; color: #fff; margin-bottom: 0.5rem; }
  header p { color: rgba(255,255,255,0.8); }
  main { flex: 1; max-width: 800px; margin: 2rem auto; padding: 0 1rem; width: 100%; }
  .card {
    background: #161b22; border: 1px solid #30363d; border-radius: 8px;
    padding: 1.5rem; margin-bottom: 1rem;
  }
  .card h2 { color: #58a6ff; margin-bottom: 1rem; }
  .card p { margin-bottom: 0.5rem; }
  .btn {
    display: inline-block; padding: 0.6rem 1.2rem; border-radius: 6px;
    text-decoration: none; font-weight: 500; margin: 0.25rem;
    border: 1px solid #30363d; background: #21262d; color: #c9d1d9;
    cursor: pointer; transition: all 0.2s;
  }
  .btn:hover { background: #30363d; border-color: #58a6ff; }
  .btn-primary { background: #238636; border-color: #2ea043; color: #fff; }
  .btn-primary:hover { background: #2ea043; }
  input, select {
    width: 100%; padding: 0.6rem; margin: 0.5rem 0;
    background: #0d1117; border: 1px solid #30363d; border-radius: 6px;
    color: #c9d1d9; font-size: 1rem;
  }
  code {
    background: #0d1117; padding: 0.2rem 0.4rem; border-radius: 4px;
    font-family: 'SF Mono', Monaco, monospace; font-size: 0.9rem;
  }
  .result { background: #0d1117; padding: 1rem; border-radius: 6px; margin-top: 1rem; }
  footer { text-align: center; padding: 2rem; color: #484f58; }
</style>
</head>
<body>
<header>
  <h1> Scaffolder</h1>
  <p>CLI universel pour generer des projets</p>
</header>
<main>
  <div class="card">
    <h2>Nouveau projet</h2>
    <input type="text" id="projectName" placeholder="Nom du projet" value="mon-projet">
    <select id="templateSelect">
      <option value="">Choisir un template...</option>
      <option value="webapi">API REST ASP.NET Core</option>
      <option value="console">Application console .NET</option>
      <option value="blazor">Blazor WebAssembly</option>
      <option value="maui">Application mobile MAUI</option>
      <option value="vite">Vite + React/TypeScript</option>
      <option value="next">Next.js</option>
      <option value="vue">Vue 3</option>
      <option value="nuxt">Nuxt 3</option>
      <option value="svelte">SvelteKit</option>
      <option value="cargo">Projet Rust</option>
      <option value="go">Projet Go</option>
      <option value="python">Projet Python</option>
      <option value="flutter">Flutter</option>
      <option value="laravel">Laravel</option>
      <option value="rails">Ruby on Rails</option>
      <option value="gradle">Gradle (Kotlin/Java)</option>
      <option value="swift">Swift</option>
      <option value="zig">Zig</option>
      <option value="elixir">Elixir</option>
      <option value="haskell">Haskell</option>
      <option value="hello">Hello World</option>
    </select>
    <button class="btn btn-primary" onclick="generateProject()">Generer</button>
    <div id="result" class="result" style="display:none"></div>
  </div>

  <div class="card">
    <h2>Commandes rapides</h2>
    <button class="btn" onclick="showCommand('suggest api rest')">Suggérer un template</button>
    <button class="btn" onclick="showCommand('explain middleware')">Expliquer un concept</button>
    <button class="btn" onclick="showCommand('doctor')">Diagnostic</button>
    <button class="btn" onclick="showCommand('--help')">Aide</button>
  </div>

  <div class="card">
    <h2>Appliquer une commande</h2>
    <input type="text" id="commandInput" placeholder="scaffold ..." value="scaffold suggest api rest">
    <button class="btn btn-primary" onclick="runCommand()">Executer</button>
    <pre id="commandOutput" style="margin-top:1rem;background:#0d1117;padding:1rem;border-radius:6px;display:none"></pre>
  </div>
</main>
<footer>
  <p>Scaffolder v2.0 — <a href="https://github.com/akaletekoffilevis/Scaffolder-CLI" style="color:#58a6ff">GitHub</a></p>
</footer>
<script>
function generateProject() {
  const name = document.getElementById('projectName').value || 'mon-projet';
  const template = document.getElementById('templateSelect').value;
  const result = document.getElementById('result');

  if (!template) {
    result.innerHTML = '<span style="color:#f85149">Veuillez choisir un template.</span>';
    result.style.display = 'block';
    return;
  }

  result.innerHTML = '<span style="color:#3fb950">Commande generee :</span><br><br>' +
    '<code>scaffold new --template=' + template + ' --name=' + name + '</code><br><br>' +
    '<span style="color:#8b949e">Execute cette commande dans ton terminal.</span>';
  result.style.display = 'block';
}

function showCommand(cmd) {
  document.getElementById('commandInput').value = 'scaffold ' + cmd;
  runCommand();
}

function runCommand() {
  const cmd = document.getElementById('commandInput').value;
  const output = document.getElementById('commandOutput');

  output.textContent = '$ ' + cmd + '\n\nPour executer cette commande, copie-la dans ton terminal.\n\n' +
    'Ou utilise directement Scaffolder en CLI :\n' + cmd;
  output.style.display = 'block';
}
</script>
</body>
</html>
""";
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
        };

        return System.Text.Json.JsonSerializer.Serialize(
            new TemplatesResult(templates),
            JsonContext.Default.TemplatesResult);
    }
}
