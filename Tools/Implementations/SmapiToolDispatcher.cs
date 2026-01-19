using Core.Interfaces;
using Core.Models;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using Microsoft.Xna.Framework;
using Tools.Movement;
using Tools.WaterBot;

namespace Tools.Implementations;

public class SmapiToolDispatcher : IToolDispatcher
{
    private class TaskState
    {
        public string Handle { get; set; } = string.Empty;
        public string NodeId { get; set; } = string.Empty;
        public string ToolName { get; set; } = string.Empty;
        public Dictionary<string, object> Args { get; set; } = new();
        public int StartTick { get; set; }
        public int Phase { get; set; } = 0;
        public Dictionary<string, object> StateData { get; set; } = new(); // For storing intermediate state like route plan
        public object? Context { get; set; } // Store tool-specific state (e.g. path controller)
    }

    private readonly Dictionary<string, TaskState> _tasks = new();
    private readonly IMonitor _monitor;
    private readonly IReflectionHelper? _reflection; // Optional, for advanced access if needed
    private readonly MovementController _movementController;
    private readonly ActionHandler _actionHandler;
    private readonly GlobalRoutePlanner _routePlanner;
    private WaterBotController? _waterBot;

    // Constructor for DI
    public SmapiToolDispatcher(IMonitor monitor, string modPath, IReflectionHelper? reflection = null)
    {
        _monitor = monitor;
        _reflection = reflection;
        
        // Initialize movement tools
        TileUtil.SetMonitor(monitor);
        var movementConfig = new MovementConfig(); // Default config for now
        _movementController = new MovementController(monitor, movementConfig);
        _actionHandler = new ActionHandler(monitor);
        _routePlanner = new GlobalRoutePlanner(monitor);
        // Load POI Database from mod assets
        string jsonPath = Path.Combine(modPath, "assets", "Locations.json");
        _routePlanner.LoadLocations(jsonPath);

        MovementPatcher.ApplyPatches(monitor);
    }

    public string Begin(string nodeId, string toolName, Dictionary<string, object> args, int tickId)
    {
        var handle = Guid.NewGuid().ToString();
        var task = new TaskState
        {
            Handle = handle,
            NodeId = nodeId,
            ToolName = toolName,
            Args = args,
            StartTick = tickId,
            Phase = 0
        };

        _tasks[handle] = task;
        _monitor.Log($"[ToolDispatcher] Begin {toolName} (Node: {nodeId})", LogLevel.Debug);

        // Immediate initialization if needed
        switch (toolName)
        {
            case "NavigateTo":
                StartNavigation(task);
                break;
            case "MoveTo":
                StartMoveTo(task);
                break;
            case "MoveToNPC":
                StartMoveToNPC(task);
                break;
            case "WaterCrops":
                StartWaterCrops(task);
                break;
        }

        return handle;
    }

    public (bool ready, ToolResult? result) Poll(string handle, int tickId)
    {
        if (!_tasks.TryGetValue(handle, out var task))
        {
            return (true, ToolResult.Failure("unknown", "unknown", "invalid_handle", "Task not found"));
        }

        try
        {
            return task.ToolName switch
            {
                "NavigateTo" => PollNavigation(task, tickId),
                "MoveTo" => PollMoveTo(task, tickId),
                "MoveToNPC" => PollMoveToNPC(task, tickId),
                "Interact" => PollInteract(task, tickId),
                "WaitUntil" => PollWaitUntil(task, tickId),
                "ShopBuy" => PollShopBuy(task, tickId),
                "WaterCrops" => PollWaterCrops(task, tickId),
                "ConsoleLog" => (true, ToolResult.Success(task.NodeId, task.ToolName)),
                _ => (true, ToolResult.Failure(task.NodeId, task.ToolName, "unknown_tool", $"Tool {task.ToolName} not implemented"))
            };
        }
        catch (Exception ex)
        {
            _monitor.Log($"Error polling tool {task.ToolName}: {ex}", LogLevel.Error);
            return (true, ToolResult.Failure(task.NodeId, task.ToolName, "exception", ex.Message));
        }
    }

