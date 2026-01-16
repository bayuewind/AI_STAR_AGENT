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
        public object? Context { get; set; } // Store tool-specific state (e.g. path controller)
    }

    private readonly Dictionary<string, TaskState> _tasks = new();
    private readonly IMonitor _monitor;
    private readonly IReflectionHelper? _reflection; // Optional, for advanced access if needed
    private readonly MovementController _movementController;
    private WaterBotController? _waterBot;

    // Constructor for DI
    public SmapiToolDispatcher(IMonitor monitor, IReflectionHelper? reflection = null)
    {
        _monitor = monitor;
        _reflection = reflection;
        
        // Initialize movement tools
        TileUtil.SetMonitor(monitor);
        var movementConfig = new MovementConfig(); // Default config for now
        _movementController = new MovementController(monitor, movementConfig);
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
        
        // Pathfinding initialization logic would be placed here
    }

    private (bool, ToolResult?) PollNavigation(TaskState task, int tickId)
    {
        if (task.Args.TryGetValue("Location", out var locNameObj))
        {
            string targetLoc = locNameObj.ToString()!;
            if (Game1.currentLocation?.NameOrUniqueName == targetLoc)
            {
                if (task.Args.ContainsKey("TileX") && task.Args.ContainsKey("TileY"))
                {
                    int tx = Convert.ToInt32(task.Args["TileX"]);
                    int ty = Convert.ToInt32(task.Args["TileY"]);
                    Point currentTile = Game1.player.TilePoint;
                    
                    if (Math.Abs(currentTile.X - tx) <= 1 && Math.Abs(currentTile.Y - ty) <= 1)
                    {
                        Game1.player.Halt();
                        return (true, ToolResult.Success(task.NodeId, "NavigateTo"));
                    }
                }
                else
                {
                    Game1.player.Halt();
                    return (true, ToolResult.Success(task.NodeId, "NavigateTo"));
                }
            }
        }

        // Return incomplete status while navigation is in progress
        return (false, null);
    }

    private (bool, ToolResult?) PollInteract(TaskState task, int tickId)
    {
        if (Game1.activeClickableMenu != null)
        {
            // Menu is open, interaction considered complete
        }

        if (task.Phase == 0)
        {
            task.Phase++;
            return (false, null);
        }

        return (true, ToolResult.Success(task.NodeId, "Interact"));
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

        int price = 100;
        if (Game1.player.Money < price * amount)
        {
            return (true, ToolResult.Failure(task.NodeId, "ShopBuy", "insufficient_gold", "Not enough money"));
        }

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
