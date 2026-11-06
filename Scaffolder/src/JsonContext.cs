using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Scaffolder.Services;

namespace Scaffolder;

[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(JsonNode))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(Dictionary<string, Dictionary<string, string>>))]
[JsonSerializable(typeof(List<Dictionary<string, object>>))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(TemplatesResult))]
[JsonSerializable(typeof(TemplateInfo))]
[JsonSerializable(typeof(AiChatRequest))]
[JsonSerializable(typeof(AiMessage))]
[JsonSerializable(typeof(GeminiRequest))]
[JsonSerializable(typeof(GeminiContent))]
[JsonSerializable(typeof(GeminiPart))]
[JsonSerializable(typeof(GeminiGenerationConfig))]
[JsonSerializable(typeof(VersionManager.VersionInfo))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public partial class JsonContext : JsonSerializerContext
{
}
