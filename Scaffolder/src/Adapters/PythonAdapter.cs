using Scaffolder.Services;

namespace Scaffolder.Adapters;

public class PythonAdapter : IAdapter
{
    public string Name => "python";
    public string Description => "Projets Python (cookiecutter, pip, poetry)";
    public string[] Languages => ["python"];
    public string[] SubTemplates => ["python", "cookiecutter"];
    public bool IsAvailable => ProcessService.CommandExists("cookiecutter") || ProcessService.CommandExists("pip");

    public async Task<(int ExitCode, string Message)> ScaffoldAsync(
        string name, string outputDir, string subTemplate, string? language)
    {
        Directory.CreateDirectory(outputDir);

        ConsoleService.Info("  generation du projet Python minimal");
        Console.WriteLine();

        if (!ProcessService.CommandExists("cookiecutter"))
        {
            ConsoleService.Warning("  cookiecutter non installe. Utilisation du template minimal.");
        }

        var mainPy = $"def main():\n    print(\"Hello, {name}!\")\n\n\nif __name__ == \"__main__\":\n    main()\n";
        File.WriteAllText(Path.Combine(outputDir, "main.py"), mainPy);

        var readme = $"# {name}\n\nHello World project in Python.\n\n## Usage\n\n```bash\npython main.py\n```\n";
        File.WriteAllText(Path.Combine(outputDir, "README.md"), readme);

        return await Task.FromResult((0, $"Projet Python créé avec le template minimal"));
    }
}
