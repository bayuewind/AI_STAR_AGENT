using Core.Models;

namespace Memory.MemoryStore;

public class MemoryStore
{
    private readonly List<MemoryEntry> _entries = new();

    public void AddEntry(MemoryEntry entry)
    {
        if (entry == null) throw new ArgumentNullException(nameof(entry));
        _entries.Add(entry);
    }

    public List<MemoryEntry> Query(MemoryQuery query)
    {
        var source = _entries.AsEnumerable();

        if (query.Type != null)
        {
            source = source.Where(e => e.Type == query.Type);
        }

        if (query.RequiredTags != null && query.RequiredTags.Any())
        {
            source = source.Where(e => query.RequiredTags.All(tag => e.Tags.Contains(tag)));
        }

        if (!string.IsNullOrEmpty(query.QueryText))
        {
            source = source.Where(e => e.Content.Contains(query.QueryText, StringComparison.OrdinalIgnoreCase));
        }

        // Vector Similarity Search
        if (query.QueryEmbedding != null)
        {
            var scoredResults = source
                .Where(e => e.Embedding != null)
                .Select(e => new { Entry = e, Score = query.QueryEmbedding.CosineSimilarity(e.Embedding!) })
                .Where(x => x.Score >= query.MinSimilarity)
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Entry.Importance)
                .Take(query.Limit)
                .Select(x => x.Entry)
                .ToList();

            if (scoredResults.Any()) return scoredResults;
        }

        // Fallback to keyword/importance ranking
        return source.OrderByDescending(e => e.Importance)
                     .ThenByDescending(e => e.CreatedAt)
                     .Take(query.Limit)
                     .ToList();
    }

    public void Clear() => _entries.Clear();
}
