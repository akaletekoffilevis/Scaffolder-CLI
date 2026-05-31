using System.Net.Http.Json;
using System.Text.Json;

namespace Scaffolder.Services;

public static class AIService
{
    private static readonly HttpClient Client = new();

    public static bool HasApiKey =>
        !string.IsNullOrWhiteSpace(ConfigService.Get(ConfigService.Keys.ApiKey));

    public static async Task<string?> AskAsync(string prompt, int maxTokens = 300)
    {
        var key = ConfigService.Get(ConfigService.Keys.ApiKey);
        var model = ConfigService.Get(ConfigService.Keys.Model) ?? "gpt-4o-mini";

        if (string.IsNullOrWhiteSpace(key))
            return null;

        try
        {
            var provider = ConfigService.Get(ConfigService.Keys.Provider) ?? "openai";
            var baseUrl = provider switch
            {
                "claude" => "https://api.anthropic.com/v1/messages",
                "gemini" => "https://generativelanguage.googleapis.com/v1beta/models/" + model + ":generateContent?key=" + key,
                "grok" => "https://api.x.ai/v1/chat/completions",
                _ => "https://api.openai.com/v1/chat/completions"
            };

            if (provider is "openai" or "grok")
            {
                var body = new AiChatRequest(model,
                [
                    new AiMessage("user", prompt)
                ], maxTokens, 0.3);
                var msg = new HttpRequestMessage(HttpMethod.Post, baseUrl);
                msg.Headers.Add("Authorization", $"Bearer {key}");
                msg.Content = JsonContent.Create(body, JsonContext.Default.AiChatRequest);
                var response = await Client.SendAsync(msg);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadFromJsonAsync(JsonContext.Default.JsonElement);
                return json.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            }

            if (provider == "claude")
            {
                var body = new AiChatRequest(model,
                [
                    new AiMessage("user", prompt)
                ], maxTokens, 0.3);
                var msg = new HttpRequestMessage(HttpMethod.Post, baseUrl);
                msg.Headers.Add("x-api-key", key);
                msg.Headers.Add("anthropic-version", "2023-06-01");
                msg.Content = JsonContent.Create(body, JsonContext.Default.AiChatRequest);
                var response = await Client.SendAsync(msg);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadFromJsonAsync(JsonContext.Default.JsonElement);
                return json.GetProperty("content")[0].GetProperty("text").GetString();
            }

            if (provider == "gemini")
            {
                var geminiPayload = new StringContent(
                    "{\"contents\":[{\"parts\":[{\"text\":\"" + prompt + "\"}]}],\"generationConfig\":{\"maxOutputTokens\":" + maxTokens + ",\"temperature\":0.3}}",
                    System.Text.Encoding.UTF8,
                    "application/json");
                var response = await Client.PostAsync(baseUrl, geminiPayload);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadFromJsonAsync(JsonContext.Default.JsonElement);
                return json.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
            }

            return null;
        }
        catch (Exception ex)
        {
            ConsoleService.Warning($"Erreur AI : {ex.Message}");
            return null;
        }
    }

    public static async Task<string> SuggestAsync(string[] keywords)
    {
        if (!HasApiKey)
            return KnowledgeBase.Suggest(keywords);

        var result = await AskAsync(
            $"Tu es un assistant qui conseille des templates de projet. " +
            $"L'utilisateur cherche un template pour : {string.Join(" ", keywords)}. " +
            $"Reponds UNIQUEMENT par le nom du template parmi : " +
            $"dotnet console, dotnet webapi, dotnet blazor, dotnet maui, dotnet classlib, " +
            $"npm vite, npm next, npm react, npm vue, npm nuxt, npm svelte, npm solid, " +
            $"cargo, go, python, flutter, composer laravel, composer symfony, rails, " +
            $"gradle, swift, zig, mix, cabal, hello. " +
            $"Si aucun ne correspond, reponds 'hello'.");

        if (result == null)
            return KnowledgeBase.Suggest(keywords);

        return result.Trim().ToLowerInvariant();
    }

    public static async Task<(string? Title, string? Content)> ExplainAsync(string concept)
    {
        if (!HasApiKey)
            return KnowledgeBase.Explain(concept);

        var result = await AskAsync(
            $"Explique le concept de '{concept}' en developpement logiciel. " +
            $"Sois concis (2-3 paragraphes), en francais, avec des exemples concrets. " +
            $"Format : TITRE: ... EXPLICATION: ...");

        if (result == null)
            return KnowledgeBase.Explain(concept);

        var parts = result.Split("EXPLICATION:");
        var title = parts.Length > 1 ? parts[0].Replace("TITRE:", "").Trim() : concept;
        var content = parts.Length > 1 ? parts[1].Trim() : result;
        return (title, content);
    }

    public static async Task<(string? Title, string? Fix)> FixAsync(string error)
    {
        if (!HasApiKey)
            return KnowledgeBase.Fix(error);

        var result = await AskAsync(
            $"L'utilisateur a cette erreur : '{error}'. " +
            $"Propose une solution en francais, concise, etape par etape. " +
            $"Format : TITRE: ... SOLUTION: ...");

        if (result == null)
            return KnowledgeBase.Fix(error);

        var parts = result.Split("SOLUTION:");
        var title = parts.Length > 1 ? parts[0].Replace("TITRE:", "").Trim() : $"Solution : {error}";
        var fix = parts.Length > 1 ? parts[1].Trim() : result;
        return (title, fix);
    }
}
