using Core.Models;

namespace Core.Interfaces;

/// <summary>
/// IEmbeddingProvider - Interface for generating vector embeddings from text.
/// Used for semantic memory retrieval.
/// </summary>
public interface IEmbeddingProvider
{
    /// <summary>
    /// Generates a vector embedding for the given text.
    /// </summary>
    Task<VectorEmbedding> GetEmbeddingAsync(string text);
}
