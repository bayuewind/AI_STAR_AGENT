// These stubs are only used when the actual Stardew Valley / SMAPI assemblies are not available.
// Projects that reference actual game assemblies should define the SMAPI_AVAILABLE symbol.
#if !SMAPI_AVAILABLE

namespace StardewValley
{
    public class Game1
    {
        public static Farmer player { get; set; } = new();
        public static GameLocation currentLocation { get; set; } = new();
        public static int timeOfDay { get; set; }
        public static int dayOfMonth { get; set; }
        public static int year { get; set; }
        public static string currentSeason { get; set; } = "spring";
        public static bool isRaining { get; set; }
        public static bool isSnowing { get; set; }
        public static bool isLightning { get; set; }
        public static StardewValley.Menus.IClickableMenu? activeClickableMenu { get; set; }
    }

    public class Farmer
    {
        public int Money { get; set; }
        public float Stamina { get; set; }
        public float MaxStamina { get; set; }
        public Microsoft.Xna.Framework.Point TilePoint { get; set; }
        public Microsoft.Xna.Framework.Rectangle GetBoundingBox() => new();
        public IList<Item> Items { get; set; } = new List<Item>();
        public int MaxItems { get; set; }
        
        public int getFriendshipLevelForNPC(string name) => 0;
        public bool hasPlayerTalkedToNPC(string name) => false;
        public Dictionary<string, List<Item>> giftedItems { get; set; } = new();

        // Pathfinding Stubs
        public object? controller { get; set; } // PathFindController
        public void setMoving(byte direction) {}
        public void halt() {}
    }

    public class GameLocation
    {
        public string NameOrUniqueName { get; set; } = "Farm";
        public bool IsOutdoors { get; set; }
        public IList<NPC> characters { get; set; } = new List<NPC>();
        public bool isTileOnMap(int x, int y) => true;
    }

    public class Item
    {
        public string ItemId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int Stack { get; set; }
        public int Category { get; set; }
        public int salePrice() => 0;
    }

    public class NPC
    {
        public string Name { get; set; } = string.Empty;
        public Microsoft.Xna.Framework.Point TilePoint { get; set; }
    }
}

namespace StardewValley.Menus
{
    public interface IClickableMenu {}
    public class ShopMenu : IClickableMenu {}
    public class DialogueBox : IClickableMenu {}
}

namespace StardewModdingAPI
{
    public interface IMonitor 
    {
        void Log(string msg, LogLevel level);
    }
    public interface IReflectionHelper 
    {
        // Minimal stub for compilation
    }
    public enum LogLevel { Trace, Debug, Info, Warn, Error, Alert }
    public static class Context 
    { 
        public static bool IsWorldReady { get; set; } = true; 
    }
    public class Utility
    {
        public static bool isFestivalDay(int day, string season) => false;
    }
}

namespace Microsoft.Xna.Framework
{
    public struct Point { public int X; public int Y; }
    public struct Rectangle { public int X; public int Y; public int Width; public int Height; }
}

#endif
