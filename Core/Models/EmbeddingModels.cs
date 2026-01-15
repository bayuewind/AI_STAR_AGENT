namespace Core.Models;

public class VectorEmbedding
{
    public float[] Values { get; set; } = Array.Empty<float>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public double CosineSimilarity(VectorEmbedding other)
    {
        if (Values.Length != other.Values.Length) return 0;
        
        float dotProduct = 0;
        float normA = 0;
        float normB = 0;
        
        for (int i = 0; i < Values.Length; i++)
        {
            dotProduct += Values[i] * other.Values[i];
            normA += Values[i] * Values[i];
            normB += other.Values[i] * other.Values[i];
        }
        
        if (normA == 0 || normB == 0) return 0;
        return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}
