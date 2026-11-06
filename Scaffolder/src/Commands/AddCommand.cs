using System.CommandLine;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class AddCommand : Command
{
    private static readonly string[] ComponentTypes = [
        "controller", "entity", "repository", "service", "page", "migration", "middleware", "filter"
    ];

    public AddCommand() : base("add", "Genere un composant individuel dans le projet courant")
    {
        var typeArg = new Argument<string>("type")
        {
            Description = $"Type de composant ({string.Join(", ", ComponentTypes)})"
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

        Add(typeArg);
        Add(nameArg);
        Add(outputOpt);
        Add(dryRunOpt);

        SetAction((ParseResult pr) => HandleAdd(
            pr.GetValue(typeArg)!, pr.GetValue(nameArg)!,
            pr.GetValue(outputOpt), pr.GetValue(dryRunOpt)));
    }

    private static int HandleAdd(string type, string name, DirectoryInfo? output, bool dryRun)
    {
        type = type.ToLowerInvariant().Trim();
        name = name.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            ConsoleService.Error("Le nom du composant est requis.");
            return 1;
        }

        if (!ComponentTypes.Contains(type))
        {
            ConsoleService.Error($"Type '{type}' inconnu. Types : {string.Join(", ", ComponentTypes)}");
            return 1;
        }

        var cwd = output?.FullName ?? Directory.GetCurrentDirectory();
        var projectInfo = DetectProject(cwd);

        if (projectInfo.Type == "unknown")
        {
            ConsoleService.Warning("Aucun projet detecte dans le dossier courant.");
            ConsoleService.Info("Place-toi dans un projet existant ou utilise --output.");
            return 1;
        }

        if (dryRun)
        {
            ShowDryRun(type, name, cwd, projectInfo);
            return 0;
        }

        ConsoleService.Info($"  Ajout de {type} '{name}' au projet {projectInfo.Description}...");

        var result = type switch
        {
            "controller" => GenerateController(cwd, name, projectInfo),
            "entity" => GenerateEntity(cwd, name, projectInfo),
            "repository" => GenerateRepository(cwd, name, projectInfo),
            "service" => GenerateService(cwd, name, projectInfo),
            "page" => GeneratePage(cwd, name, projectInfo),
            "migration" => GenerateMigration(cwd, name, projectInfo),
            "middleware" => GenerateMiddleware(cwd, name, projectInfo),
            "filter" => GenerateFilter(cwd, name, projectInfo),
            _ => (Success: false, Message: "Type non supporte")
        };

        if (result.Success)
            ConsoleService.Success($"{type} '{name}' genere avec succes !");
        else
            ConsoleService.Error($"Echec : {result.Message}");

        return result.Success ? 0 : 1;
    }

    private static void ShowDryRun(string type, string name, string cwd, ProjectInfo info)
    {
        ConsoleService.Info("PREVISUALISATION (--dry-run) :");
        ConsoleService.SummaryLine("Type", type);
        ConsoleService.SummaryLine("Nom", name);
        ConsoleService.SummaryLine("Projet", info.Description);
        ConsoleService.SummaryLine("Dossier", cwd);

        var files = type switch
        {
            "controller" => new[] { $"Controllers/{name}Controller.cs" },
            "entity" => new[] { $"Models/{name}.cs" },
            "repository" => new[] { $"Repositories/I{name}Repository.cs", $"Repositories/{name}Repository.cs" },
            "service" => new[] { $"Services/I{name}Service.cs", $"Services/{name}Service.cs" },
            "page" => new[] { $"Pages/{name}.cshtml", $"Pages/{name}.cshtml.cs" },
            "migration" => new[] { $"Migrations/{DateTime.Now:yyyyMMddHHmmss}_{name}.cs" },
            "middleware" => new[] { $"Middleware/{name}Middleware.cs" },
            "filter" => new[] { $"Filters/{name}Filter.cs" },
            _ => Array.Empty<string>()
        };

        ConsoleService.SummaryLine("Fichiers", string.Join(", ", files));
        ConsoleService.Info("Aucun fichier genere. Retire --dry-run pour generer.");
    }

    private static (bool Success, string Message) GenerateController(string cwd, string name, ProjectInfo info)
    {
        var dir = Path.Combine(cwd, "Controllers");
        Directory.CreateDirectory(dir);

        var ns = info.Namespace ?? Path.GetFileName(cwd);
        var content = $"using Microsoft.AspNetCore.Mvc;\n\n" +
            $"namespace {ns}.Controllers;\n\n" +
            $"[ApiController]\n" +
            $"[Route(\"api/[controller]\")]\n" +
            $"public class {name}Controller : ControllerBase\n" +
            $"{{\n" +
            $"    [HttpGet]\n" +
            $"    public IActionResult GetAll()\n" +
            $"    {{\n" +
            $"        return Ok(new {{ message = \"{name} endpoint\" }});\n" +
            $"    }}\n\n" +
            $"    [HttpGet(\"{{id}}\")]\n" +
            $"    public IActionResult GetById(int id)\n" +
            $"    {{\n" +
            $"        return Ok(new {{ id, message = \"{name} item\" }});\n" +
            $"    }}\n\n" +
            $"    [HttpPost]\n" +
            $"    public IActionResult Create([FromBody] object data)\n" +
            $"    {{\n" +
            $"        return CreatedAtAction(nameof(GetById), new {{ id = 1 }}, data);\n" +
            $"    }}\n" +
            $"}}\n";

        File.WriteAllText(Path.Combine(dir, $"{name}Controller.cs"), content);
        return (true, "");
    }

    private static (bool Success, string Message) GenerateEntity(string cwd, string name, ProjectInfo info)
    {
        var dir = Path.Combine(cwd, "Models");
        Directory.CreateDirectory(dir);

        var ns = info.Namespace ?? Path.GetFileName(cwd);
        var content = $"namespace {ns}.Models;\n\n" +
            $"public class {name}\n" +
            $"{{\n" +
            $"    public int Id {{ get; set; }}\n" +
            $"    public string Name {{ get; set; }} = string.Empty;\n" +
            $"    public DateTime CreatedAt {{ get; set; }} = DateTime.UtcNow;\n" +
            $"}}\n";

        File.WriteAllText(Path.Combine(dir, $"{name}.cs"), content);
        return (true, "");
    }

    private static (bool Success, string Message) GenerateRepository(string cwd, string name, ProjectInfo info)
    {
        var dir = Path.Combine(cwd, "Repositories");
        Directory.CreateDirectory(dir);

        var ns = info.Namespace ?? Path.GetFileName(cwd);
        var iface = $"I{name}Repository";
        var impl = $"{name}Repository";

        var ifaceContent = $"namespace {ns}.Repositories;\n\n" +
            $"public interface {iface}\n" +
            $"{{\n" +
            $"    Task<{name}?> GetByIdAsync(int id);\n" +
            $"    Task<IEnumerable<{name}>> GetAllAsync();\n" +
            $"    Task<{name}> CreateAsync({name} entity);\n" +
            $"    Task UpdateAsync({name} entity);\n" +
            $"    Task DeleteAsync(int id);\n" +
            $"}}\n";

        var implContent = $"namespace {ns}.Repositories;\n\n" +
            $"public class {impl} : {iface}\n" +
            $"{{\n" +
            $"    private readonly List<{name}> _items = new();\n" +
            $"    private int _nextId = 1;\n\n" +
            $"    public Task<{name}?> GetByIdAsync(int id)\n" +
            $"        => Task.FromResult(_items.FirstOrDefault(x => x.Id == id));\n\n" +
            $"    public Task<IEnumerable<{name}>> GetAllAsync()\n" +
            $"        => Task.FromResult<IEnumerable<{name}>>(_items.ToList());\n\n" +
            $"    public Task<{name}> CreateAsync({name} entity)\n" +
            $"    {{\n" +
            $"        entity.Id = _nextId++;\n" +
            $"        _items.Add(entity);\n" +
            $"        return Task.FromResult(entity);\n" +
            $"    }}\n\n" +
            $"    public Task UpdateAsync({name} entity)\n" +
            $"    {{\n" +
            $"        var existing = _items.FirstOrDefault(x => x.Id == entity.Id);\n" +
            $"        if (existing != null) existing.Name = entity.Name;\n" +
            $"        return Task.CompletedTask;\n" +
            $"    }}\n\n" +
            $"    public Task DeleteAsync(int id)\n" +
            $"    {{\n" +
            $"        _items.RemoveAll(x => x.Id == id);\n" +
            $"        return Task.CompletedTask;\n" +
            $"    }}\n" +
            $"}}\n";

        File.WriteAllText(Path.Combine(dir, $"{iface}.cs"), ifaceContent);
        File.WriteAllText(Path.Combine(dir, $"{impl}.cs"), implContent);
        return (true, "");
    }

    private static (bool Success, string Message) GenerateService(string cwd, string name, ProjectInfo info)
    {
        var dir = Path.Combine(cwd, "Services");
        Directory.CreateDirectory(dir);

        var ns = info.Namespace ?? Path.GetFileName(cwd);
        var iface = $"I{name}Service";
        var impl = $"{name}Service";

        var ifaceContent = $"namespace {ns}.Services;\n\n" +
            $"public interface {iface}\n" +
            $"{{\n" +
            $"    Task<string> ProcessAsync(string input);\n" +
            $"}}\n";

        var implContent = $"using Microsoft.Extensions.Logging;\n\n" +
            $"namespace {ns}.Services;\n\n" +
            $"public class {impl} : {iface}\n" +
            $"{{\n" +
            $"    private readonly ILogger<{impl}> _logger;\n\n" +
            $"    public {impl}(ILogger<{impl}> logger)\n" +
            $"    {{\n" +
            $"        _logger = logger;\n" +
            $"    }}\n\n" +
            $"    public Task<string> ProcessAsync(string input)\n" +
            $"    {{\n" +
            $"        _logger.LogInformation(\"Processing {{Input}}\", input);\n" +
            $"        var result = input.ToUpperInvariant();\n" +
            $"        return Task.FromResult(result);\n" +
            $"    }}\n" +
            $"}}\n";

        File.WriteAllText(Path.Combine(dir, $"{iface}.cs"), ifaceContent);
        File.WriteAllText(Path.Combine(dir, $"{impl}.cs"), implContent);
        return (true, "");
    }

    private static (bool Success, string Message) GeneratePage(string cwd, string name, ProjectInfo info)
    {
        var dir = Path.Combine(cwd, "Pages");
        Directory.CreateDirectory(dir);

        var content = $"@page\n" +
            $"@model {info.Namespace ?? Path.GetFileName(cwd)}.Pages.{name}Model\n" +
            $"@{{\n" +
            $"    ViewData[\"Title\"] = \"{name}\";\n" +
            $"}}\n\n" +
            $"<h1>{name}</h1>\n" +
            $"<p>Page generee par Scaffolder.</p>\n";

        var modelContent = $"using Microsoft.AspNetCore.Mvc.RazorPages;\n\n" +
            $"namespace {info.Namespace ?? Path.GetFileName(cwd)}.Pages;\n\n" +
            $"public class {name}Model : PageModel\n" +
            $"{{\n" +
            $"    public string Message {{ get; set; }} = string.Empty;\n\n" +
            $"    public void OnGet()\n" +
            $"    {{\n" +
            $"        Message = \"Bienvenue sur {name}!\";\n" +
            $"    }}\n" +
            $"}}\n";

        File.WriteAllText(Path.Combine(dir, $"{name}.cshtml"), content);
        File.WriteAllText(Path.Combine(dir, $"{name}.cshtml.cs"), modelContent);
        return (true, "");
    }

    private static (bool Success, string Message) GenerateMigration(string cwd, string name, ProjectInfo info)
    {
        var dir = Path.Combine(cwd, "Migrations");
        Directory.CreateDirectory(dir);

        var ns = info.Namespace ?? Path.GetFileName(cwd);
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        var content = $"using Microsoft.EntityFrameworkCore.Migrations;\n\n" +
            $"#nullable disable\n\n" +
            $"namespace {ns}.Migrations;\n\n" +
            $"public partial class {name} : Migration\n" +
            $"{{\n" +
            $"    protected override void Up(MigrationBuilder migrationBuilder)\n" +
            $"    {{\n" +
            $"        // TODO: Ajouter la logique de migration\n" +
            $"    }}\n\n" +
            $"    protected override void Down(MigrationBuilder migrationBuilder)\n" +
            $"    {{\n" +
            $"        // TODO: Ajouter le rollback\n" +
            $"    }}\n" +
            $"}}\n";

        File.WriteAllText(Path.Combine(dir, $"{timestamp}_{name}.cs"), content);
        return (true, "");
    }

    private static (bool Success, string Message) GenerateMiddleware(string cwd, string name, ProjectInfo info)
    {
        var dir = Path.Combine(cwd, "Middleware");
        Directory.CreateDirectory(dir);

        var ns = info.Namespace ?? Path.GetFileName(cwd);
        var content = $"namespace {ns}.Middleware;\n\n" +
            $"public class {name}Middleware\n" +
            $"{{\n" +
            $"    private readonly RequestDelegate _next;\n\n" +
            $"    public {name}Middleware(RequestDelegate next)\n" +
            $"    {{\n" +
            $"        _next = next;\n" +
            $"    }}\n\n" +
            $"    public async Task InvokeAsync(HttpContext context)\n" +
            $"    {{\n" +
            $"        // Before\n" +
            $"        await _next(context);\n" +
            $"        // After\n" +
            $"    }}\n" +
            $"}}\n\n" +
            $"public static class {name}MiddlewareExtensions\n" +
            $"{{\n" +
            $"    public static IApplicationBuilder Use{name}(this IApplicationBuilder app)\n" +
            $"        => app.UseMiddleware<{name}Middleware>();\n" +
            $"}}\n";

        File.WriteAllText(Path.Combine(dir, $"{name}Middleware.cs"), content);
        return (true, "");
    }

    private static (bool Success, string Message) GenerateFilter(string cwd, string name, ProjectInfo info)
    {
        var dir = Path.Combine(cwd, "Filters");
        Directory.CreateDirectory(dir);

        var ns = info.Namespace ?? Path.GetFileName(cwd);
        var content = $"using Microsoft.AspNetCore.Mvc;\nusing Microsoft.AspNetCore.Mvc.Filters;\n\n" +
            $"namespace {ns}.Filters;\n\n" +
            $"public class {name}Filter : IActionFilter\n" +
            $"{{\n" +
            $"    public void OnActionExecuting(ActionExecutingContext context)\n" +
            $"    {{\n" +
            $"        // Before action\n" +
            $"    }}\n\n" +
            $"    public void OnActionExecuted(ActionExecutedContext context)\n" +
            $"    {{\n" +
            $"        // After action\n" +
            $"    }}\n" +
            $"}}\n";

        File.WriteAllText(Path.Combine(dir, $"{name}Filter.cs"), content);
        return (true, "");
    }

    private static ProjectInfo DetectProject(string cwd)
    {
        var lang = LanguageDetector.Detect(cwd);
        var ns = Path.GetFileName(cwd);

        var csproj = Directory.GetFiles(cwd, "*.csproj", SearchOption.TopDirectoryOnly).FirstOrDefault();
        if (csproj != null)
        {
            var content = File.ReadAllText(csproj);
            if (content.Contains("Microsoft.NET.Sdk.Web"))
                return new("dotnet-web", ".NET Web API", "csharp", ns);
            if (content.Contains("Microsoft.NET.Sdk.Blazor"))
                return new("dotnet-blazor", ".NET Blazor", "csharp", ns);
            return new("dotnet", ".NET", "csharp", ns);
        }

        return lang switch
        {
            "node" => new("node", "Node.js", "typescript", ns),
            "python" => new("python", "Python", "python", ns),
            "rust" => new("rust", "Rust", "rust", ns),
            "go" => new("go", "Go", "go", ns),
            _ => new("unknown", "Projet inconnu", "unknown", ns)
        };
    }

    private record ProjectInfo(string Type, string Description, string Language, string? Namespace);
}
