namespace Scaffolder.Adapters;

public interface IAdapter
{
    string Name { get; }
    string Description { get; }
    string[] Languages { get; }
    string[] SubTemplates { get; }
    bool IsAvailable { get; }
    Task<(int ExitCode, string Message)> ScaffoldAsync(string name, string outputDir, string subTemplate, string? language);
}
