using Core.Models;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Object = StardewValley.Object;

namespace Tools.World;

/// <summary>
/// Caches the world state as a grid with O(1) tile lookup.
/// Rebuilds on location change or when invalidated by game events.
/// </summary>
public class WorldStateCache
{
    private readonly IMonitor _monitor;
    private WorldStateSnapshot? _cache;
    private string? _cachedLocation;
    private int _cacheTickId;
    private bool _isInvalidated = true;
    
    // Cache freshness threshold (rebuild if older than this many ticks)
    private const int CACHE_STALE_THRESHOLD = 60; // ~1 second at 60fps
    
    public WorldStateCache(IMonitor monitor)
    {
        _monitor = monitor;
    }
    
    /// <summary>
    /// Check if cache is fresh and valid for current location
    /// </summary>
    public bool IsFresh()
    {
        if (_isInvalidated || _cache == null) return false;
        
        var currentLocation = Game1.currentLocation?.NameOrUniqueName;
        if (currentLocation != _cachedLocation) return false;
        
        // Check if cache is too old
        // Note: We use a simple tick comparison, not actual game ticks
        return true; // For now, only invalidate on explicit events
    }
    
    /// <summary>
    /// Mark cache as needing rebuild
    /// </summary>
    public void Invalidate()
    {
        _isInvalidated = true;
        _monitor.Log("[WorldStateCache] Cache invalidated", LogLevel.Trace);
    }
    
    /// <summary>
    /// Rebuild cache for the given location
    /// </summary>
    public void Rebuild(GameLocation location)
    {
        if (location == null)
        {
            _monitor.Log("[WorldStateCache] Cannot rebuild: location is null", LogLevel.Warn);
            return;
        }
        
        var startTime = DateTime.Now;
        
        int width = location.map.Layers[0].LayerWidth;
        int height = location.map.Layers[0].LayerHeight;
        
        _monitor.Log($"[WorldStateCache] Rebuilding cache for {location.NameOrUniqueName} ({width}x{height})", LogLevel.Debug);
        
        // Initialize grid
        var grid = new byte[height][];
        for (int y = 0; y < height; y++)
        {
            grid[y] = new byte[width];
        }
        
        var entities = new List<TileEntityState>();
        var usedKinds = new HashSet<TileKind>();
        
        // Step 1: Base layer - passability from tile map
        ScanBaseLayer(location, grid, width, height, usedKinds);
        
        // Step 2: Terrain features (trees, grass, crops)
        ScanTerrainFeatures(location, grid, entities, usedKinds);
        
        // Step 3: Objects (stones, machines, forageables)
        ScanObjects(location, grid, entities, usedKinds);
        
        // Step 4: Furniture (beds, TVs, tables, etc.)
        ScanFurniture(location, grid, entities, usedKinds);
        
        // Step 5: Resource clumps (large rocks, stumps)
        ScanResourceClumps(location, grid, entities, usedKinds);
        
        // Step 6: Buildings (if buildable location)
        ScanBuildings(location, grid, entities, usedKinds);
        
        // Step 7: Warps
        ScanWarps(location, grid, usedKinds);
        
        // Build legend
        var legend = usedKinds
            .OrderBy(k => (int)k)
            .ToDictionary(k => (int)k, k => k.ToString());
        
        // Create snapshot
        _cache = new WorldStateSnapshot
        {
            Location = location.NameOrUniqueName,
            Width = width,
            Height = height,
            Tick = Game1.ticks,
            Grid = grid,
            Legend = legend,
            Entities = entities,
            PlayerX = Game1.player.TilePoint.X,
            PlayerY = Game1.player.TilePoint.Y
        };
        
        _cachedLocation = location.NameOrUniqueName;
        _cacheTickId = Game1.ticks;
        _isInvalidated = false;
        
        var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
        _monitor.Log($"[WorldStateCache] Rebuilt in {elapsed:F1}ms: {entities.Count} entities, {usedKinds.Count} tile types", LogLevel.Debug);
    }
    
    /// <summary>
    /// Get the cached snapshot
    /// </summary>
    public WorldStateSnapshot? GetSnapshot()
    {
        return _cache;
    }
    
    /// <summary>
    /// Get tile kind at position (O(1) lookup)
    /// </summary>
    public TileKind GetTileKind(int x, int y)
    {
        if (_cache?.Grid == null) return TileKind.Unknown;
        if (y < 0 || y >= _cache.Height || x < 0 || x >= _cache.Width) return TileKind.Unknown;
        return (TileKind)_cache.Grid[y][x];
    }
    
