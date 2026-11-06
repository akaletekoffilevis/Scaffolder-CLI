using System.CommandLine;
using System.Text;
using Scaffolder.Services;

namespace Scaffolder.Commands;

/// <summary>
/// Genere un composant dans le projet courant. Le type de composant et le code produit
/// dependent du langage detecte (.NET, Node, Python, Go, Rust).
/// </summary>
public class AddCommand : Command
{
    private static readonly string[] DotnetTypes =
        ["controller", "entity", "repository", "service", "page", "migration", "middleware", "filter"];

    private static readonly string[] NodeTypes =
        ["component", "hook", "service", "page", "route", "test"];

    private static readonly string[] PythonTypes =
        ["module", "class", "route", "service", "test"];

    private static readonly string[] GoTypes =
        ["handler", "model", "service", "test"];

    private static readonly string[] RustTypes =
        ["module", "struct", "handler", "test"];

    public AddCommand() : base("add", "Genere un composant individuel dans le projet courant")
    {
        var typeArg = new Argument<string>("type")
        {
            Description = "Type de composant (voir --list pour la liste selon le projet)"
        };
        var nameArg = new Argument<string>("name")
        {
            Description = "Nom du composant"
        };
        var outputOpt = new Option<DirectoryInfo?>("--output")
        {
            Description = "Dossier cible (defaut: detection auto)"
        };
        var dryRunOpt = new Option<bool>("--dry-run")
        {
            Description = "Previsualisation sans generer"
        };
        var listOpt = new Option<bool>("--list", ["-l"])
        {
            Description = "Liste les types de composants supportes pour ce projet"
        };

        Add(typeArg);
        Add(nameArg);
        Add(outputOpt);
        Add(dryRunOpt);
        Add(listOpt);

        SetAction((ParseResult pr) => HandleAdd(
            pr.GetValue(typeArg)!, pr.GetValue(nameArg)!,
            pr.GetValue(outputOpt), pr.GetValue(dryRunOpt), pr.GetValue(listOpt)));
    }

    private static int HandleAdd(string type, string name, DirectoryInfo? output, bool dryRun, bool list)
    {
        var cwd = output?.FullName ?? Directory.GetCurrentDirectory();
        var project = DetectProject(cwd);

        if (list)
            return ShowTypes(project);

        type = (type ?? "").ToLowerInvariant().Trim();
        name = (name ?? "").Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            ConsoleService.Error("Le nom du composant est requis.");
            return 1;
        }

        if (!IsValidName(name))
        {
            ConsoleService.Error($"Nom invalide : '{name}'. Utilise lettres, chiffres, '-' ou '_'.");
            return 1;
        }

        if (project.Type == "unknown")
        {
            ConsoleService.Warning("Aucun projet detecte dans le dossier courant.");
            ConsoleService.Info("Place-toi dans un projet existant ou utilise --output.");
            return 1;
        }

        var allowed = TypesFor(project);
        if (!allowed.Contains(type))
        {
            ConsoleService.Error($"Type '{type}' non supporte pour {project.Description}.");
            ConsoleService.Info($"Types disponibles : {string.Join(", ", allowed)}");
            return 1;
        }

        if (dryRun)
        {
            ShowDryRun(type, name, cwd, project);
            return 0;
        }

        ConsoleService.Info($"  Ajout de {type} '{name}' au projet {project.Description}...");

        var result = project.Language switch
        {
            "csharp" => GenerateDotnet(type, cwd, name, project),
            "typescript" or "javascript" => GenerateNode(type, cwd, name, project),
            "python" => GeneratePython(type, cwd, name, project),
            "go" => GenerateGo(type, cwd, name, project),
            "rust" => GenerateRust(type, cwd, name, project),
            _ => (Success: false, Message: $"Langage '{project.Language}' non supporte")
        };

        if (result.Success)
        {
            ConsoleService.Success($"{type} '{name}' genere avec succes !");
            return 0;
        }

