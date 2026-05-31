using System.Text.Json.Serialization;

namespace Scaffolder;

public sealed record TemplatesResult(TemplateInfo[] Templates);
public sealed record TemplateInfo(string Name, string Description);

public sealed record AiMessage(string Role, string Content);
public sealed record AiChatRequest(string Model, AiMessage[] Messages, int MaxTokens, double Temperature);
