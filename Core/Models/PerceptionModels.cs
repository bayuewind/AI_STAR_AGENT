namespace Core.Models;

public class Item
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Stack { get; set; }
    public int MaxStack { get; set; } = 999;
    public string Category { get; set; } = string.Empty;
    public int Price { get; set; }
}

public class Inventory
{
    public List<Item> Items { get; set; } = new();
    public int MaxSlots { get; set; } = 12;

    public int GetItemCount(string itemId) => Items.Where(i => i.Id == itemId).Sum(i => i.Stack);
    public bool HasSpace() => Items.Count < MaxSlots;
    public bool HasItem(string itemId, int count = 1) => GetItemCount(itemId) >= count;
}

public class NPC
{
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int TileX { get; set; }
    public int TileY { get; set; }
    public bool IsVisible { get; set; } = true;
    public int FriendshipLevel { get; set; }
    public bool TalkedToday { get; set; }
    public int GiftsGivenThisWeek { get; set; }
}

public class NPCPreference
{
    public string NPCName { get; set; } = string.Empty;
    public List<string> LovedItems { get; set; } = new();
    public List<string> LikedItems { get; set; } = new();
    public List<string> DislikedItems { get; set; } = new();
}

public class MapObject
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // e.g., Tree, Stone, Crop, Machine
    public int TileX { get; set; }
    public int TileY { get; set; }
    public bool IsPassable { get; set; }
}

public class WorldState
{
    public string TimeOfDay { get; set; } = "06:00";
    public string Season { get; set; } = "Spring";
    public int DayOfMonth { get; set; } = 1;
    public string Weather { get; set; } = "Sun";
    public bool IsFestivalDay { get; set; }
}
