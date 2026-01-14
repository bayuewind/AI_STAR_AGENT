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
    private readonly PlanGraph _planGraph;
    private readonly IToolDispatcher _toolDispatcher;

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
        CurrentNodeId = planGraph.StartNodeId;
        _nodeStartTick = 0;
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

        // 查找当前节点
        var currentNode = _planGraph.FindNode(CurrentNodeId);
        if (currentNode == null)
        {
            Console.WriteLine($"[Tick {tickId}] 错误: 找不到节点 '{CurrentNodeId}'");
            return RunnerStatus.NeedReplan;
        }

        // 根据节点类型处理
        return currentNode.Type switch
        {
            NodeType.ToolCall => HandleToolCallNode(tickId, currentNode),
            NodeType.Recover => HandleToolCallNode(tickId, currentNode), // Recover 也是工具调用
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
            _currentHandle = _toolDispatcher.Begin(node.NodeId, node.Tool, node.Args);
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
        }

        // Poll 工具结果
        var (ready, result) = _toolDispatcher.Poll(_currentHandle!);

        if (!ready && !isTimeout)
        {
            // 还未就绪，继续等待
            return RunnerStatus.WaitingTool;
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
