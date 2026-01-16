using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using Newtonsoft.Json;

namespace Tools.Movement
{
    public class GlobalRoutePlanner
    {
        private readonly IMonitor _monitor;
        private Dictionary<string, LocationInfo> _poiDatabase = new();
        
        // Internal class for JSON deserialization
        public class LocationInfo
        {
            public string Region { get; set; } = string.Empty;
            public int X { get; set; }
            public int Y { get; set; }
            public string? Condition { get; set; }
        }

        public GlobalRoutePlanner(IMonitor monitor)
        {
            _monitor = monitor;
        }

        public void LoadLocations(string jsonPath)
        {
            try
            {
                if (!File.Exists(jsonPath))
                {
                    _monitor.Log($"[GlobalRoutePlanner] Locations.json not found at: {jsonPath}", LogLevel.Warn);
                    return;
                }

                string jsonContent = File.ReadAllText(jsonPath);
                var data = JsonConvert.DeserializeObject<Dictionary<string, LocationInfo>>(jsonContent);

                if (data != null)
                {
                    _poiDatabase = data;
                    _monitor.Log($"[GlobalRoutePlanner] Loaded {_poiDatabase.Count} POIs from json.", LogLevel.Info);
                }
            }
            catch (Exception ex)
            {
                _monitor.Log($"[GlobalRoutePlanner] Failed to load locations: {ex.Message}", LogLevel.Error);
            }
        }

        public (string? mapName, Vector2? tile) GetDestination(string locationKey)
        {
            // Try to find in POI database first (e.g. "PierreStore")
            // Keys in json are like "Town/PierreStore" or just Region/Name
            // We search for partial match if exact match fails
            
            LocationInfo? info = null;
            
            // 1. Check exact key (rarely used by user input)
            if (_poiDatabase.ContainsKey(locationKey)) 
                info = _poiDatabase[locationKey];
            
            // 2. Check "Region/Name" where Name == locationKey
            if (info == null)
            {
                foreach(var kvp in _poiDatabase)
                {
                    if (kvp.Key.EndsWith($"/{locationKey}", StringComparison.OrdinalIgnoreCase))
                    {
                        info = kvp.Value;
                        break;
                    }
                }
            }
            
            // 3. Check if key itself describes a map (e.g. "Town")
            if (info == null && Game1.getLocationFromName(locationKey) != null)
            {
                return (locationKey, null); // Just go to the map, no specific tile
            }

            if (info != null)
            {
                return (info.Region, new Vector2(info.X, info.Y));
            }

            return (null, null);
        }

        public Warp? FindNextWarp(string currentMapName, string targetMapName)
        {
            if (currentMapName == targetMapName) return null;

            // BFS to find the route of maps
            Queue<string> queue = new Queue<string>();
            queue.Enqueue(currentMapName);
            
            Dictionary<string, string> cameFrom = new Dictionary<string, string>();
            cameFrom[currentMapName] = null!; // Start node

            // We build the graph dynamically from Game1.locations
            // But Game1.locations is a list, let's make a lookup for performance if needed
            // For now, iterating is fine as map count is < 100 usually.

            while (queue.Count > 0)
            {
                string current = queue.Dequeue();

                if (current == targetMapName)
                {
                    // Found path! Trace back to find the first step (the warp in currentMapName)
                    string step = current;
                    while (cameFrom[step] != currentMapName)
                    {
                        step = cameFrom[step];
                    }
                    // 'step' is the map we need to go to directly from 'currentMapName'
                    return GetWarpTo(currentMapName, step);
                }

                // Get neighbors
                GameLocation? loc = Game1.getLocationFromName(current);
                if (loc == null) continue;

                foreach (var warp in loc.warps)
                {
                    string neighbor = warp.TargetName;
                    if (!cameFrom.ContainsKey(neighbor))
                    {
                        cameFrom[neighbor] = current;
                        queue.Enqueue(neighbor);
                    }
                }
            }
            
            return null; // No route found
        }

        private Warp? GetWarpTo(string fromMap, string toMap)
        {
            GameLocation loc = Game1.getLocationFromName(fromMap);
            if (loc == null) return null;

            foreach (var warp in loc.warps)
            {
                if (warp.TargetName == toMap)
                {
                    return warp;
                }
            }
            return null;
        }
    }
}
