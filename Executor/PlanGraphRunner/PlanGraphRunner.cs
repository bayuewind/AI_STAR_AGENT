using Agent.PlanGraphModels;
using Tools.Interfaces;
using Tools.Models;
using Executor.TransitionMatcher;

namespace Executor.PlanGraphRunner;

/// <summary>
/// Runner 状态枚举
/// </summary>
public enum RunnerStatus
{
    /// <summary>
    /// 正在运行（处理节点）
    /// </summary>
    Running,

    /// <summary>
    /// 等待工具执行完成
    /// </summary>
    WaitingTool,

    /// <summary>
    /// 需要重新规划
    /// </summary>
    NeedReplan,

    /// <summary>
    /// 完成目标
    /// </summary>
    Done
}

/// <summary>
/// PlanGraphRunner - 图执行器
/// 职责：执行node、处理Begin/Poll工具调用、超时检测、transition匹配、进入replan/done
/// </summary>
public class PlanGraphRunner
{
    private PlanGraph _planGraph;
    private readonly IToolDispatcher _toolDispatcher;
    
    /// <summary>
    /// 节点查找缓存（优化性能）
    /// </summary>
    private Dictionary<string, Node> _nodeCache;

    /// <summary>
    /// 当前节点ID
    /// </summary>
    public string? CurrentNodeId { get; private set; }

    /// <summary>
    /// 当前工具调用的 handle（如果有）
    /// </summary>
    private string? _currentHandle;

    /// <summary>
    /// 当前节点开始执行的 tick
    /// </summary>
    private int _nodeStartTick;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PlanGraphRunner(PlanGraph planGraph, IToolDispatcher toolDispatcher)
    {
        _planGraph = planGraph ?? throw new ArgumentNullException(nameof(planGraph));
        _toolDispatcher = toolDispatcher ?? throw new ArgumentNullException(nameof(toolDispatcher));
        _nodeCache = BuildNodeCache(planGraph);
        CurrentNodeId = planGraph.StartNodeId;
        _nodeStartTick = 0;
    }

    /// <summary>
    /// 构建节点查找缓存（O(1) 查找）
    /// </summary>
    private Dictionary<string, Node> BuildNodeCache(PlanGraph planGraph)
    {
        var cache = new Dictionary<string, Node>();
        foreach (var node in planGraph.Nodes)
        {
            cache[node.NodeId] = node;
        }
        return cache;
    }

    /// <summary>
    /// 替换当前计划图（用于 replan）
    /// </summary>
    public void ReplacePlan(PlanGraph newPlanGraph, int tickId)
    {
        // 清理旧的 handle
        if (_currentHandle != null)
        {
            _toolDispatcher.Cancel(_currentHandle, tickId);
            _currentHandle = null;
        }

        // 更新计划图和缓存
        _planGraph = newPlanGraph ?? throw new ArgumentNullException(nameof(newPlanGraph));
        _nodeCache = BuildNodeCache(newPlanGraph);
        CurrentNodeId = newPlanGraph.StartNodeId;
        _nodeStartTick = tickId;
        
        Console.WriteLine($"[PlanGraphRunner] 计划图已替换: {newPlanGraph.PlanId}, 起始节点: {newPlanGraph.StartNodeId}");
    }

    /// <summary>
    /// 重置 Runner 到当前计划图的起始节点
    /// </summary>
    public void Reset(int tickId)
    {
        // 清理旧的 handle
        if (_currentHandle != null)
        {
            _toolDispatcher.Cancel(_currentHandle, tickId);
            _currentHandle = null;
        }

        CurrentNodeId = _planGraph.StartNodeId;
        _nodeStartTick = tickId;
        
        Console.WriteLine($"[PlanGraphRunner] Runner 已重置到起始节点: {_planGraph.StartNodeId}");
    }

    /// <summary>
    /// 执行一个 tick
    /// </summary>
    /// <param name="tickId">当前 tick ID</param>
    /// <returns>Runner 状态</returns>
    public RunnerStatus Tick(int tickId)
    {
        // 如果没有当前节点，返回 NeedReplan
        if (string.IsNullOrEmpty(CurrentNodeId))
        {
            return RunnerStatus.NeedReplan;
        }

        // 查找当前节点（使用缓存，O(1)）
        if (!_nodeCache.TryGetValue(CurrentNodeId, out var currentNode))
        {
            Console.WriteLine($"[Tick {tickId}] 错误: 找不到节点 '{CurrentNodeId}'");
            return RunnerStatus.NeedReplan;
        }

        // 根据节点类型处理
        return currentNode.Type switch
        {
            NodeType.ToolCall => HandleToolCallNode(tickId, currentNode),
            NodeType.Recover => HandleRecoverNode(tickId, currentNode),
            NodeType.LlmReplan => HandleLlmReplanNode(tickId, currentNode),
            NodeType.FinishGoal => HandleFinishGoalNode(tickId, currentNode),
            _ => RunnerStatus.NeedReplan
        };
    }

