using Tools.Interfaces;
using Tools.Models;

namespace Tools.Implementations;

/// <summary>
/// ToolDispatcherStub - 工具调度器存根实现（用于测试）
/// 关键：能"延迟几 tick 返回结果"
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
    /// 当前 tick 计数器
    /// </summary>
    public int CurrentTick { get; private set; }

    /// <summary>
    /// 待处理任务字典：handle -> TaskInfo
    /// </summary>
    private readonly Dictionary<string, TaskInfo> _pendingTasks = new();

    /// <summary>
    /// 已完成任务结果字典：handle -> ToolResult
    /// </summary>
    private readonly Dictionary<string, ToolResult> _results = new();

    /// <summary>
    /// ShopBuy 的配置结果（用于测试错误情况）
    /// </summary>
    private (bool ok, string? errorCode, string? errorDetail)? _shopBuyConfig;

    /// <summary>
    /// 推进一个 tick（由外部调用，模拟游戏 tick）
    /// </summary>
    public void Tick()
    {
        CurrentTick++;
    }

    /// <summary>
    /// 配置 ShopBuy 的返回结果（用于测试）
    /// </summary>
    /// <param name="ok">是否成功</param>
    /// <param name="errorCode">错误码（如果失败）</param>
    /// <param name="errorDetail">错误详情（如果失败）</param>
    public void ConfigureShopBuyResult(bool ok, string? errorCode = null, string? errorDetail = null)
    {
        _shopBuyConfig = (ok, errorCode, errorDetail);
    }

    /// <summary>
    /// 重置 ShopBuy 配置为默认（成功）
    /// </summary>
    public void ResetShopBuyConfig()
    {
        _shopBuyConfig = null;
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
    private ToolResult CreateToolResult(string nodeId, string toolName, bool ok, string? errorCode = null, string? errorDetail = null)
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
            Telemetry = new Dictionary<string, object>()
        };
    }

    public string Begin(string nodeId, string toolName, Dictionary<string, object> args)
    {
        var handle = Guid.NewGuid().ToString();
        var delayTicks = GetToolDelayTicks(toolName);
        
        // 确定结果（对于 ShopBuy，使用配置；其它工具默认成功）
        ToolResult? plannedResult = null;
        if (toolName == "ShopBuy" && _shopBuyConfig.HasValue)
        {
            var config = _shopBuyConfig.Value;
            plannedResult = CreateToolResult(nodeId, toolName, config.ok, config.errorCode, config.errorDetail);
        }
        else
        {
            // 默认成功
            plannedResult = CreateToolResult(nodeId, toolName, ok: true);
        }

        var taskInfo = new TaskInfo
        {
            NodeId = nodeId,
            ToolName = toolName,
            StartTick = CurrentTick,
            FinishTick = CurrentTick + delayTicks,
            PlannedResult = plannedResult
        };

        _pendingTasks[handle] = taskInfo;
        return handle;
    }

    public (bool ready, ToolResult? result) Poll(string handle)
    {
        // 如果已经有结果，直接返回
        if (_results.TryGetValue(handle, out var existingResult))
        {
            return (true, existingResult);
        }

        // 检查是否开始执行
        if (!_pendingTasks.TryGetValue(handle, out var taskInfo))
        {
            // 无效的 handle
            return (false, null);
        }

        // 检查是否已经到达完成 tick
        if (CurrentTick < taskInfo.FinishTick)
        {
            // 还未就绪
            return (false, null);
        }

        // 已经就绪，返回计划的结果
        var result = taskInfo.PlannedResult ?? CreateToolResult(taskInfo.NodeId, taskInfo.ToolName, ok: true);
        _results[handle] = result;
        _pendingTasks.Remove(handle);
        return (true, result);
    }

    public void Cancel(string handle)
    {
        _results.Remove(handle);
        _pendingTasks.Remove(handle);
    }
}
