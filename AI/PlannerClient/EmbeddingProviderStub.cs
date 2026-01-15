using Core.Interfaces;
using Core.Models;

namespace AI.PlannerClient;

/// <summary>
/// EmbeddingProviderStub - Simulates vector embedding generation.
/// In a real system, this would call OpenAI/Gemini embedding endpoints.
/// </summary>
public class EmbeddingProviderStub : IEmbeddingProvider
{
    public async Task<VectorEmbedding> GetEmbeddingAsync(string text)
    {
        await Task.Delay(20); // Simulate network latency

        // For demo purposes, we generate a deterministic "pseudo-vector" based on the string content.
        // This allows us to test similarity without a real model.
        float[] values = new float[128]; // 128-dimensional embedding
        
        // Very simple hashing to simulate semantic meaning
        int seed = text.GetHashCode();
        var random = new Random(seed);
        
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = (float)random.NextDouble();
        }

        // Normalize the vector (simplifies cosine similarity)
        float sumSq = values.Sum(v => v * v);
        float norm = (float)Math.Sqrt(sumSq);
        for (int i = 0; i < values.Length; i++) values[i] /= norm;

        return new VectorEmbedding { Values = values };
    }
}
