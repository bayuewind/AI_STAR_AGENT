using Tools.Interfaces;
using Tools.Models;

namespace Tools.Implementations;

/// <summary>
/// ToolDispatcherStub - 工具调度器存根实现（用于测试）
/// 关键：能"延迟几 tick 返回结果"，tick 驱动由外部 Runner 控制
/// </summary>
public class ToolDispatcherStub : IToolDispatcher
{
    /// <summary>
    /// 任务信息：保存 handle 对应的任务状态
    /// </summary>
    private class TaskInfo
    {
        public string NodeId { get; set; } = string.Empty;
        public string ToolName { get; set; } = string.Empty;
        public int StartTick { get; set; }
        public int FinishTick { get; set; }
        public ToolResult? PlannedResult { get; set; }
    }

    /// <summary>
    /// 工具配置信息
    /// </summary>
    private class ToolConfig
    {
        public bool Ok { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorDetail { get; set; }
    }

    /// <summary>
    /// 待处理任务字典：handle -> TaskInfo
    /// </summary>
    private readonly Dictionary<string, TaskInfo> _pendingTasks = new();

    /// <summary>
    /// 已完成任务结果字典：handle -> ToolResult
    /// </summary>
    private readonly Dictionary<string, ToolResult> _results = new();

    /// <summary>
    /// 已取消的 handle 集合
    /// </summary>
    private readonly HashSet<string> _canceled = new();

    /// <summary>
    /// 工具配置字典：toolName -> ToolConfig
    /// </summary>
    private readonly Dictionary<string, ToolConfig> _toolConfigs = new();

    /// <summary>
    /// 配置工具的返回结果（用于测试）
    /// </summary>
    /// <param name="toolName">工具名称</param>
    /// <param name="ok">是否成功</param>
    /// <param name="errorCode">错误码（如果失败）</param>
    /// <param name="errorDetail">错误详情（如果失败）</param>
    public void ConfigureToolResult(string toolName, bool ok, string? errorCode = null, string? errorDetail = null)
    {
        _toolConfigs[toolName] = new ToolConfig
        {
            Ok = ok,
            ErrorCode = errorCode,
            ErrorDetail = errorDetail
        };
    }

    /// <summary>
    /// 重置工具配置为默认（成功）
    /// </summary>
    public void ResetToolConfig(string? toolName = null)
    {
        if (toolName == null)
        {
            _toolConfigs.Clear();
        }
        else
        {
            _toolConfigs.Remove(toolName);
        }
    }

    /// <summary>
    /// 配置 ShopBuy 的返回结果（向后兼容）
    /// </summary>
    public void ConfigureShopBuyResult(bool ok, string? errorCode = null, string? errorDetail = null)
    {
        ConfigureToolResult("ShopBuy", ok, errorCode, errorDetail);
    }

    /// <summary>
    /// 重置 ShopBuy 配置（向后兼容）
    /// </summary>
    public void ResetShopBuyConfig()
    {
        ResetToolConfig("ShopBuy");
    }

    /// <summary>
    /// 获取工具的延迟 tick 数
    /// </summary>
    private int GetToolDelayTicks(string toolName)
    {
        return toolName switch
        {
            "NavigateTo" => 60,
            "ShopBuy" => 10,
            _ => 5 // 其它工具默认 5 ticks
        };
    }

    /// <summary>
    /// 创建工具结果
    /// </summary>
    private ToolResult CreateToolResult(string nodeId, string toolName, bool ok, int tickId, string? errorCode = null, string? errorDetail = null)
    {
        return new ToolResult
        {
            ActionNodeId = nodeId,
            Tool = toolName,
            Ok = ok,
            Error = ok ? null : new ToolError
            {
                Code = errorCode ?? "stub_error",
                Detail = errorDetail ?? "Stub error"
            },
            Telemetry = new Dictionary<string, object>
            {
                ["tick"] = tickId,
                ["node_id"] = nodeId,
                ["tool"] = toolName
            }
        };
    }

    public string Begin(string nodeId, string toolName, Dictionary<string, object> args, int tickId)
    {
        var handle = Guid.NewGuid().ToString();
        var delayTicks = GetToolDelayTicks(toolName);
        
        // 确定结果（如果有配置则使用配置，否则默认成功）
        ToolResult? plannedResult = null;
        if (_toolConfigs.TryGetValue(toolName, out var config))
        {
            plannedResult = CreateToolResult(nodeId, toolName, config.Ok, tickId + delayTicks, config.ErrorCode, config.ErrorDetail);
        }
        else
        {
            // 默认成功
            plannedResult = CreateToolResult(nodeId, toolName, ok: true, tickId + delayTicks);
        }

        var taskInfo = new TaskInfo
        {
            NodeId = nodeId,
            ToolName = toolName,
            StartTick = tickId,
            FinishTick = tickId + delayTicks,
            PlannedResult = plannedResult
        };

        _pendingTasks[handle] = taskInfo;
        return handle;
    }

    public (bool ready, ToolResult? result) Poll(string handle, int tickId)
    {
        // 检查是否已取消
        if (_canceled.Contains(handle))
        {
            // 已取消：返回 ready=true 且 error.code="canceled"
            var canceledResult = CreateToolResult("unknown", "unknown", ok: false, tickId, "canceled", "Tool execution was canceled");
            return (true, canceledResult);
        }

        // 如果已经有结果，直接返回
        if (_results.TryGetValue(handle, out var existingResult))
        {
            return (true, existingResult);
        }

        // 检查是否开始执行
        if (!_pendingTasks.TryGetValue(handle, out var taskInfo))
        {
            // 无效的 handle：返回 ready=true 且 error.code="invalid_handle"
            var invalidResult = CreateToolResult("unknown", "unknown", ok: false, tickId, "invalid_handle", "Handle not found or already completed");
            return (true, invalidResult);
        }

        // 检查是否已经到达完成 tick
        if (tickId < taskInfo.FinishTick)
        {
            // 还未就绪
            return (false, null);
        }

        // 已经就绪，返回计划的结果
        var result = taskInfo.PlannedResult ?? CreateToolResult(taskInfo.NodeId, taskInfo.ToolName, ok: true, tickId);
        _results[handle] = result;
        _pendingTasks.Remove(handle);
        return (true, result);
    }

    public void Cancel(string handle, int tickId)
    {
        // 标记为已取消
        _canceled.Add(handle);
        
        // 清理任务和结果
        _results.Remove(handle);
        _pendingTasks.Remove(handle);
    }
}
