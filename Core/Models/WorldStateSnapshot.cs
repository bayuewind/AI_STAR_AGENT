namespace Core.Models;

/// <summary>
/// Complete snapshot of a game location's state.
/// Contains a compact grid representation plus detailed entity states.
/// </summary>
public class WorldStateSnapshot
{
    /// <summary>
    /// Location name (e.g., "Farm", "Town", "UndergroundMine10")
    /// </summary>
    public string Location { get; set; } = string.Empty;
    
    /// <summary>
    /// Map dimensions
    /// </summary>
    public int Width { get; set; }
    public int Height { get; set; }
    
    /// <summary>
    /// Game tick when snapshot was taken
    /// </summary>
    public int Tick { get; set; }
    
    /// <summary>
    /// Compact grid: Grid[y][x] = TileKind value
    /// Using byte[][] for memory efficiency
    /// </summary>
    public byte[][]? Grid { get; set; }
    
    /// <summary>
    /// Legend mapping TileKind values to names for AI interpretation
    /// </summary>
    public Dictionary<int, string> Legend { get; set; } = new();
    
    /// <summary>
    /// Detailed entity states (only for tiles with interesting metadata)
    /// </summary>
    public List<TileEntityState> Entities { get; set; } = new();
    
    /// <summary>
    /// Player's current position for reference
    /// </summary>
    public int PlayerX { get; set; }
    public int PlayerY { get; set; }
    
    /// <summary>
    /// Convert to dictionary for JSON serialization in ToolResult
    /// </summary>
    public Dictionary<string, object> ToDict(bool includeGrid = true, bool includeEntities = true)
    {
        var dict = new Dictionary<string, object>
        {
            ["location"] = Location,
            ["width"] = Width,
            ["height"] = Height,
            ["tick"] = Tick,
            ["playerX"] = PlayerX,
            ["playerY"] = PlayerY,
            ["legend"] = Legend
        };
        
        if (includeGrid && Grid != null)
        {
            // Convert byte[][] to int[][] for JSON compatibility
            dict["grid"] = Grid.Select(row => row.Select(b => (int)b).ToArray()).ToArray();
        }
        
        if (includeEntities)
        {
            dict["entities"] = Entities.Select(e => e.ToDict()).ToList();
            dict["entityCount"] = Entities.Count;
        }
        
        return dict;
    }
    
    /// <summary>
    /// Get a summary without the full grid (for logging/debugging)
    /// </summary>
    public Dictionary<string, object> ToSummaryDict()
    {
        var kindCounts = new Dictionary<string, int>();
        if (Grid != null)
        {
            foreach (var row in Grid)
            {
                foreach (var cell in row)
                {
                    var kindName = ((TileKind)cell).ToString();
                    kindCounts[kindName] = kindCounts.GetValueOrDefault(kindName, 0) + 1;
                }
            }
        }
        
        return new Dictionary<string, object>
        {
            ["location"] = Location,
            ["size"] = $"{Width}x{Height}",
            ["tick"] = Tick,
            ["entityCount"] = Entities.Count,
            ["tileCounts"] = kindCounts
        };
    }
}
