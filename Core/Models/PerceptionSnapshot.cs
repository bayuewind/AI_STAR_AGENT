namespace Core.Models;

public class PerceptionSnapshot
{
    public int TickId { get; set; }
    public int Gold { get; set; }
    public int TileX { get; set; }
    public int TileY { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? LastErrorCode { get; set; }

    // Rich perception data
    public Inventory Inventory { get; set; } = new();
    public List<NPC> NearbyNPCs { get; set; } = new();
    public WorldState World { get; set; } = new();
}
