using System.CommandLine;
using System.Text.Json;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class StoreCommand : Command
{
    public StoreCommand() : base("store", "Genere un site web statique pour le marketplace de templates")
    {
        var outputOpt = new Option<DirectoryInfo?>("--output")
        {
            Description = "Dossier de sortie du site",
            Required = false
        };
        var publishOpt = new Option<bool>("--publish")
        {
            Description = "Publie sur GitHub Pages apres generation"
        };
        Add(outputOpt);
        Add(publishOpt);
        SetAction((ParseResult pr) => Handle(
            pr.GetValue(outputOpt), pr.GetValue(publishOpt)));
    }

    private static int Handle(DirectoryInfo? output, bool publish)
    {
        var registryDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".scaffolder", "registry");
        var outDir = output?.FullName ?? Path.Combine(Directory.GetCurrentDirectory(), "template-store");

        Directory.CreateDirectory(outDir);

        ConsoleService.Info("Generation du template store...");
        Console.WriteLine();

        // Collect all templates
        var templates = new List<Dictionary<string, object>>();
        var registryExists = Directory.Exists(registryDir);

        if (registryExists)
        {
            foreach (var tmplDir in Directory.GetDirectories(registryDir))
            {
                var metaPath = Path.Combine(tmplDir, "metadata.json");
                if (!File.Exists(metaPath)) continue;
                try
                {
                    var meta = JsonSerializer.Deserialize<Dictionary<string, object>>(
                        File.ReadAllText(metaPath), JsonContext.Default.DictionaryStringObject);
                    if (meta != null)
                    {
                        meta["dir"] = tmplDir;
                        templates.Add(meta);
                    }
                }
                catch { }
            }
        }

        // Built-in templates
        var builtIn = new[]
        {
            new { name = "hello", desc = "Hello World minimaliste", tags = "debutant" },
            new { name = "dotnet", desc = "Application .NET (console, webapi, blazor, maui)", tags = "dotnet" },
            new { name = "npm", desc = "Application Node.js (vite, next, react, vue)", tags = "node" },
            new { name = "cargo", desc = "Projet Rust", tags = "rust" },
            new { name = "go", desc = "Projet Go", tags = "golang" },
            new { name = "python", desc = "Projet Python", tags = "python" },
            new { name = "flutter", desc = "Application Flutter", tags = "dart" },
            new { name = "laravel", desc = "Laravel", tags = "php" },
            new { name = "rails", desc = "Ruby on Rails", tags = "ruby" },
        };

        // Generate HTML
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("""
<!DOCTYPE html>
<html lang="fr">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>Scaffolder Template Store</title>
<style>
* { margin:0; padding:0; box-sizing:border-box; }
body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; background:#0d1117; color:#c9d1d9; }
header { text-align:center; padding:60px 24px 40px; background:radial-gradient(ellipse at 50% 0%, rgba(88,166,255,.08) 0%, transparent 60%); }
header h1 { font-size:2.5rem; background:linear-gradient(135deg,#58a6ff,#d2a8ff); -webkit-background-clip:text; -webkit-text-fill-color:transparent; background-clip:text; }
header p { color:#8b949e; margin-top:8px; }
.search-box { max-width:600px; margin:24px auto 0; }
.search-box input { width:100%; padding:12px 16px; background:#161b22; border:1px solid #30363d; border-radius:8px; color:#c9d1d9; font-size:1rem; outline:none; }
.search-box input:focus { border-color:#58a6ff; }
.grid { max-width:1100px; margin:0 auto; padding:40px 24px; display:grid; grid-template-columns:repeat(auto-fill,minmax(300px,1fr)); gap:16px; }
.card { background:#161b22; border:1px solid #30363d; border-radius:8px; padding:20px; transition:border-color .2s; }
.card:hover { border-color:#58a6ff; }
.card h3 { font-size:1.1rem; margin-bottom:6px; }
.card h3 code { color:#58a6ff; font-family:'SF Mono',monospace; }
.card p { color:#8b949e; font-size:.9rem; margin-bottom:8px; }
.card .tags { display:flex; gap:4px; flex-wrap:wrap; }
.card .tags span { background:rgba(88,166,255,.12); color:#58a6ff; padding:2px 10px; border-radius:12px; font-size:.75rem; }
.card .meta { font-size:.8rem; color:#8b949e; margin-top:8px; }
.card .dl { display:inline-block; margin-top:8px; padding:6px 16px; background:#238636; color:#fff; border-radius:6px; font-size:.85rem; text-decoration:none; }
.card .dl:hover { background:#2ea043; }
footer { text-align:center; padding:40px; color:#8b949e; font-size:.85rem; border-top:1px solid #30363d; }
.hidden { display:none; }
</style>
</head>
<body>
<header>
  <h1>Template Store</h1>
  <p>Parcourir les templates Scaffolder</p>
  <div class="search-box">
    <input type="text" id="search" placeholder="Rechercher un template..." oninput="filter()">
  </div>
</header>
<div class="grid" id="grid">
""");

        // Built-in templates
        foreach (var t in builtIn)
        {
            sb.AppendLine($"""
<div class="card" data-name="{t.name}" data-tags="{t.tags}">
  <h3><code>{t.name}</code></h3>
  <p>{t.desc}</p>
  <div class="tags"><span>{t.tags}</span></div>
  <div class="meta">Integre a Scaffolder</div>
  <a class="dl" href="scaffold+new+--template={t.name}+--name=mon-projet" onclick="event.preventDefault();navigator.clipboard.writeText('scaffold new --template={t.name} --name=mon-projet');alert('Commande copiee !')">Copier la commande</a>
</div>
""");
        }

        // Registry templates
        foreach (var t in templates)
        {
            var name = t.GetValueOrDefault("name", "?")?.ToString() ?? "?";
            var desc = t.GetValueOrDefault("description", "")?.ToString() ?? "";
            var tags = "";
            if (t.TryGetValue("tags", out var rawTags) && rawTags is JsonElement je && je.ValueKind == JsonValueKind.Array)
            {
                tags = string.Join(" ", je.EnumerateArray().Select(x => x.GetString()));
            }
            var files = t.GetValueOrDefault("files", 0)?.ToString() ?? "?";
            var downloads = t.GetValueOrDefault("downloads", 0)?.ToString() ?? "0";

            sb.AppendLine($"""
<div class="card" data-name="{name}" data-tags="{tags}">
  <h3><code>{name}</code></h3>
  <p>{desc}</p>
  <div class="tags">{string.Join("", (tags + " custom").Split(' ').Where(x => !string.IsNullOrEmpty(x)).Select(x => $"<span>{x}</span>"))}</div>
  <div class="meta">{files} fichiers &middot; {downloads} telechargements</div>
  <a class="dl" href="scaffold+new+--template={name}+--name=mon-projet" onclick="event.preventDefault();navigator.clipboard.writeText('scaffold new --template={name} --name=mon-projet');alert('Commande copiee !')">Copier la commande</a>
</div>
""");
        }

        sb.AppendLine("""
</div>
<script>
function filter() {
  const q = document.getElementById('search').value.toLowerCase();
  document.querySelectorAll('.card').forEach(c => {
    const text = (c.dataset.name + ' ' + c.dataset.tags + ' ' + c.textContent).toLowerCase();
    c.classList.toggle('hidden', !text.includes(q));
  });
}
</script>
<footer>Scaffolder Template Store &middot; <a href="https://github.com/akaletekoffilevis/Scaffolder-CLI">GitHub</a></footer>
</body>
</html>
""");

        var html = sb.ToString();
        File.WriteAllText(Path.Combine(outDir, "index.html"), html);

        ConsoleService.Success($"Template store genere : {outDir}/index.html");
        ConsoleService.Info($"  {builtIn.Length} templates integres");
        ConsoleService.Info($"  {templates.Count} templates personnalises");
        Console.WriteLine();

        if (registryExists)
        {
            // Copy template files for download
            var downDir = Path.Combine(outDir, "templates");
            foreach (var t in templates)
            {
                if (t.TryGetValue("dir", out var dirObj) && dirObj is string dir && Directory.Exists(dir))
                {
                    var target = Path.Combine(downDir, Path.GetFileName(dir));
                    CopyDirectory(dir, target);
                }
            }
            if (Directory.Exists(downDir))
                ConsoleService.Info($"  Templates copies dans {downDir}/");
        }

        if (publish)
        {
            ConsoleService.Info("Publication sur GitHub Pages...");
            var cwd = Directory.GetCurrentDirectory();
            var pagesResult = ProcessService.RunAsync("git", "add . && git commit -m \"Update template store\" && git push", outDir)
                .GetAwaiter().GetResult();
            if (pagesResult.ExitCode == 0)
                ConsoleService.Success("Template store publie !");
            else
                ConsoleService.Warning("Echec de la publication automatique.");
        }

        return 0;
    }

    private static void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(source, file);
            var target = Path.Combine(dest, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }
}
