using Core.Interfaces;
using Core.Models;

namespace Tools.Implementations;

public class ToolDispatcherStub : IToolDispatcher
{
    private class TaskInfo
    {
        public string NodeId { get; set; } = string.Empty;
        public string ToolName { get; set; } = string.Empty;
        public int StartTick { get; set; }
        public int FinishTick { get; set; }
        public ToolResult? PlannedResult { get; set; }
    }

    private class ToolConfig
    {
        public bool Ok { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorDetail { get; set; }
    }

    private readonly Dictionary<string, TaskInfo> _pendingTasks = new();
    private readonly Dictionary<string, ToolResult> _results = new();
    private readonly HashSet<string> _canceled = new();
    private readonly Dictionary<string, ToolConfig> _toolConfigs = new();

    public void ConfigureToolResult(string toolName, bool ok, string? errorCode = null, string? errorDetail = null)
    {
        _toolConfigs[toolName] = new ToolConfig { Ok = ok, ErrorCode = errorCode, ErrorDetail = errorDetail };
    }

    public void ResetToolConfig(string? toolName = null)
    {
        if (toolName == null) _toolConfigs.Clear();
        else _toolConfigs.Remove(toolName);
    }

    private int GetToolDelayTicks(string toolName)
    {
        return toolName switch
        {
            "NavigateTo" => 60,
            "ShopBuy" => 10,
            _ => 5
        };
    }

    private ToolResult CreateToolResult(string nodeId, string toolName, bool ok, int tickId, Dictionary<string, object> args, string? errorCode = null, string? errorDetail = null)
    {
        var telemetry = new Dictionary<string, object>
        {
            ["tick"] = tickId,
            ["node_id"] = nodeId,
            ["tool"] = toolName
        };

        foreach (var kvp in args)
        {
            telemetry[kvp.Key] = kvp.Value;
        }

        var result = new ToolResult
        {
            ActionNodeId = nodeId,
            Tool = toolName,
            Ok = ok,
            Error = ok ? null : new ErrorInfo
            {
                Code = errorCode ?? "stub_error",
                Detail = errorDetail ?? "Stub error"
            },
            Telemetry = telemetry
        };

        // Add Mock Deltas for specific tools
        if (ok)
        {
            var delta = new ActionDelta();
            bool hasDelta = false;

            if (toolName == "ShopBuy" && args.TryGetValue("ItemId", out var itemId) && args.TryGetValue("Amount", out var amount))
            {
                delta.ItemsChanged.Add(new ItemDelta { ItemId = itemId.ToString()!, CountChanged = Convert.ToInt32(amount) });
                delta.GoldChanged = -100;
                hasDelta = true;
            }
            else if (toolName == "NavigateTo" && args.TryGetValue("Location", out var loc))
            {
                delta.LocationChanged = loc.ToString();
                hasDelta = true;
            }
            else if (toolName == "DumpItems" && args.TryGetValue("Count", out var count))
            {
                delta.ItemsChanged.Add(new ItemDelta { ItemId = "Unknown", CountChanged = -Convert.ToInt32(count) });
                hasDelta = true;
            }

            else if (toolName == "TalkTo" && args.TryGetValue("NPCName", out var npcTalk))
            {
                delta.ItemsChanged.Add(new ItemDelta { ItemId = $"Talk_{npcTalk}", CountChanged = 1 });
                hasDelta = true;
            }
            else if (toolName == "GiveGift" && args.TryGetValue("NPCName", out var npcGift) && args.TryGetValue("ItemId", out var giftId))
            {
                delta.ItemsChanged.Add(new ItemDelta { ItemId = giftId.ToString()!, CountChanged = -1 });
                hasDelta = true;
            }

            if (hasDelta) result.Delta = delta;
        }

        return result;
    }

    public string Begin(string nodeId, string toolName, Dictionary<string, object> args, int tickId)
    {
        var handle = Guid.NewGuid().ToString();
        var delayTicks = GetToolDelayTicks(toolName);
        
        // Special logic for WaitUntil: delay is determined by args
        if (toolName == "WaitUntil" && args.TryGetValue("Time", out var timeObj))
        {
            // Simplified: Wait for 30 ticks for the demo purposes
            delayTicks = 30; 
        }

        ToolResult? plannedResult;
        if (_toolConfigs.TryGetValue(toolName, out var config))
        {
            plannedResult = CreateToolResult(nodeId, toolName, config.Ok, tickId + delayTicks, args, config.ErrorCode, config.ErrorDetail);
        }
        else
        {
            plannedResult = CreateToolResult(nodeId, toolName, ok: true, tickId + delayTicks, args);
        }

        _pendingTasks[handle] = new TaskInfo
        {
            NodeId = nodeId,
            ToolName = toolName,
            StartTick = tickId,
            FinishTick = tickId + delayTicks,
            PlannedResult = plannedResult
        };

        return handle;
    }

    public (bool ready, ToolResult? result) Poll(string handle, int tickId)
    {
        if (_canceled.Contains(handle))
        {
            return (true, CreateToolResult("unknown", "unknown", ok: false, tickId, new Dictionary<string, object>(), "canceled", "Tool execution was canceled"));
        }

        if (_results.TryGetValue(handle, out var existingResult)) return (true, existingResult);

        if (!_pendingTasks.TryGetValue(handle, out var taskInfo))
        {
            return (true, CreateToolResult("unknown", "unknown", ok: false, tickId, new Dictionary<string, object>(), "invalid_handle", "Handle not found"));
        }

        if (tickId < taskInfo.FinishTick) return (false, null);

        var result = taskInfo.PlannedResult ?? CreateToolResult(taskInfo.NodeId, taskInfo.ToolName, ok: true, tickId, new Dictionary<string, object>());
        _results[handle] = result;
        _pendingTasks.Remove(handle);
        return (true, result);
    }

    public void Cancel(string handle, int tickId)
    {
        _canceled.Add(handle);
        _results.Remove(handle);
        _pendingTasks.Remove(handle);
    }
}
