namespace Scaffolder.Models;

public class Template
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Language { get; set; } = "";

    public override string ToString() => $"{Name} — {Description}";
}