    /// <summary>
    /// 处理工具调用节点
    /// </summary>
    private RunnerStatus HandleToolCallNode(int tickId, Node node)
    {
        // 如果还没有开始执行，调用 Begin
        if (_currentHandle == null)
        {
            _currentHandle = _toolDispatcher.Begin(node.NodeId, node.Tool, node.Args, tickId);
            _nodeStartTick = tickId;
            Console.WriteLine($"[Tick {tickId}] 节点 {node.NodeId}: 开始执行工具 {node.Tool}");
            return RunnerStatus.WaitingTool;
        }

        // 检查超时
        bool isTimeout = false;
        if (node.Timeout > 0 && (tickId - _nodeStartTick) >= node.Timeout)
        {
            isTimeout = true;
            Console.WriteLine($"[Tick {tickId}] 节点 {node.NodeId}: 超时 (超时时间: {node.Timeout} ticks)");
            // ★关键：超时后立即 Cancel，避免残留动作和资源泄漏
            _toolDispatcher.Cancel(_currentHandle!, tickId);
        }

        // Poll 工具结果
        var (ready, result) = _toolDispatcher.Poll(_currentHandle!, tickId);

        if (!ready && !isTimeout)
        {
            // 还未就绪，继续等待
            return RunnerStatus.WaitingTool;
        }

        // 处理异常情况：ready=true 但 result=null（dispatcher bug）
        if (ready && result == null)
        {
            Console.WriteLine($"[Tick {tickId}] 节点 {node.NodeId}: Poll 返回 ready=true 但 result=null，强制取消并重规划");
            _toolDispatcher.Cancel(_currentHandle!, tickId);
            _currentHandle = null;
            return RunnerStatus.NeedReplan;
        }

        // 如果超时但没有结果，创建一个超时结果
        if (isTimeout && result == null)
        {
            result = ToolResult.Timeout(node.NodeId, node.Tool);
        }

        // 确保有结果
        if (result == null)
        {
            Console.WriteLine($"[Tick {tickId}] 节点 {node.NodeId}: 错误 - 工具执行无结果");
            _toolDispatcher.Cancel(_currentHandle!, tickId);
            _currentHandle = null;
            return RunnerStatus.NeedReplan;
        }

        var errorInfo = result.Error != null ? $", Error={result.Error.Code}" : "";
        Console.WriteLine($"[Tick {tickId}] 节点 {node.NodeId}: 工具 {node.Tool} 执行完成, Ok={result.Ok}{errorInfo}");

        // 使用 TransitionMatcher 匹配转移条件
        var nextNodeId = Executor.TransitionMatcher.TransitionMatcher.Match(node.Transitions, result, isTimeout);

        // 清理当前 handle
        _currentHandle = null;

        if (nextNodeId == null)
        {
            // 无 transition 命中，需要重新规划
            Console.WriteLine($"[Tick {tickId}] 节点 {node.NodeId}: 无 transition 命中，需要重新规划");
            return RunnerStatus.NeedReplan;
        }

        // 跳转到下一个节点
        Console.WriteLine($"[Tick {tickId}] 节点 {node.NodeId}: 跳转到节点 {nextNodeId}");
        CurrentNodeId = nextNodeId;
        return RunnerStatus.Running;
    }

    /// <summary>
    /// 处理恢复节点（Recover）
    /// 未来可以在这里添加特殊策略：更高的重试次数、更强的恢复逻辑等
    /// </summary>
    private RunnerStatus HandleRecoverNode(int tickId, Node node)
    {
        // MVP：直接当 ToolCall 处理，但保留扩展点
        // 未来可以添加：
        // - 更高的重试次数
        // - 更强的 nudge/teleport/重置控制器
        // - Recover 失败后直接 llm_replan
        return HandleToolCallNode(tickId, node);
    }

    /// <summary>
    /// 处理 LLM 重规划节点
    /// </summary>
    private RunnerStatus HandleLlmReplanNode(int tickId, Node node)
    {
        Console.WriteLine($"[Tick {tickId}] 节点 {node.NodeId}: 遇到 LLM 重规划节点");
        return RunnerStatus.NeedReplan;
    }

    /// <summary>
    /// 处理完成目标节点
    /// </summary>
    private RunnerStatus HandleFinishGoalNode(int tickId, Node node)
    {
        Console.WriteLine($"[Tick {tickId}] 节点 {node.NodeId}: 完成目标节点");
        return RunnerStatus.Done;
    }
}
