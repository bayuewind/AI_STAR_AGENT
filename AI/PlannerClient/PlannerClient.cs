using Core.Models;
using Core.Registry;
using Core.Interfaces;
using Memory.MemoryStore;
using AI.PromptBuilder;
using AI.PlanParser;

namespace AI.PlannerClient;

public class PlannerClient
{
    private readonly PromptBuilder.PromptBuilder _promptBuilder = new();
    private readonly PlanParser.PlanParser _planParser = new();
    private readonly ILLMProvider _llmProvider;

    public PlannerClient(ILLMProvider llmProvider)
    {
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
    }

    public async Task<PlanGraph?> PlanAsync(string agentId, Goal goal, PerceptionSnapshot snapshot, MemoryStore memory, ToolRegistry toolRegistry, int tickId, IEmbeddingProvider embeddingProvider)
    {
        try 
        {
            // 1. Fetch relevant context via Semantic Retrieval
            var goalEmbedding = await embeddingProvider.GetEmbeddingAsync(goal.Text);
            
            var relevantContext = memory.Query(new MemoryQuery 
            { 
                QueryEmbedding = goalEmbedding,
                MinSimilarity = 0.8,
                Limit = 5 
            });

            var toolSchemas = toolRegistry.GetAllSchemas();

            // 2. Build the prompt
            string prompt = _promptBuilder.BuildPrompt(agentId, goal, snapshot, relevantContext, toolSchemas);

            // 3. Call LLM (Async)
            string rawResponse = await _llmProvider.GetCompletionAsync(prompt);
            
            // 4. Parse and return the plan
            return _planParser.Parse(rawResponse);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PlannerClient] Error during planning: {ex.Message}");
            return null;
        }
    }
}