    public void Cancel(string handle, int tickId)
    {
        if (_tasks.TryGetValue(handle, out var task))
        {
            _monitor.Log($"[ToolDispatcher] Cancel {task.ToolName}", LogLevel.Info);
            
            // Cleanup logic
            if (task.ToolName == "NavigateTo")
            {
                Game1.player.Halt();
                Game1.player.controller = null;
            }
            else if (task.ToolName == "MoveTo" || task.ToolName == "MoveToNPC")
            {
                _movementController.Stop();
            }
            else if (task.ToolName == "WaterCrops")
            {
                _waterBot?.Stop();
            }

            _tasks.Remove(handle);
        }
    }

    #region Tool Implementations

    private void StartMoveTo(TaskState task)
    {
        if (task.Args.TryGetValue("TileX", out var xObj) && task.Args.TryGetValue("TileY", out var yObj))
        {
            int x = Convert.ToInt32(xObj);
            int y = Convert.ToInt32(yObj);
            _movementController.StartMoveTo(x, y);
        }
    }

    private (bool, ToolResult?) PollMoveTo(TaskState task, int tickId)
    {
        _movementController.Update();
        
        if (_movementController.IsArrived)
        {
            return (true, ToolResult.Success(task.NodeId, "MoveTo"));
        }
        
        // Timeout check (e.g. 30 seconds = 1800 ticks approx)
        if (tickId - task.StartTick > 1800)
        {
             _movementController.Stop();
             return (true, ToolResult.Failure(task.NodeId, "MoveTo", "timeout", "Movement timed out"));
        }

        return (false, null);
    }

    private void StartMoveToNPC(TaskState task)
    {
        if (task.Args.TryGetValue("NPCName", out var nameObj))
        {
            string npcName = nameObj.ToString()!;
            _movementController.StartMoveToNPC(npcName);
        }
    }

    private (bool, ToolResult?) PollMoveToNPC(TaskState task, int tickId)
    {
        _movementController.Update();

        if (_movementController.IsArrived)
        {
            return (true, ToolResult.Success(task.NodeId, "MoveToNPC"));
        }

        if (tickId - task.StartTick > 1800)
        {
             _movementController.Stop();
             return (true, ToolResult.Failure(task.NodeId, "MoveToNPC", "timeout", "Movement timed out"));
        }

        return (false, null);
    }

    private void StartNavigation(TaskState task)
    {
        if (!task.Args.TryGetValue("Location", out var locNameObj)) return;
        string targetLocName = locNameObj.ToString()!;
        
        _monitor.Log($"[NavigateTo] Request to go to: {targetLocName}", LogLevel.Info);
        
        // Use GlobalRoutePlanner to resolve the location
        var dest = _routePlanner.GetDestination(targetLocName);
        
        if (dest.mapName == null)
        {
            _monitor.Log($"[NavigateTo] Unknown location: {targetLocName}", LogLevel.Warn);
            // Fallback: try to treat input as a raw map name
            dest = (targetLocName, null);
        }

        task.StateData["TargetMap"] = dest.mapName;
        if (dest.tile.HasValue)
        {
            task.StateData["TargetX"] = (int)dest.tile.Value.X;
            task.StateData["TargetY"] = (int)dest.tile.Value.Y;
        }
        else
        {
            task.StateData["UseDefaultSpawn"] = true;
        }
        
        task.Phase = 0; // 0: Planning, 1: Moving, 2: Warping
    }