        ConsoleService.Error($"Echec : {result.Message}");
        return 1;
    }

    private static string[] TypesFor(ProjectInfo project) => project.Language switch
    {
        "csharp" => DotnetTypes,
        "typescript" or "javascript" => NodeTypes,
        "python" => PythonTypes,
        "go" => GoTypes,
        "rust" => RustTypes,
        _ => []
    };

    private static int ShowTypes(ProjectInfo project)
    {
        ConsoleService.ShowBanner("Types de composants", project.Description);
        foreach (var type in TypesFor(project))
            ConsoleService.MarkupLine($"  [green]{ConsoleService.Escape(type)}[/]");
        ConsoleService.WriteLine();
        ConsoleService.Info($"Exemple : scaffold add {TypesFor(project).FirstOrDefault() ?? "service"} MonComposant");
        return 0;
    }

    private static bool IsValidName(string name) =>
        name.Length is > 0 and <= 100 &&
        name.All(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.');

    private static void ShowDryRun(string type, string name, string cwd, ProjectInfo info)
    {
        ConsoleService.Info("PREVISUALISATION (--dry-run) :");
        ConsoleService.SummaryLine("Type", type);
        ConsoleService.SummaryLine("Nom", name);
        ConsoleService.SummaryLine("Projet", info.Description);
        ConsoleService.SummaryLine("Dossier", cwd);
        ConsoleService.SummaryLine("Fichiers", string.Join(", ", PlannedFiles(type, name, info)));
        ConsoleService.Info("Aucun fichier genere. Retire --dry-run pour generer.");
    }

    private static IEnumerable<string> PlannedFiles(string type, string name, ProjectInfo info)
    {
        if (info.Language == "csharp") return type switch
        {
            "controller" => [$"Controllers/{name}Controller.cs"],
            "entity" => [$"Models/{name}.cs"],
            "repository" => [$"Repositories/I{name}Repository.cs", $"Repositories/{name}Repository.cs"],
            "service" => [$"Services/I{name}Service.cs", $"Services/{name}Service.cs"],
            "page" => [$"Pages/{name}.cshtml", $"Pages/{name}.cshtml.cs"],
            "migration" => [$"Migrations/{DateTime.Now:yyyyMMddHHmmss}_{name}.cs"],
            "middleware" => [$"Middleware/{name}Middleware.cs"],
            "filter" => [$"Filters/{name}Filter.cs"],
            _ => []
        };

        if (info.Language is "typescript" or "javascript") return type switch
        {
            "component" => [$"src/components/{Pascal(name)}.{info.Ext}"],
            "hook" => [$"src/hooks/use{Pascal(name)}.{info.Ext}"],
            "service" => [$"src/services/{Camel(name)}Service.{info.Ext}"],
            "page" => [$"src/pages/{Pascal(name)}.{info.Ext}"],
            "route" => [$"routes/{Kebab(name)}.{info.Ext}"],
            "test" => [$"tests/{Pascal(name)}.test.{info.Ext}"],
            _ => []
        };

        if (info.Language == "python") return type switch
        {
            "module" => [$"{Snake(name)}.py"],
            "class" => [$"{Snake(name)}.py"],
            "route" => [$"routes/{Snake(name)}.py"],
            "service" => [$"services/{Snake(name)}.py"],
            "test" => [$"tests/test_{Snake(name)}.py"],
            _ => []
        };

        if (info.Language == "go") return type switch
        {
            "handler" => [$"internal/handlers/{Snake(name)}.go"],
            "model" => [$"internal/models/{Snake(name)}.go"],
            "service" => [$"internal/services/{Snake(name)}.go"],
            "test" => [$"internal/{Snake(name)}_test.go"],
            _ => []
        };

        if (info.Language == "rust") return type switch
        {
            "module" => [$"src/{Snake(name)}.rs"],
            "struct" => [$"src/{Snake(name)}.rs"],
            "handler" => [$"src/handlers/{Snake(name)}.rs"],
            "test" => [$"tests/{Snake(name)}.rs"],
            _ => []
        };

        return [];
    }

    // ---------------- .NET ----------------

    private static (bool Success, string Message) GenerateDotnet(
        string type, string cwd, string name, ProjectInfo info)
    {
        var ns = info.Namespace ?? Path.GetFileName(cwd);

        return type switch
        {
            "controller" => Write(cwd, "Controllers", $"{name}Controller.cs", DotnetController(ns, name)),
            "entity" => Write(cwd, "Models", $"{name}.cs", DotnetEntity(ns, name)),
            "repository" => GenerateDotnetRepository(cwd, name, ns),
            "service" => GenerateDotnetService(cwd, name, ns),
            "page" => GenerateRazorPage(cwd, name, ns),
            "migration" => Write(cwd, "Migrations",
                $"{DateTime.Now:yyyyMMddHHmmss}_{name}.cs", DotnetMigration(ns, name)),
            "middleware" => Write(cwd, "Middleware", $"{name}Middleware.cs", DotnetMiddleware(ns, name)),
            "filter" => Write(cwd, "Filters", $"{name}Filter.cs", DotnetFilter(ns, name)),
            _ => (false, "Type non supporte")
        };
    }

    private static string DotnetController(string ns, string name) => $$"""
        using Microsoft.AspNetCore.Mvc;

        namespace {{ns}}.Controllers;

        [ApiController]
        [Route("api/[controller]")]
        public class {{name}}Controller : ControllerBase
        {
            [HttpGet]
            public IActionResult GetAll() => Ok(Array.Empty<object>());

            [HttpGet("{id:int}")]
            public IActionResult GetById(int id) => Ok(new { id });

            [HttpPost]
            public IActionResult Create([FromBody] object data) => Ok(data);

            [HttpPut("{id:int}")]
            public IActionResult Update(int id, [FromBody] object data) => Ok(new { id, data });

            [HttpDelete("{id:int}")]
            public IActionResult Delete(int id) => NoContent();
        }
        """;

    private static string DotnetEntity(string ns, string name) => $$"""
        namespace {{ns}}.Models;

        public class {{name}}
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        }
        """;

    private static (bool, string) GenerateDotnetRepository(string cwd, string name, string ns)
    {
        var iface = $$"""
            namespace {{ns}}.Repositories;

            public interface I{{name}}Repository
            {
                Task<IReadOnlyList<object>> GetAllAsync(CancellationToken ct = default);
                Task<object?> GetByIdAsync(int id, CancellationToken ct = default);
            }
            """;

        var impl = $$"""
            using Microsoft.EntityFrameworkCore;

            namespace {{ns}}.Repositories;

            public class {{name}}Repository : I{{name}}Repository
            {
                // Injecte ton DbContext ici.
                public Task<IReadOnlyList<object>> GetAllAsync(CancellationToken ct = default)
                    => Task.FromResult<IReadOnlyList<object>>(Array.Empty<object>());

                public Task<object?> GetByIdAsync(int id, CancellationToken ct = default)
                    => Task.FromResult<object?>(null);
            }
            """;

        var a = Write(cwd, "Repositories", $"I{name}Repository.cs", iface);
        if (!a.Success) return a;
        return Write(cwd, "Repositories", $"{name}Repository.cs", impl);
    }

    private static (bool, string) GenerateDotnetService(string cwd, string name, string ns)
    {
        var iface = $$"""
            namespace {{ns}}.Services;

            public interface I{{name}}Service
            {
                Task<object?> GetAsync(int id, CancellationToken ct = default);
            }
            """;

        var impl = $$"""
            namespace {{ns}}.Services;

            public class {{name}}Service : I{{name}}Service
            {
                public Task<object?> GetAsync(int id, CancellationToken ct = default)
                    => Task.FromResult<object?>(null);
            }
            """;

        var a = Write(cwd, "Services", $"I{name}Service.cs", iface);
        if (!a.Success) return a;
        return Write(cwd, "Services", $"{name}Service.cs", impl);
    }

    private static (bool, string) GenerateRazorPage(string cwd, string name, string ns)
    {
        var razor = $$"""
            @page
            @model {{ns}}.Pages.{{name}}Model
            @{
                ViewData["Title"] = "{{name}}";
            }

            <h1>{{name}}</h1>
            """;

        var model = $$"""
            using Microsoft.AspNetCore.Mvc.RazorPages;

            namespace {{ns}}.Pages;

            public class {{name}}Model : PageModel
            {
                public void OnGet()
                {
                }
            }
            """;

        var a = Write(cwd, "Pages", $"{name}.cshtml", razor);
        if (!a.Success) return a;
        return Write(cwd, "Pages", $"{name}.cshtml.cs", model);
    }

    private static string DotnetMigration(string ns, string name) => $$"""
        using Microsoft.EntityFrameworkCore.Migrations;

        namespace {{ns}}.Migrations;

        public partial class {{name}} : Migration
        {
            protected override void Up(MigrationBuilder migrationBuilder)
            {
                // TODO: decrire la migration
            }

            protected override void Down(MigrationBuilder migrationBuilder)
            {
                // TODO: annuler la migration
            }
        }
        """;

    private static string DotnetMiddleware(string ns, string name) => $$"""
        namespace {{ns}}.Middleware;

        public class {{name}}Middleware
        {
            private readonly RequestDelegate _next;

            public {{name}}Middleware(RequestDelegate next) => _next = next;

            public async Task InvokeAsync(HttpContext context)
            {
                // Avant la requete
                await _next(context);
                // Apres la requete
            }
        }

        public static class {{name}}MiddlewareExtensions
        {
            public static IApplicationBuilder Use{{name}}(this IApplicationBuilder app)
                => app.UseMiddleware<{{name}}Middleware>();
        }
        """;

    private static string DotnetFilter(string ns, string name) => $$"""
        using Microsoft.AspNetCore.Mvc.Filters;

        namespace {{ns}}.Filters;

        public class {{name}}Filter : IActionFilter
        {
            public void OnActionExecuting(ActionExecutingContext context)
            {
                // Avant l'action
            }

            public void OnActionExecuted(ActionExecutedContext context)
            {
                // Apres l'action
            }
        }
        """;

    // ---------------- Node ----------------

    private static (bool Success, string Message) GenerateNode(
        string type, string cwd, string name, ProjectInfo info)
    {
        var ext = info.Ext;
        var pascal = Pascal(name);
        var camel = Camel(name);

        return type switch
        {
            "component" => Write(cwd, "src/components", $"{pascal}.{ext}",
                ext == "ts"
                    ? $"type {pascal}Props = {{\n  children?: React.ReactNode;\n}};\n\nexport function {pascal}({{ children }}: {pascal}Props) {{\n  return <div className=\"{Kebab(name)}\">{{children}}</div>;\n}}\n"
                    : $"export function {pascal}({{ children }}) {{\n  return <div className=\"{Kebab(name)}\">{{children}}</div>;\n}}\n"),

            "hook" => Write(cwd, "src/hooks", $"use{pascal}.{ext}",
                ext == "ts"
                    ? $"import {{ useState }} from 'react';\n\nexport function use{pascal}<T>(initial: T) {{\n  const [value, setValue] = useState<T>(initial);\n  return {{ value, setValue }} as const;\n}}\n"
                    : $"import {{ useState }} from 'react';\n\nexport function use{pascal}(initial) {{\n  const [value, setValue] = useState(initial);\n  return {{ value, setValue }};\n}}\n"),

            "service" => Write(cwd, "src/services", $"{camel}Service.{ext}",
                ext == "ts"
                    ? $"export interface {pascal} {{\n  id: number;\n  name: string;\n}}\n\nconst BASE = '/api/{Kebab(name)}';\n\nexport const {camel}Service = {{\n  async list(): Promise<{pascal}[]> {{\n    const res = await fetch(BASE);\n    if (!res.ok) throw new Error(`HTTP ${{res.status}}`);\n    return res.json();\n  }},\n\n  async get(id: number): Promise<{pascal}> {{\n    const res = await fetch(`${{BASE}}/${{id}}`);\n    if (!res.ok) throw new Error(`HTTP ${{res.status}}`);\n    return res.json();\n  }}\n}};\n"
                    : $"const BASE = '/api/{Kebab(name)}';\n\nexport const {camel}Service = {{\n  async list() {{\n    const res = await fetch(BASE);\n    if (!res.ok) throw new Error(`HTTP ${{res.status}}`);\n    return res.json();\n  }}\n}};\n"),

            "page" => Write(cwd, "src/pages", $"{pascal}.{ext}",
                $"export default function {pascal}Page() {{\n  return (\n    <main>\n      <h1>{pascal}</h1>\n    </main>\n  );\n}}\n"),

            "route" => Write(cwd, "routes", $"{Kebab(name)}.{ext}",
                $"import {{ Router }} from 'express';\n\nconst router = Router();\n\nrouter.get('/', (_req, res) => {{\n  res.json({{ ok: true, resource: '{Kebab(name)}' }});\n}});\n\nexport default router;\n"),

            "test" => Write(cwd, "tests", $"{pascal}.test.{ext}",
                $"import {{ describe, it, expect }} from 'vitest';\n\ndescribe('{pascal}', () => {{\n  it('works', () => {{\n    expect(true).toBe(true);\n  }});\n}});\n"),

            _ => (false, "Type non supporte")
        };
    }

    // ---------------- Python ----------------

    private static (bool Success, string Message) GeneratePython(
        string type, string cwd, string name, ProjectInfo info)
    {
        var snake = Snake(name);
        var pascal = Pascal(name);

        return type switch
        {
            "module" => Write(cwd, ".", $"{snake}.py",
                $"\"\"\"Module {name}.\"\"\"\n\n\ndef main() -> None:\n    print(\"{pascal} ready\")\n\n\nif __name__ == \"__main__\":\n    main()\n"),

            "class" => Write(cwd, ".", $"{snake}.py",
                $"class {pascal}:\n    def __init__(self) -> None:\n        self.name = \"{name}\"\n\n    def __repr__(self) -> str:\n        return f\"{pascal}(name={{self.name!r}})\"\n"),

            "route" => Write(cwd, "routes", $"{snake}.py",
                $"from fastapi import APIRouter\n\nrouter = APIRouter(prefix=\"/{Kebab(name)}\", tags=[\"{Kebab(name)}\"])\n\n\n@router.get(\"\")\ndef list_{snake}():\n    return []\n\n\n@router.get(\"/{{item_id}}\")\ndef get_{snake}(item_id: int):\n    return {{\"id\": item_id}}\n"),

            "service" => Write(cwd, "services", $"{snake}.py",
                $"class {pascal}Service:\n    def get(self, item_id: int):\n        return {{\"id\": item_id}}\n"),

            "test" => Write(cwd, "tests", $"test_{snake}.py",
                $"def test_{snake}_works():\n    assert True\n"),

            _ => (false, "Type non supporte")
        };
    }

    // ---------------- Go ----------------

    private static (bool Success, string Message) GenerateGo(
        string type, string cwd, string name, ProjectInfo info)
    {
        var snake = Snake(name);
        var pascal = Pascal(name);
        var module = info.Namespace ?? "app";

        return type switch
        {
            "handler" => Write(cwd, "internal/handlers", $"{snake}.go",
                $"package handlers\n\nimport (\n\t\"encoding/json\"\n\t\"net/http\"\n)\n\nfunc {pascal}Handler(w http.ResponseWriter, r *http.Request) {{\n\tw.Header().Set(\"Content-Type\", \"application/json\")\n\t_ = json.NewEncoder(w).Encode(map[string]any{{\"ok\": true, \"resource\": \"{Kebab(name)}\"}})\n}}\n"),

            "model" => Write(cwd, "internal/models", $"{snake}.go",
                $"package models\n\nimport \"time\"\n\ntype {pascal} struct {{\n\tID        int       `json:\"id\"`\n\tName      string    `json:\"name\"`\n\tCreatedAt time.Time `json:\"created_at\"`\n}}\n"),

            "service" => Write(cwd, "internal/services", $"{snake}.go",
                $"package services\n\ntype {pascal}Service struct{{}}\n\nfunc New{pascal}Service() *{pascal}Service {{\n\treturn &{pascal}Service{{}}\n}}\n\nfunc (s *{pascal}Service) Get(id int) any {{\n\treturn map[string]any{{\"id\": id}}\n}}\n"),

            "test" => Write(cwd, "internal", $"{snake}_test.go",
                $"package internal\n\nimport \"testing\"\n\nfunc Test{Pascal(name)}(t *testing.T) {{\n\tt.Log(\"{module}: {name} OK\")\n}}\n"),

            _ => (false, "Type non supporte")
        };
    }

    // ---------------- Rust ----------------

    private static (bool Success, string Message) GenerateRust(
        string type, string cwd, string name, ProjectInfo info)
    {
        var snake = Snake(name);
        var pascal = Pascal(name);

        return type switch
        {
            "module" => Write(cwd, "src", $"{snake}.rs",
                $"pub fn run() {{\n    println!(\"{pascal} ready\");\n}}\n"),

            "struct" => Write(cwd, "src", $"{snake}.rs",
                $"#[derive(Debug, Clone, Default)]\npub struct {pascal} {{\n    pub id: i64,\n    pub name: String,\n}}\n\nimpl {pascal} {{\n    pub fn new(name: impl Into<String>) -> Self {{\n        Self {{ id: 0, name: name.into() }}\n    }}\n}}\n"),

            "handler" => Write(cwd, "src/handlers", $"{snake}.rs",
                $"pub async fn {snake}() -> &'static str {{\n    \"{pascal} OK\"\n}}\n"),

            "test" => Write(cwd, "tests", $"{snake}.rs",
                $"#[test]\nfn {snake}_works() {{\n    assert!(true);\n}}\n"),

            _ => (false, "Type non supporte")
        };
    }

    // ---------------- Helpers ----------------

    private static (bool Success, string Message) Write(
        string cwd, string relativeDir, string file, string content)
    {
        var dir = relativeDir == "." ? cwd : Path.Combine(cwd, relativeDir);
        Directory.CreateDirectory(dir);

        var path = Path.Combine(dir, file);
        if (File.Exists(path))
            return (false, $"Le fichier existe deja : {Path.GetRelativePath(cwd, path)}");

        File.WriteAllText(path, content + Environment.NewLine);
        ConsoleService.Debug($"Fichier cree : {Path.GetRelativePath(cwd, path)}");
        return (true, "");
    }

    private static string Pascal(string value)
    {
        var parts = value.Split(['-', '_', '.', ' '], StringSplitOptions.RemoveEmptyEntries);
        var builder = new StringBuilder();
        foreach (var part in parts)
            builder.Append(char.ToUpperInvariant(part[0])).Append(part[1..]);
        return builder.Length == 0 ? "Item" : builder.ToString();
    }

    private static string Camel(string value)
    {
        var pascal = Pascal(value);
        return char.ToLowerInvariant(pascal[0]) + pascal[1..];
    }

    private static string Snake(string value)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (c is '-' or '.' or ' ') builder.Append('_');
            else if (char.IsUpper(c) && i > 0 && builder.Length > 0 && builder[^1] != '_')
                builder.Append('_').Append(char.ToLowerInvariant(c));
            else builder.Append(char.ToLowerInvariant(c));
        }

        var result = builder.ToString().Replace("__", "_").Trim('_');
        return string.IsNullOrEmpty(result) ? "item" : result;
    }

    private static string Kebab(string value) =>
        Snake(value).Replace('_', '-');

    private static ProjectInfo DetectProject(string cwd)
    {
        var lang = LanguageDetector.Detect(cwd);
        var ns = Path.GetFileName(cwd);

        var csproj = Directory.GetFiles(cwd, "*.csproj", SearchOption.TopDirectoryOnly).FirstOrDefault();
        if (csproj != null)
        {
            var content = File.ReadAllText(csproj);
            var nsFromProject = Path.GetFileNameWithoutExtension(csproj);
            var rootNs = string.IsNullOrWhiteSpace(nsFromProject) ? ns : nsFromProject;

            if (content.Contains("Microsoft.NET.Sdk.Web", StringComparison.Ordinal))
                return new("dotnet-web", ".NET Web API", "csharp", rootNs, "cs");
            if (content.Contains("Blazor", StringComparison.Ordinal))
                return new("dotnet-blazor", ".NET Blazor", "csharp", rootNs, "cs");

            return new("dotnet", ".NET", "csharp", rootNs, "cs");
        }

        var isTs = File.Exists(Path.Combine(cwd, "tsconfig.json"));
        var nodeExt = isTs ? "tsx" : "jsx";

        return lang switch
        {
            "node" or "deno" => new("node", "Node.js", isTs ? "typescript" : "javascript", ns, nodeExt),
            "python" => new("python", "Python", "python", ns, "py"),
            "rust" => new("rust", "Rust", "rust", ns, "rs"),
            "go" => new("go", "Go", "go", ns, "go"),
            _ => new("unknown", "Projet inconnu", "unknown", ns, "txt")
        };
    }

    private record ProjectInfo(
        string Type, string Description, string Language, string? Namespace, string Ext);
}
