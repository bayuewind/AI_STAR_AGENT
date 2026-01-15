using Core.Models;

namespace Core.Interfaces;

/// <summary>
/// ILLMProvider - Interface for Large Language Model providers.
/// Enables swapping between different models (Gemini, GPT, Local LLMs).
/// </summary>
public interface ILLMProvider
{
    /// <summary>
    /// Sends a prompt to the LLM and returns the raw string response.
    /// </summary>
    Task<string> GetCompletionAsync(string prompt, string? systemInstruction = null);
}