    private (bool, ToolResult?) PollNavigation(TaskState task, int tickId)
    {
        // Ensure state data exists
        if (!task.StateData.ContainsKey("TargetMap"))
        {
            return (true, ToolResult.Failure(task.NodeId, "NavigateTo", "no_target", "Navigation target not set"));
        }

        string targetMap = (string)task.StateData["TargetMap"];
        string currentMap = Game1.currentLocation?.NameOrUniqueName ?? "";

        _monitor.Log($"[NavigateTo] Poll - Phase:{task.Phase}, Current:{currentMap}, Target:{targetMap}", LogLevel.Trace);

        // Phase 0: Planning for current segment
        if (task.Phase == 0)
        {
            if (currentMap == targetMap)
            {
                // We are in the correct map, move to specific tile if provided
                if (task.StateData.ContainsKey("TargetX") && task.StateData.ContainsKey("TargetY"))
                {
                    int x = (int)task.StateData["TargetX"];
                    int y = (int)task.StateData["TargetY"];
                    _movementController.StartMoveTo(x * 64 + 32, y * 64 + 32); // Convert tile to pixel
                    _monitor.Log($"[NavigateTo] Final leg: Moving to tile ({x}, {y}) in {currentMap}", LogLevel.Info);
                    task.Phase = 1; // Moving to final destination
                }
                else
                {
                    // No specific tile, we are just 'in the right map'
                    _monitor.Log($"[NavigateTo] Arrived at map: {targetMap}", LogLevel.Info);
                    return (true, ToolResult.Success(task.NodeId, "NavigateTo"));
                }
            }
            else
            {
                // We need to route to next map via warp
                var nextWarp = _routePlanner.FindNextWarp(currentMap, targetMap);
                if (nextWarp == null)
                {
                    return (true, ToolResult.Failure(task.NodeId, "NavigateTo", "no_route", $"Cannot find route from {currentMap} to {targetMap}"));
                }
                
                // Store current map to detect warp later
                task.StateData["WarpFromMap"] = currentMap;
                task.StateData["WarpToMap"] = nextWarp.TargetName;
                
                // Move to the warp tile
                _movementController.StartMoveTo(nextWarp.X * 64 + 32, nextWarp.Y * 64 + 32);
                _monitor.Log($"[NavigateTo] Segment: Moving to Warp at ({nextWarp.X}, {nextWarp.Y}) -> {nextWarp.TargetName}", LogLevel.Info);
                task.Phase = 2; // Moving to Warp
            }
        }

        // Phase 1: Moving to Final Destination (within target map)
        if (task.Phase == 1)
        {
            if (_movementController.IsArrived)
            {
                _monitor.Log($"[NavigateTo] Arrived at final destination in {targetMap}", LogLevel.Info);
                return (true, ToolResult.Success(task.NodeId, "NavigateTo"));
            }
            
            // Timeout check (30 seconds)
            if (tickId - task.StartTick > 1800)
            {
                return (true, ToolResult.Failure(task.NodeId, "NavigateTo", "timeout", "Movement timed out"));
            }
        }
        
        // Phase 2: Moving to Warp tile
        if (task.Phase == 2)
        {
            if (_movementController.IsArrived)
            {
                // We are at warp tile. Wait for game to switch map.
                task.Phase = 3; 
                task.StateData["WarpWaitStart"] = tickId;
                _monitor.Log("[NavigateTo] Reached warp tile, waiting for map switch...", LogLevel.Debug);
            }
            
            // Timeout check (30 seconds)
            if (tickId - task.StartTick > 1800)
            {
                return (true, ToolResult.Failure(task.NodeId, "NavigateTo", "timeout", "Movement to warp timed out"));
            }
        }
        
        // Phase 3: Waiting for Map Switch after walking into warp
        if (task.Phase == 3)
        {
            string warpFromMap = (string)task.StateData["WarpFromMap"];
            
            if (Game1.currentLocation?.NameOrUniqueName != warpFromMap)
            {
                // Map changed!
                _monitor.Log($"[NavigateTo] Map changed to {Game1.currentLocation?.NameOrUniqueName}. Re-planning.", LogLevel.Info);
                task.Phase = 0; // Go back to planning for new map
                task.StartTick = tickId; // Reset timer
                _movementController.Stop(); // Reset movement state
            }
            else
            {
                // Still on the same map - timeout check
                int warpWaitStart = (int)task.StateData["WarpWaitStart"];
                if (tickId - warpWaitStart > 300) // 5 seconds
                {
                    return (true, ToolResult.Failure(task.NodeId, "NavigateTo", "warp_timeout", "Failed to warp within timeout"));
                }
            }
        }

        return (false, null);
    }

