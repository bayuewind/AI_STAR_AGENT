using Core.Models;
using Core.Interfaces;

namespace Executor.PlanGraphRunner;

public enum RunnerStatus
{
    Running,
    WaitingTool,
    NeedReplan,
    Done
}

public class PlanGraphRunner
{
    private PlanGraph _planGraph;
    private readonly IToolDispatcher _toolDispatcher;
    private Dictionary<string, Node> _nodeCache;

    public string? CurrentNodeId { get; private set; }
    public ToolResult? LastToolResult { get; private set; }
    public string? CurrentToolName 
    { 
        get 
        {
            if (CurrentNodeId != null && _nodeCache.TryGetValue(CurrentNodeId, out var node))
            {
                return node.Tool;
            }
            return null;
        }
    }
    private string? _currentHandle;
    private int _nodeStartTick;

    public PlanGraphRunner(PlanGraph planGraph, IToolDispatcher toolDispatcher)
    {
        _planGraph = planGraph ?? throw new ArgumentNullException(nameof(planGraph));
        _toolDispatcher = toolDispatcher ?? throw new ArgumentNullException(nameof(toolDispatcher));
        _nodeCache = BuildNodeCache(planGraph);
        CurrentNodeId = planGraph.StartNodeId;
        _nodeStartTick = 0;
    }

    private Dictionary<string, Node> BuildNodeCache(PlanGraph planGraph)
    {
        var cache = new Dictionary<string, Node>();
        foreach (var node in planGraph.Nodes)
        {
            cache[node.NodeId] = node;
        }
        return cache;
    }

    public void ReplacePlan(PlanGraph newPlanGraph, int tickId)
    {
        if (_currentHandle != null)
        {
            _toolDispatcher.Cancel(_currentHandle, tickId);
            _currentHandle = null;
        }

        _planGraph = newPlanGraph ?? throw new ArgumentNullException(nameof(newPlanGraph));
        _nodeCache = BuildNodeCache(newPlanGraph);
        CurrentNodeId = newPlanGraph.StartNodeId;
        _nodeStartTick = tickId;
    }

    public void Reset(int tickId)
    {
        if (_currentHandle != null)
        {
            _toolDispatcher.Cancel(_currentHandle, tickId);
            _currentHandle = null;
        }

        CurrentNodeId = _planGraph.StartNodeId;
        _nodeStartTick = tickId;
    }

    public PlanGraph GetPlanGraph() => _planGraph;

    public RunnerStatus Tick(int tickId)
    {
        if (string.IsNullOrEmpty(CurrentNodeId)) return RunnerStatus.NeedReplan;

        if (!_nodeCache.TryGetValue(CurrentNodeId, out var currentNode))
        {
            Console.WriteLine($"[Runner] Error: Node {CurrentNodeId} not found in cache.");
            return RunnerStatus.NeedReplan;
        }

        return currentNode.Type switch
        {
            NodeType.ToolCall => HandleToolCallNode(tickId, currentNode),
            NodeType.Recover => HandleToolCallNode(tickId, currentNode),
            NodeType.LlmReplan => RunnerStatus.NeedReplan,
            NodeType.FinishGoal => RunnerStatus.Done,
            _ => RunnerStatus.NeedReplan
        };
    }

    private RunnerStatus HandleToolCallNode(int tickId, Node node)
    {
        if (_currentHandle == null)
        {
            _currentHandle = _toolDispatcher.Begin(node.NodeId, node.Tool, node.Args, tickId);
            _nodeStartTick = tickId;
            Console.WriteLine($"[Runner] Tick {tickId}: Node {node.NodeId} - Started tool {node.Tool}");
            return RunnerStatus.WaitingTool;
        }

        bool isTimeout = false;
        if (node.Timeout > 0 && (tickId - _nodeStartTick) >= node.Timeout)
        {
            isTimeout = true;
            Console.WriteLine($"[Runner] Tick {tickId}: Node {node.NodeId} - Timeout reached ({node.Timeout} ticks)");
            _toolDispatcher.Cancel(_currentHandle, tickId);
        }

        var (ready, result) = _toolDispatcher.Poll(_currentHandle, tickId);

        if (!ready && !isTimeout) return RunnerStatus.WaitingTool;

        if (isTimeout && result == null) result = ToolResult.Timeout(node.NodeId, node.Tool);

        if (result == null)
        {
            Console.WriteLine($"[Runner] Tick {tickId}: Node {node.NodeId} - Error: Tool result is null");
            _toolDispatcher.Cancel(_currentHandle, tickId);
            _currentHandle = null;
            return RunnerStatus.NeedReplan;
        }

        LastToolResult = result;
        Console.WriteLine($"[Runner] Tick {tickId}: Node {node.NodeId} - Tool {node.Tool} finished. Ok={result.Ok}");

        var nextNodeId = Executor.TransitionMatcher.TransitionMatcher.Match(node.Transitions, result, isTimeout);
        _currentHandle = null;

        if (nextNodeId == null)
        {
            Console.WriteLine($"[Runner] Tick {tickId}: Node {node.NodeId} - No matching transition found.");
            return RunnerStatus.NeedReplan;
        }

        Console.WriteLine($"[Runner] Tick {tickId}: Node {node.NodeId} -> Transition to {nextNodeId}");
        CurrentNodeId = nextNodeId;
        return RunnerStatus.Running;
    }
}
