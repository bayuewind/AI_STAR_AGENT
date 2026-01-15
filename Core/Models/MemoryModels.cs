namespace Core.Models;

public class MemoryEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = "fact";
    public List<string> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int Importance { get; set; } = 1;
    public VectorEmbedding? Embedding { get; set; }
}

public class MemoryQuery
{
    public string? QueryText { get; set; }
    public List<string>? RequiredTags { get; set; }
    public string? Type { get; set; }
    public int Limit { get; set; } = 10;
    public VectorEmbedding? QueryEmbedding { get; set; }
    public double MinSimilarity { get; set; } = 0.7;
}