    private (bool, ToolResult?) PollInteract(TaskState task, int tickId)
    {
        // Use ActionHandler to perform 'physical' interaction check
        // We set target to the tile in front of the player (GrabTile)
        // because the bot usually faces the target after MoveTo.
        
        Vector2 grabTile = Game1.player.GetGrabTile();
        _actionHandler.updateTarget(Game1.player.GetToolLocation(grabTile));
        
        bool success = _actionHandler.tryDoAction();
        
        // Also check if a menu opened, which counts as success
        if (Game1.activeClickableMenu != null)
        {
            success = true;
        }

        if (success)
        {
            return (true, ToolResult.Success(task.NodeId, "Interact"));
        }
        
        // Retry a few times if needed, or just return success if we assume action was "attempted"
        // But for "Open Door" or "Open Shop", we really want it to happen.
        // Let's try for a few ticks.
        if (tickId - task.StartTick > 60) // 1 second timeout for simple click
        {
             return (true, ToolResult.Failure(task.NodeId, "Interact", "timeout", "Interact failed to trigger anything"));
        }

        return (false, null);
    }

    private (bool, ToolResult?) PollWaitUntil(TaskState task, int tickId)
    {
        if (task.Args.TryGetValue("Time", out var timeObj))
        {
            string targetTimeStr = timeObj.ToString()!;
            // Parse "09:00" -> 900
            if (int.TryParse(targetTimeStr.Replace(":", ""), out int targetTime))
            {
                if (Game1.timeOfDay >= targetTime)
                    return (true, ToolResult.Success(task.NodeId, "WaitUntil"));
            }
        }
        
        // Fallback: wait for duration if provided
        return (false, null);
    }

    private (bool, ToolResult?) PollShopBuy(TaskState task, int tickId)
    {
        if (Game1.activeClickableMenu is not ShopMenu shopMenu)
        {
            return (true, ToolResult.Failure(task.NodeId, "ShopBuy", "menu_not_open", "Shop menu is not currently open"));
        }

        if (!task.Args.TryGetValue("ItemId", out var itemIdObj)) 
            return (true, ToolResult.Failure(task.NodeId, "ShopBuy", "missing_arg", "ItemId required"));
            
        string itemId = itemIdObj.ToString()!;
        int amount = task.Args.ContainsKey("Amount") ? Convert.ToInt32(task.Args["Amount"]) : 1;
        
        // Logic B: Direct Data Transaction
        ISalable? targetItem = null;
        int price = 0;

        foreach(var kvp in shopMenu.itemPriceAndStock)
        {
            ISalable item = kvp.Key;
            var stockInfo = kvp.Value; // ItemStockInformation
            
            // Try to match by ID or Name
            // Note: ISalable strictly doesn't always have ItemId in interface depending on version
            // Cast to Item if possible
            string? currentId = (item as StardewValley.Item)?.ItemId ?? item.Name;
            
            if (currentId == itemId || item.Name == itemId || item.DisplayName == itemId) 
            {
                targetItem = item;
                price = stockInfo.Price;
                break;
            }
        }

        if (targetItem == null)
        {
             return (true, ToolResult.Failure(task.NodeId, "ShopBuy", "item_not_found", $"Item '{itemId}' not found in shop"));
        }

        int totalCost = price * amount;
        if (Game1.player.Money < totalCost)
        {
            return (true, ToolResult.Failure(task.NodeId, "ShopBuy", "insufficient_gold", $"Need {totalCost}g but have {Game1.player.Money}g"));
        }

        // Execute Transaction
        Game1.player.Money -= totalCost;
        
        for(int i=0; i<amount; i++)
        {
             ISalable boughtItem = targetItem.GetSalableInstance();
             if (boughtItem is StardewValley.Item item)
             {
                 Game1.player.addItemByMenuIfNecessary(item);
             }
        }
        
        Game1.playSound("purchaseClick");
        _monitor.Log($"[ShopBuy] Bought {amount}x {targetItem.DisplayName} for {totalCost}g", LogLevel.Info);

        return (true, ToolResult.Success(task.NodeId, "ShopBuy"));
    }

