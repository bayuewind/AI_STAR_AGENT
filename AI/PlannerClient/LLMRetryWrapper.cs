using Core.Interfaces;

namespace AI.PlannerClient;

public class LLMRetryWrapper : ILLMProvider
{
    private readonly ILLMProvider _innerProvider;
    private readonly int _maxRetries;
    private readonly int _delayMs;

    public LLMRetryWrapper(ILLMProvider innerProvider, int maxRetries = 3, int delayMs = 1000)
    {
        _innerProvider = innerProvider ?? throw new ArgumentNullException(nameof(innerProvider));
        _maxRetries = maxRetries;
        _delayMs = delayMs;
    }

    public async Task<string> GetCompletionAsync(string prompt, string? systemInstruction = null)
    {
        int attempts = 0;
        while (true)
        {
            try
            {
                attempts++;
                return await _innerProvider.GetCompletionAsync(prompt, systemInstruction);
            }
            catch (Exception ex)
            {
                if (attempts >= _maxRetries)
                {
                    Console.WriteLine($"[LLMRetryWrapper] All {_maxRetries} attempts failed. Last error: {ex.Message}");
                    throw;
                }

                Console.WriteLine($"[LLMRetryWrapper] Attempt {attempts} failed: {ex.Message}. Retrying in {_delayMs}ms...");
                await Task.Delay(_delayMs);
            }
        }
    }
}