    /// <summary>
    /// Find all tiles of a specific kind
    /// </summary>
    public List<(int x, int y)> FindTiles(TileKind kind)
    {
        var result = new List<(int, int)>();
        if (_cache?.Grid == null) return result;
        
        byte kindByte = (byte)kind;
        for (int y = 0; y < _cache.Height; y++)
        {
            for (int x = 0; x < _cache.Width; x++)
            {
                if (_cache.Grid[y][x] == kindByte)
                {
                    result.Add((x, y));
                }
            }
        }
        return result;
    }
    
    /// <summary>
    /// Find entities of a specific kind
    /// </summary>
    public List<TileEntityState> FindEntities(TileKind kind)
    {
        if (_cache == null) return new List<TileEntityState>();
        return _cache.Entities.Where(e => e.Kind == kind).ToList();
    }
    
    /// <summary>
    /// Find nearest tile of a specific kind to player
    /// </summary>
    public (int x, int y, int distance)? FindNearest(TileKind kind)
    {
        if (_cache?.Grid == null) return null;
        
        int playerX = Game1.player.TilePoint.X;
        int playerY = Game1.player.TilePoint.Y;
        
        var tiles = FindTiles(kind);
        if (tiles.Count == 0) return null;
        
        var nearest = tiles
            .Select(t => (t.x, t.y, distance: Math.Abs(t.x - playerX) + Math.Abs(t.y - playerY)))
            .OrderBy(t => t.distance)
            .First();
        
        return nearest;
    }
    
    #region Scanning Methods
    
    private void ScanBaseLayer(GameLocation location, byte[][] grid, int width, int height, HashSet<TileKind> usedKinds)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var tilePos = new Vector2(x, y);
                