    private void StartWaterCrops(TaskState task)
    {
        // Parse optional config from args
        var config = new WaterBotConfig();
        
        if (task.Args.TryGetValue("UseSmallGrouping", out var smallGroupObj))
        {
            config.UseSmallGrouping = Convert.ToBoolean(smallGroupObj);
        }
        if (task.Args.TryGetValue("RefillOnFinish", out var refillObj))
        {
            config.RefillOnFinish = Convert.ToBoolean(refillObj);
        }
        if (task.Args.TryGetValue("RefillIfLower", out var refillLowerObj))
        {
            config.RefillIfLower = Convert.ToInt32(refillLowerObj);
        }
        if (task.Args.TryGetValue("RedoPathOnRefill", out var redoPathObj))
        {
            config.RedoPathOnRefill = Convert.ToBoolean(redoPathObj);
        }

        _waterBot = new WaterBotController(_monitor, config);
        bool started = _waterBot.Start();
        
        if (!started && _waterBot.Status == WaterBotStatus.Error)
        {
            _monitor.Log($"[WaterCrops] Failed to start: {_waterBot.StatusMessage}", LogLevel.Error);
        }
    }

    private (bool, ToolResult?) PollWaterCrops(TaskState task, int tickId)
    {
        if (_waterBot == null)
        {
            return (true, ToolResult.Failure(task.NodeId, "WaterCrops", "not_initialized", "WaterBot not initialized"));
        }

        var result = _waterBot.GetResult();

        // Check for completion or error states
        if (result.IsCompleted || !result.IsActive)
        {
            var data = new Dictionary<string, object>
            {
                { "status", result.Status.ToString() },
                { "message", result.StatusMessage },
                { "cropsWatered", result.CropsWatered },
                { "totalCrops", result.TotalCrops }
            };

            switch (result.Status)
            {
                case WaterBotStatus.Completed:
                    return (true, ToolResult.Success(task.NodeId, "WaterCrops", data));
                
                case WaterBotStatus.Stopped:
                    return (true, ToolResult.Failure(task.NodeId, "WaterCrops", "stopped", result.StatusMessage, data));
                
                case WaterBotStatus.Exhausted:
                    return (true, ToolResult.Failure(task.NodeId, "WaterCrops", "exhausted", result.StatusMessage, data));
                
                case WaterBotStatus.NoWater:
                    return (true, ToolResult.Failure(task.NodeId, "WaterCrops", "no_water", result.StatusMessage, data));
                
                case WaterBotStatus.Error:
                    return (true, ToolResult.Failure(task.NodeId, "WaterCrops", "error", result.StatusMessage, data));
                
                default:
                    return (true, ToolResult.Success(task.NodeId, "WaterCrops", data));
            }
        }

        // Still in progress - timeout check (5 minutes = 18000 ticks approx at 60fps)
        if (tickId - task.StartTick > 18000)
        {
            _waterBot.Stop();
            return (true, ToolResult.Failure(task.NodeId, "WaterCrops", "timeout", "Watering timed out after 5 minutes"));
        }

        return (false, null);
    }

    #endregion
}
