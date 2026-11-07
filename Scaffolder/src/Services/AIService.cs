using System.Text;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Scaffolder.Services;

public static class AIService
{
    private static readonly HttpClient Client = CreateClient();

    private const int MaxAttempts = 3;

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        client.DefaultRequestHeaders.Add("User-Agent", "Scaffolder");
        return client;
    }

    public static bool HasApiKey =>
        !string.IsNullOrWhiteSpace(ConfigService.Get(ConfigService.Keys.ApiKey));

    public static string Provider =>
        (ConfigService.Get(ConfigService.Keys.Provider) ?? "openai").Trim().ToLowerInvariant();

    public static string Model =>
        ConfigService.Get(ConfigService.Keys.Model) ?? DefaultModel(Provider);

    public static string DefaultModel(string provider) => provider switch
    {
        "claude" => "claude-3-5-haiku-latest",
        "gemini" => "gemini-3.5-flash",
        "grok" => "grok-2-latest",
        _ => "gpt-4o-mini"
    };

    public static async Task<string?> AskAsync(
        string prompt, int maxTokens = 300, string? system = null,
        double temperature = 0.3)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return null;

        var key = ConfigService.Get(ConfigService.Keys.ApiKey);
        if (string.IsNullOrWhiteSpace(key))
            return null;

        var provider = Provider;
        var model = Model;

        try
        {
            return provider switch
            {
                "claude" => await AskAnthropicAsync(prompt, system, model, key, maxTokens, temperature),
                "gemini" => await AskGeminiAsync(prompt, system, model, key, maxTokens, temperature),
                "grok" => await AskOpenAiCompatibleAsync(
                    "https://api.x.ai/v1/chat/completions", prompt, system, model, key, maxTokens, temperature),
                _ => await AskOpenAiCompatibleAsync(
                    "https://api.openai.com/v1/chat/completions", prompt, system, model, key, maxTokens, temperature)
            };
        }
        catch (Exception ex)
        {
            ConsoleService.Warning($"Erreur AI ({provider}) : {ex.Message}");
            ConsoleService.Debug(ex.ToString());
            return null;
        }
    }

    private static async Task<string?> AskOpenAiCompatibleAsync(
        string url, string prompt, string? system, string model, string key,
        int maxTokens, double temperature)
    {
        var messages = BuildMessages(prompt, system);

        // Les modeles de raisonnement (o1/o3/gpt-5) imposent max_completion_tokens
        // et n'acceptent pas de temperature personnalisee.
        var isReasoningModel = model.StartsWith("o1", StringComparison.OrdinalIgnoreCase) ||
                               model.StartsWith("o3", StringComparison.OrdinalIgnoreCase) ||
                               model.StartsWith("o4", StringComparison.OrdinalIgnoreCase) ||
                               model.StartsWith("gpt-5", StringComparison.OrdinalIgnoreCase);

        var body = isReasoningModel
            ? new AiChatRequest(model, messages, null, maxTokens, null)
            : new AiChatRequest(model, messages, maxTokens, null, temperature);

        var response = await SendWithRetryAsync(url, () =>
        {
            var msg = new HttpRequestMessage(HttpMethod.Post, url);
            msg.Headers.Add("Authorization", $"Bearer {key}");
            msg.Content = JsonContent.Create(body, JsonContext.Default.AiChatRequest);
            return msg;
        });

        if (response == null) return null;

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.TryGetProperty("choices", out var choices) &&
               choices.ValueKind == JsonValueKind.Array &&
               choices.GetArrayLength() > 0
            ? choices[0].GetProperty("message").GetProperty("content").GetString()
            : null;
    }

    private static async Task<string?> AskAnthropicAsync(
        string prompt, string? system, string model, string key,
        int maxTokens, double temperature)
    {
        const string url = "https://api.anthropic.com/v1/messages";

        var body = new AiChatRequest(model, BuildMessages(prompt, system), maxTokens, null, temperature);

        var response = await SendWithRetryAsync(url, () =>
        {
            var msg = new HttpRequestMessage(HttpMethod.Post, url);
            msg.Headers.Add("x-api-key", key);
            msg.Headers.Add("anthropic-version", "2023-06-01");
            msg.Content = JsonContent.Create(body, JsonContext.Default.AiChatRequest);
            return msg;
        });

        if (response == null) return null;

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.TryGetProperty("content", out var content) &&
               content.ValueKind == JsonValueKind.Array &&
               content.GetArrayLength() > 0
            ? content[0].GetProperty("text").GetString()
            : null;
    }

    private static async Task<string?> AskGeminiAsync(
        string prompt, string? system, string model, string key,
        int maxTokens, double temperature)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";

        var fullPrompt = string.IsNullOrWhiteSpace(system) ? prompt : $"{system}\n\n{prompt}";
        var body = new GeminiRequest(
            [new GeminiContent([new GeminiPart(fullPrompt)])],
            new GeminiGenerationConfig(maxTokens, temperature));

        var response = await SendWithRetryAsync(url, () =>
        {
            var msg = new HttpRequestMessage(HttpMethod.Post, url);
            msg.Headers.Add("x-goog-api-key", key);
            msg.Content = JsonContent.Create(body, JsonContext.Default.GeminiRequest);
            return msg;
        });

        if (response == null) return null;

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        if (!json.RootElement.TryGetProperty("candidates", out var candidates) ||
            candidates.ValueKind != JsonValueKind.Array || candidates.GetArrayLength() == 0)
            return null;

        // Les modeles "thinking" peuvent renvoyer un candidat sans texte si le budget est court.
        if (!candidates[0].TryGetProperty("content", out var content) ||
            !content.TryGetProperty("parts", out var parts) || parts.ValueKind != JsonValueKind.Array)
            return null;

        var text = new StringBuilder();
        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("thought", out var thought) && thought.ValueKind == JsonValueKind.True)
                continue;
            if (part.TryGetProperty("text", out var t) && t.ValueKind == JsonValueKind.String)
                text.Append(t.GetString());
        }

        return text.Length > 0 ? text.ToString() : null;
    }

    private static AiMessage[] BuildMessages(string prompt, string? system)
    {
        if (string.IsNullOrWhiteSpace(system))
            return [new AiMessage("user", prompt)];

        return
        [
            new AiMessage("system", system),
            new AiMessage("user", prompt)
        ];
    }

    /// <summary>Envoie la requete avec retry (429 / 5xx) et retourne une reponse valide ou null.</summary>
    private static async Task<HttpResponseMessage?> SendWithRetryAsync(
        string url, Func<HttpRequestMessage> requestFactory)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            using var request = requestFactory();

            HttpResponseMessage response;
            try
            {
                response = await Client.SendAsync(request);
            }
            catch (HttpRequestException ex)
            {
                if (attempt == MaxAttempts)
                {
                    ConsoleService.Warning($"Requete AI impossible : {ex.Message}");
                    return null;
                }
                await Task.Delay(TimeSpan.FromMilliseconds(400 * Math.Pow(2, attempt - 1)));
                continue;
            }

            if (response.IsSuccessStatusCode)
                return response;

            var body = await response.Content.ReadAsStringAsync();
            var retryable = response.StatusCode is HttpStatusCode.TooManyRequests
                or HttpStatusCode.InternalServerError
                or HttpStatusCode.BadGateway
                or HttpStatusCode.ServiceUnavailable
                or HttpStatusCode.GatewayTimeout;

            if (!retryable || attempt == MaxAttempts)
            {
                ConsoleService.Warning($"AI HTTP {(int)response.StatusCode} : {Truncate(body, 300)}");
                response.Dispose();
                return null;
            }

            var delay = response.Headers.RetryAfter?.Delta
                        ?? TimeSpan.FromMilliseconds(500 * Math.Pow(2, attempt - 1));

            response.Dispose();
            await Task.Delay(delay);
        }

        return null;
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "...";

    /// <summary>Verifie que la cle API fonctionne (utilise par `scaffold ai test`).</summary>
    public static async Task<(bool Ok, string Message)> TestAsync()
    {
        if (!HasApiKey)
            return (false, "Aucune cle API configuree. Lance `scaffold config set apiKey <cle>`.");

        var answer = await AskAsync("Reponds uniquement par: OK", maxTokens: 256, temperature: 0);
        return answer == null
            ? (false, $"Echec de la connexion au fournisseur '{Provider}'.")
            : (true, $"Connexion OK ({Provider} / {Model}) : {answer.Trim()}");
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
            $"Si aucun ne correspond, reponds 'hello'.",
            system: "Tu reponds de facon concise et tu ne fournis jamais d'explication inutile.");

        if (string.IsNullOrWhiteSpace(result))
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
            $"Format : TITRE: ... EXPLICATION: ...",
            maxTokens: 600);

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
            $"Format : TITRE: ... SOLUTION: ...",
            maxTokens: 500);

        if (result == null)
            return KnowledgeBase.Fix(error);

        var parts = result.Split("SOLUTION:");
        var title = parts.Length > 1 ? parts[0].Replace("TITRE:", "").Trim() : $"Solution : {error}";
        var fix = parts.Length > 1 ? parts[1].Trim() : result;
        return (title, fix);
    }
}
