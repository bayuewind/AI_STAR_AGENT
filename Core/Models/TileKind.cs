namespace Core.Models;

/// <summary>
/// Represents the type of content at a map tile.
/// Used for efficient grid-based world state representation.
/// </summary>
public enum TileKind : byte
{
    // Base types
    Unknown = 0,
    Passable = 1,
    Blocked = 2,
    Water = 3,
    
    // Terrain Features (10-19)
    Tree = 10,
    FruitTree = 11,
    Grass = 12,
    HoeDirt = 13,
    HoeDirtWatered = 14,
    HoeDirtCrop = 15,
    Flooring = 16,
    
    // Objects (20-29)
    Stone = 20,
    Ore = 21,
    Twig = 22,
    Weed = 23,
    Forageable = 24,
    Machine = 25,
    Chest = 26,
    Fence = 27,
    Furniture = 28,
    
    // Large Objects (30-39)
    ResourceClump = 30,      // Large rock, stump, meteorite
    GiantCrop = 31,
    Building = 32,
    
    // Special (40-49)
    Warp = 40,
    Door = 41,
    Ladder = 42,
    
    // Entities (50-59)
    NPC = 50,
    Monster = 51,
    FarmAnimal = 52
}
