using Core.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AI.PlanParser;

public class PlanParser
{
    private readonly JsonSerializerOptions _options;

    public PlanParser()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            // PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower, // Not available in .NET 6
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };
        _options.Converters.Add(new JsonStringEnumConverter());
    }

    public PlanGraph? Parse(string jsonResponse)
    {
        if (string.IsNullOrWhiteSpace(jsonResponse)) return null;

        try
        {
            string cleanJson = ExtractJson(jsonResponse);
            
            var decision = JsonSerializer.Deserialize<DecisionResponseDTO>(cleanJson, _options);
            if (decision?.PlanGraph == null) return null;

            if (!decision.PlanGraph.Validate())
            {
                Console.WriteLine($"[PlanParser] Warning: PlanGraph validation failed for {decision.PlanGraph.PlanId}");
                return null;
            }

            return decision.PlanGraph;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PlanParser] Error parsing LLM response: {ex.Message}");
            return null;
        }
    }

    private string ExtractJson(string input)
    {
        input = input.Trim();
        if (input.StartsWith("```json") && input.EndsWith("```"))
        {
            return input.Substring(7, input.Length - 10).Trim();
        }
        if (input.StartsWith("```") && input.EndsWith("```"))
        {
            return input.Substring(3, input.Length - 6).Trim();
        }
        return input;
    }

    private class DecisionResponseDTO
    {
        [JsonPropertyName("protocol_version")]
        public string ProtocolVersion { get; set; } = string.Empty;

        [JsonPropertyName("mode")]
        public string Mode { get; set; } = string.Empty;

        [JsonPropertyName("plan_graph")]
        public PlanGraph? PlanGraph { get; set; }
    }
}
