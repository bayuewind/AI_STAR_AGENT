using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Interfaces;

namespace AI.PlannerClient;

public class OpenAIProvider : ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly string _baseUrl;

    public OpenAIProvider(string apiKey, string model = "gpt-4o", string baseUrl = "https://api.openai.com/v1")
    {
        if (string.IsNullOrEmpty(apiKey)) throw new ArgumentNullException(nameof(apiKey));
        _apiKey = apiKey;
        _model = model;
        _baseUrl = baseUrl.TrimEnd('/');
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(60);
    }

    public async Task<string> GetCompletionAsync(string prompt, string? systemInstruction = null)
    {
        var messages = new List<object>();
        
        if (!string.IsNullOrEmpty(systemInstruction))
        {
            messages.Add(new { role = "system", content = systemInstruction });
        }
        
        messages.Add(new { role = "user", content = prompt });

        var requestBody = new
        {
            model = _model,
            messages = messages,
            temperature = 0.2, // Low temperature for deterministic planning
            max_tokens = 2000
        };

        var jsonContent = JsonSerializer.Serialize(requestBody);
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat/completions");
        request.Headers.Add("Authorization", $"Bearer {_apiKey}");
        request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.SendAsync(request);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"OpenAI API Error: {response.StatusCode} - {responseString}");
            }

            using var doc = JsonDocument.Parse(responseString);
            var root = doc.RootElement;
            var content = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

            return content ?? string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OpenAIProvider] Error: {ex.Message}");
            throw;
        }
    }
}
