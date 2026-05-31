using System.CommandLine;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class MigrateCommand : Command
{
    public MigrateCommand() : base("migrate", "Migre un projet d'un template a un autre")
    {
        var fromArg = new Argument<string>("from")
        {
            Description = "Template source (ex: express)"
        };
        var toArg = new Argument<string>("to")
        {
            Description = "Template destination (ex: fastify)"
        };
        var pathOpt = new Option<DirectoryInfo?>("--path")
        {
            Description = "Chemin du projet a migrer"
        };
        Add(fromArg);
        Add(toArg);
        Add(pathOpt);
        SetAction((ParseResult pr) => HandleMigrate(
            pr.GetValue(fromArg), pr.GetValue(toArg), pr.GetValue(pathOpt)));
    }

    private static int HandleMigrate(string? from, string? to, DirectoryInfo? path)
    {
        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
        {
            ConsoleService.Error("Usage : scaffold migrate <de> <vers>");
            ConsoleService.Info("Exemple : scaffold migrate express fastify");
            ConsoleService.Info("Exemple : scaffold migrate flask fastapi");
            return 1;
        }

        var cwd = path?.FullName ?? Directory.GetCurrentDirectory();

        var migration = FindMigration(from, to);
        if (migration == null)
        {
            ConsoleService.Warning($"Migration de '{from}' vers '{to}' non disponible.");
            ConsoleService.Info("Migrations disponibles :");
            foreach (var m in GetMigrations())
                ConsoleService.Info($"  {m.From,-15} -> {m.To}");
            return 1;
        }

        var (fromVal, toVal, guide) = migration.Value;
        ConsoleService.Info($"Migration : {fromVal} -> {toVal}");
        Console.WriteLine();
        Console.WriteLine(guide);
        return 0;
    }

    private static (string From, string To, string Guide)? FindMigration(string from, string to)
    {
        var f = from.ToLowerInvariant();
        var t = to.ToLowerInvariant();
        return GetMigrations().FirstOrDefault(m =>
            m.From.ToLowerInvariant() == f && m.To.ToLowerInvariant() == t);
    }

    private static (string From, string To, string Guide)[] GetMigrations() =>
    [
        ("express", "fastify", """
Guide de migration Express -> Fastify :

1. Remplacer 'express' par 'fastify' dans package.json
2. Changer les routes :
   - app.get('/path', handler) -> app.get('/path', handler)
   - app.use(middleware) -> app.register(require('@fastify/middie'))
3. Remplacer res.json() par reply.send()
4. Remplacer res.status() par reply.code()
5. npm install fastify && npm uninstall express
"""),
        ("flask", "fastapi", """
Guide de migration Flask -> FastAPI :

1. Remplacer @app.route par @app.get/@app.post
2. Changer les parametres : request.json -> parametres de fonction
3. Ajouter les types Python aux parametres
4. Remplacer render_template par Jinja2Templates
5. pip install fastapi uvicorn && pip uninstall flask
"""),
        ("create-react-app", "vite", """
Guide de migration CRA -> Vite :

1. npm install vite @vitejs/plugin-react
2. Creer vite.config.js avec le plugin React
3. Deplacer index.html a la racine
4. Remplacer PUBLIC_URL par Vite env vars
5. Changer les imports d'images (import.meta.env.BASE_URL)
6. npm uninstall react-scripts
"""),
        ("javascript", "typescript", """
Guide de migration JS -> TS :

1. npm install typescript @types/node
2. Creer tsconfig.json
3. Renommer .js en .ts (ou .tsx pour React)
4. Ajouter les types aux fonctions et variables
5. npm run build pour verifier
"""),
        ("dotnet-framework", "dotnet-core", """
Guide de migration .NET Framework -> .NET Core :

1. Creer un nouveau projet .NET Core
2. Copier les fichiers source (sauf .config, packages.config)
3. Remplacer packages.config par des PackageReference
4. Mettre a jour les namespaces (System.Web -> ASP.NET Core)
5. dotnet build pour verifier
"""),
    ];
}