                // Check water first
                if (location.isWaterTile(x, y))
                {
                    grid[y][x] = (byte)TileKind.Water;
                    usedKinds.Add(TileKind.Water);
                }
                // Check passability 
                else if (location.isTilePassable(new xTile.Dimensions.Location(x, y), Game1.viewport))
                {
                    grid[y][x] = (byte)TileKind.Passable;
                    usedKinds.Add(TileKind.Passable);
                }
                else
                {
                    grid[y][x] = (byte)TileKind.Blocked;
                    usedKinds.Add(TileKind.Blocked);
                }
            }
        }
    }
    
    private void ScanTerrainFeatures(GameLocation location, byte[][] grid, List<TileEntityState> entities, HashSet<TileKind> usedKinds)
    {
        foreach (var kvp in location.terrainFeatures.Pairs)
        {
            var pos = kvp.Key;
            var feature = kvp.Value;
            int x = (int)pos.X;
            int y = (int)pos.Y;
            
            if (y < 0 || y >= grid.Length || x < 0 || x >= grid[0].Length) continue;
            
            TileKind kind = TileKind.Unknown;
            TileEntityState? entity = null;
            
            switch (feature)
            {
                case Tree tree:
                    kind = TileKind.Tree;
                    entity = new TileEntityState(x, y, kind, tree.treeType.Value.ToString(), GetTreeName(tree.treeType.Value))
                    {
                        Meta = new Dictionary<string, object>
                        {
                            ["treeType"] = tree.treeType.Value,
                            ["growthStage"] = tree.growthStage.Value,
                            ["hasSeed"] = tree.hasSeed.Value,
                            ["stump"] = tree.stump.Value,
                            ["tapped"] = tree.tapped.Value
                        }
                    };
                    break;
                    
                case FruitTree fruitTree:
                    kind = TileKind.FruitTree;
                    entity = new TileEntityState(x, y, kind, fruitTree.treeId.Value, $"Fruit Tree ({fruitTree.treeId.Value})")
                    {
                        Meta = new Dictionary<string, object>
                        {
                            ["growthStage"] = fruitTree.growthStage.Value,
                            ["fruitsOnTree"] = fruitTree.fruit.Count,
                            ["daysUntilMature"] = fruitTree.daysUntilMature.Value
                        }
                    };
                    break;
                    
                case Grass grass:
                    kind = TileKind.Grass;
                    // Grass usually doesn't need detailed entity tracking
                    break;
                    
                case HoeDirt hoeDirt:
                    if (hoeDirt.crop != null)
                    {
                        kind = TileKind.HoeDirtCrop;
                        var crop = hoeDirt.crop;
                        entity = new TileEntityState(x, y, kind, crop.indexOfHarvest.Value, crop.netSeedIndex.Value)
                        {
                            Meta = new Dictionary<string, object>
                            {
                                ["phase"] = crop.currentPhase.Value,
                                ["dayOfPhase"] = crop.dayOfCurrentPhase.Value,
                                ["fullyGrown"] = crop.fullyGrown.Value,
                                ["dead"] = crop.dead.Value,
                                ["watered"] = hoeDirt.state.Value == 1
                            }
                        };
                    }
                    else
                    {
                        kind = hoeDirt.state.Value == 1 ? TileKind.HoeDirtWatered : TileKind.HoeDirt;
                    }
                    break;
                    
                case Flooring:
                    kind = TileKind.Flooring;
                    break;
            }
            
            if (kind != TileKind.Unknown)
            {
                grid[y][x] = (byte)kind;
                usedKinds.Add(kind);
                if (entity != null) entities.Add(entity);
            }
        }
        
        // Large terrain features
        if (location.largeTerrainFeatures != null)
        {
            foreach (var ltf in location.largeTerrainFeatures)
            {
                int x = (int)ltf.Tile.X;
                int y = (int)ltf.Tile.Y;
                
                if (y >= 0 && y < grid.Length && x >= 0 && x < grid[0].Length)
                {
                    grid[y][x] = (byte)TileKind.ResourceClump;
                    usedKinds.Add(TileKind.ResourceClump);
                    
                    entities.Add(new TileEntityState(x, y, TileKind.ResourceClump, ltf.GetType().Name, ltf.GetType().Name));
                }
            }
        }
    }
    
    private void ScanObjects(GameLocation location, byte[][] grid, List<TileEntityState> entities, HashSet<TileKind> usedKinds)
    {
        foreach (var kvp in location.Objects.Pairs)
        {
            var pos = kvp.Key;
            var obj = kvp.Value;
            int x = (int)pos.X;
            int y = (int)pos.Y;
            
            if (y < 0 || y >= grid.Length || x < 0 || x >= grid[0].Length) continue;
            
            TileKind kind = ClassifyObject(obj);
            grid[y][x] = (byte)kind;
            usedKinds.Add(kind);
            
            // Create entity with metadata for interesting objects
            var entity = new TileEntityState(x, y, kind, obj.ItemId, obj.DisplayName);
            
            // Add object-specific metadata
            var meta = new Dictionary<string, object>();
            
            if (obj.MinutesUntilReady > 0)
            {
                meta["minutesUntilReady"] = obj.MinutesUntilReady;
            }
            
            if (obj.readyForHarvest.Value)
            {
                meta["readyForHarvest"] = true;
            }
            
            if (obj.heldObject.Value != null)
            {
                meta["heldItem"] = obj.heldObject.Value.DisplayName;
                meta["heldItemId"] = obj.heldObject.Value.ItemId;
            }
            
            if (obj is Chest chest)
            {
                meta["itemCount"] = chest.Items.Count;
            }
            
            if (meta.Count > 0)
            {
                entity.Meta = meta;
            }
            
            entities.Add(entity);
        }
    }
    
    private void ScanFurniture(GameLocation location, byte[][] grid, List<TileEntityState> entities, HashSet<TileKind> usedKinds)
    {
        if (location.furniture == null) return;
        
        foreach (var furniture in location.furniture)
        {
            int x = (int)furniture.TileLocation.X;
            int y = (int)furniture.TileLocation.Y;
            
            if (y < 0 || y >= grid.Length || x < 0 || x >= grid[0].Length) continue;
            
            // Mark tile as furniture
            grid[y][x] = (byte)TileKind.Furniture;
            usedKinds.Add(TileKind.Furniture);
            
            // Create entity with metadata
            var entity = new TileEntityState(x, y, TileKind.Furniture, furniture.ItemId, furniture.DisplayName)
            {
                Meta = new Dictionary<string, object>
                {
                    ["furnitureType"] = furniture.furniture_type.Value,
                    ["rotations"] = furniture.rotations.Value
                }
            };
            
            // Add held object info for furniture that can hold items (like tables)
            if (furniture.heldObject.Value != null)
            {
                entity.Meta["heldItem"] = furniture.heldObject.Value.DisplayName;
            }
            
            entities.Add(entity);
        }
    }
    
    private void ScanResourceClumps(GameLocation location, byte[][] grid, List<TileEntityState> entities, HashSet<TileKind> usedKinds)
    {
        foreach (var clump in location.resourceClumps)
        {
            int startX = (int)clump.Tile.X;
            int startY = (int)clump.Tile.Y;
            int clumpWidth = clump.width.Value;
            int clumpHeight = clump.height.Value;
            
            string clumpType = GetResourceClumpType(clump.parentSheetIndex.Value);
            
            // Mark all tiles covered by this clump
            for (int dy = 0; dy < clumpHeight; dy++)
            {
                for (int dx = 0; dx < clumpWidth; dx++)
                {
                    int x = startX + dx;
                    int y = startY + dy;
                    
                    if (y >= 0 && y < grid.Length && x >= 0 && x < grid[0].Length)
                    {
                        grid[y][x] = (byte)TileKind.ResourceClump;
                    }
                }
            }
            usedKinds.Add(TileKind.ResourceClump);
            
            // Add entity for the clump origin
            entities.Add(new TileEntityState(startX, startY, TileKind.ResourceClump, clump.parentSheetIndex.Value.ToString(), clumpType)
            {
                Meta = new Dictionary<string, object>
                {
                    ["width"] = clumpWidth,
                    ["height"] = clumpHeight,
                    ["health"] = clump.health.Value
                }
            });
        }
    }
    
    private void ScanBuildings(GameLocation location, byte[][] grid, List<TileEntityState> entities, HashSet<TileKind> usedKinds)
    {
        // Buildings are on Farm and similar locations that have the buildings property
        if (location.buildings == null || location.buildings.Count == 0) return;
        
        foreach (var building in location.buildings)
        {
            int startX = building.tileX.Value;
            int startY = building.tileY.Value;
            int bWidth = building.tilesWide.Value;
            int bHeight = building.tilesHigh.Value;
            
            // Mark building footprint
            for (int dy = 0; dy < bHeight; dy++)
            {
                for (int dx = 0; dx < bWidth; dx++)
                {
                    int x = startX + dx;
                    int y = startY + dy;
                    
                    if (y >= 0 && y < grid.Length && x >= 0 && x < grid[0].Length)
                    {
                        grid[y][x] = (byte)TileKind.Building;
                    }
                }
            }
            usedKinds.Add(TileKind.Building);
            
            // Add entity
            var meta = new Dictionary<string, object>
            {
                ["width"] = bWidth,
                ["height"] = bHeight
            };
            
            // Add animal house info if applicable
            if (building.indoors.Value is AnimalHouse animalHouse)
            {
                meta["animalCount"] = animalHouse.animalsThatLiveHere.Count;
            }
            
            entities.Add(new TileEntityState(startX, startY, TileKind.Building, building.buildingType.Value, building.buildingType.Value)
            {
                Meta = meta
            });
        }
    }
    
    private void ScanWarps(GameLocation location, byte[][] grid, HashSet<TileKind> usedKinds)
    {
        foreach (var warp in location.warps)
        {
            int x = warp.X;
            int y = warp.Y;
            
            if (y >= 0 && y < grid.Length && x >= 0 && x < grid[0].Length)
            {
                grid[y][x] = (byte)TileKind.Warp;
                usedKinds.Add(TileKind.Warp);
            }
        }
    }
    
    #endregion
    
    #region Helper Methods
    
    private TileKind ClassifyObject(Object obj)
    {
        // Check for specific object types
        if (obj is Chest) return TileKind.Chest;
        if (obj is Fence) return TileKind.Fence;
        if (obj is Furniture) return TileKind.Furniture;
        
        // Check by name patterns
        string name = obj.Name.ToLower();
        
        if (name.Contains("stone") || obj.ParentSheetIndex == 450) return TileKind.Stone;
        if (name.Contains("twig") || name.Contains("branch")) return TileKind.Twig;
        if (name.Contains("weed")) return TileKind.Weed;
        
        // Ore nodes
        if (obj.ParentSheetIndex >= 751 && obj.ParentSheetIndex <= 765) return TileKind.Ore;
        if (obj.ParentSheetIndex >= 290 && obj.ParentSheetIndex <= 294) return TileKind.Ore;
        
        // Forageables (spawned objects)
        if (obj.IsSpawnedObject) return TileKind.Forageable;
        
        // Machines (craftables that process items)
        if (obj.bigCraftable.Value) return TileKind.Machine;
        
        return TileKind.Blocked; // Default for objects
    }
    
    private string GetTreeName(string treeType)
    {
        return treeType switch
        {
            "1" => "Oak Tree",
            "2" => "Maple Tree",
            "3" => "Pine Tree",
            "6" => "Palm Tree",
            "7" => "Mushroom Tree",
            "8" => "Mahogany Tree",
            _ => $"Tree ({treeType})"
        };
    }
    
    private string GetResourceClumpType(int index)
    {
        return index switch
        {
            600 => "Large Stump",
            602 => "Hollow Log",
            622 => "Meteorite",
            672 => "Large Boulder",
            752 => "Large Stone (Copper)",
            754 => "Large Stone (Iron)",
            756 => "Large Stone (Gold)",
            758 => "Large Stone (Iridium)",
            _ => $"Resource Clump ({index})"
        };
    }
    
    #endregion
}
