namespace Scaffolder.Services;

public static class TemplateRegistry
{
    public record TemplateInfo(string Name, string Description, string Category, string[] Tags);

    private static readonly TemplateInfo[] All =
    [
        new("hello", "Hello World dans un langage", "hello", ["starter", "beginner"]),
        new("dotnet", "Projets .NET", "dotnet", ["csharp", "fsharp", "microsoft"]),
        new("webapi", "API Web REST .NET", "dotnet", ["api", "rest", "backend"]),
        new("blazor", "Application Blazor .NET", "dotnet", ["web", "wasm", "frontend"]),
        new("console", "Application console .NET", "dotnet", ["cli", "terminal"]),
        new("classlib", "Bibliotheque de classes .NET", "dotnet", ["library", "nuget"]),
        new("maui", "Application Mobile .NET MAUI", "dotnet", ["mobile", "android", "ios"]),
        new("npm", "Projets JavaScript/Node.js", "node", ["javascript", "typescript"]),
        new("vite", "Application Vite (React/Vue/Svelte)", "node", ["frontend", "bundler", "fast"]),
        new("react", "Application React", "node", ["react", "spa", "frontend"]),
        new("vue", "Application Vue.js", "node", ["vue", "spa", "frontend"]),
        new("svelte", "Application Svelte", "node", ["svelte", "frontend", "compiler"]),
        new("solid", "Application SolidJS", "node", ["solid", "frontend", "reactive"]),
        new("next", "Application Next.js (SSR)", "node", ["nextjs", "ssr", "react"]),
        new("nuxt", "Application Nuxt.js (SSR)", "node", ["nuxtjs", "ssr", "vue"]),
        new("express", "API Express.js", "node", ["express", "api", "backend"]),
        new("nest", "API NestJS", "node", ["nestjs", "api", "enterprise"]),
        new("cargo", "Projets Rust", "rust", ["rust", "systems", "performance"]),
        new("go", "Projets Go", "go", ["golang", "backend", "concurrency"]),
        new("python", "Projets Python", "python", ["python", "data", "backend"]),
        new("fastapi", "API FastAPI Python", "python", ["fastapi", "api", "async"]),
        new("django", "Application Django", "python", ["django", "web", "fullstack"]),
        new("flutter", "Applications Flutter/Dart", "dart", ["flutter", "mobile", "android", "ios"]),
        new("laravel", "Application Laravel", "php", ["laravel", "php", "web"]),
        new("rails", "Application Ruby on Rails", "ruby", ["rails", "ruby", "web"]),
        new("phoenix", "Application Phoenix (Elixir)", "elixir", ["phoenix", "elixir", "realtime"]),
        new("gradle", "Projets JVM (Kotlin/Java)", "jvm", ["kotlin", "java", "gradle"]),
        new("swift", "Projets Swift", "swift", ["swift", "ios", "macos"]),
        new("zig", "Projets Zig", "zig", ["zig", "systems", "performance"]),
        new("haskell", "Projets Haskell", "haskell", ["haskell", "functional"]),
        new("scala", "Projets Scala", "scala", ["scala", "jvm", "functional"]),
        new("crystal", "Projets Crystal", "crystal", ["crystal", "ruby-like", "compiled"]),
        new("stack", "Stack fullstack (frontend+backend)", "stack", ["fullstack", "multi-tier"]),
    ];

    public static TemplateInfo[] GetAll() => All;

    public static TemplateInfo[] GetByCategory(string category) =>
        All.Where(t => t.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToArray();

    public static TemplateInfo[] Search(string query)
    {
        query = query ?? throw new ArgumentNullException(nameof(query));
        var q = query.ToLowerInvariant();
        return All.Where(t =>
            t.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            t.Description.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            t.Tags.Any(tag => tag.Contains(q, StringComparison.OrdinalIgnoreCase))
        ).ToArray();
    }

    public static string[] GetNames() => All.Select(t => t.Name).ToArray();

    public static string[] GetCategories() => All.Select(t => t.Category).Distinct().ToArray();
}
