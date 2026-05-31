using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scaffolder;

[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(List<Dictionary<string, object>>))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(TemplatesResult))]
[JsonSerializable(typeof(TemplateInfo))]
[JsonSerializable(typeof(AiChatRequest))]
[JsonSerializable(typeof(AiMessage))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public partial class JsonContext : JsonSerializerContext
{
}
