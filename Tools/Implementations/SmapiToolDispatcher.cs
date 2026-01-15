using Core.Interfaces;
using Core.Models;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using Microsoft.Xna.Framework;

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

    // Constructor for DI
    public SmapiToolDispatcher(IMonitor monitor, IReflectionHelper? reflection = null)
    {
        _monitor = monitor;
        _reflection = reflection;
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
                "Interact" => PollInteract(task, tickId),
                "WaitUntil" => PollWaitUntil(task, tickId),
                "ShopBuy" => PollShopBuy(task, tickId),
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
                Game1.player.halt();
                Game1.player.controller = null;
            }

            _tasks.Remove(handle);
        }
    }

    #region Tool Implementations

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
                        Game1.player.halt();
                        return (true, ToolResult.Success(task.NodeId, "NavigateTo"));
                    }
                }
                else
                {
                    Game1.player.halt();
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

    #endregion
}
