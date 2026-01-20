namespace Core.Models;

/// <summary>
/// Represents detailed state of an entity at a specific tile.
/// Contains metadata beyond just the TileKind.
/// </summary>
public class TileEntityState
{
    public int X { get; set; }
    public int Y { get; set; }
    public TileKind Kind { get; set; }
    
    /// <summary>
    /// ItemId or type identifier (e.g., "Coal", "Oak Tree", "Furnace")
    /// </summary>
    public string? Id { get; set; }
    
    /// <summary>
    /// Display name for human readability
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// Type-specific metadata.
    /// Examples:
    /// - Tree: { "treeType": 1, "growthStage": 5, "hasSeed": true }
    /// - Machine: { "isReady": true, "minutesRemaining": 0, "heldItem": "Gold Bar" }
    /// - Crop: { "cropId": "Parsnip", "phase": 4, "fullyGrown": true }
    /// - Ore: { "oreType": "Copper", "health": 3 }
    /// </summary>
    public Dictionary<string, object>? Meta { get; set; }
    
    public TileEntityState() { }
    
    public TileEntityState(int x, int y, TileKind kind, string? id = null, string? name = null)
    {
        X = x;
        Y = y;
        Kind = kind;
        Id = id;
        Name = name;
    }
    
    /// <summary>
    /// Convert to dictionary for JSON serialization in ToolResult
    /// </summary>
    public Dictionary<string, object> ToDict()
    {
        var dict = new Dictionary<string, object>
        {
            ["x"] = X,
            ["y"] = Y,
            ["kind"] = (int)Kind,
            ["kindName"] = Kind.ToString()
        };
        
        if (Id != null) dict["id"] = Id;
        if (Name != null) dict["name"] = Name;
        if (Meta != null) dict["meta"] = Meta;
        
        return dict;
    }
}
