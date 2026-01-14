namespace Agent.PlanGraphModels;

/// <summary>
/// 计划图模型
/// 必须包含：plan_id, start_node_id, nodes[]
/// </summary>
public class PlanGraph
{
    /// <summary>
    /// 计划ID
    /// </summary>
    public string PlanId { get; set; } = string.Empty;

    /// <summary>
    /// 起始节点ID
    /// </summary>
    public string StartNodeId { get; set; } = string.Empty;

    /// <summary>
    /// 节点列表
    /// </summary>
    public List<Node> Nodes { get; set; } = new();

    /// <summary>
    /// 根据节点ID查找节点
    /// </summary>
    public Node? FindNode(string nodeId)
    {
        return Nodes.FirstOrDefault(n => n.NodeId == nodeId);
    }

    /// <summary>
    /// 验证计划图的基本完整性
    /// </summary>
    public bool Validate()
    {
        if (string.IsNullOrEmpty(PlanId))
            return false;
        
        if (string.IsNullOrEmpty(StartNodeId))
            return false;
        
        if (Nodes == null || Nodes.Count == 0)
            return false;
        
        // 检查起始节点是否存在
        if (FindNode(StartNodeId) == null)
            return false;
        
        // 检查所有转移的目标节点是否存在
        foreach (var node in Nodes)
        {
            foreach (var transition in node.Transitions)
            {
                if (FindNode(transition.TargetNodeId) == null)
                    return false;
            }
        }
        
        return true;
    }
}

/// <summary>
/// 节点类型枚举
/// 必须支持：tool_call, llm_replan, finish_goal, recover
/// </summary>
public enum NodeType
{
    /// <summary>
    /// 工具调用节点
    /// </summary>
    ToolCall,

    /// <summary>
    /// 恢复策略节点（仍可用 tool_call 表达）
    /// </summary>
    Recover,

    /// <summary>
    /// 要求执行器重新调用 LLM
    /// </summary>
    LlmReplan,

    /// <summary>
    /// 完成目标节点
    /// </summary>
    FinishGoal
}

/// <summary>
/// 计划节点
/// 每个节点包含：node_id, type, tool, args, timeout, transitions[]
/// </summary>
public class Node
{
    /// <summary>
    /// 节点ID（唯一标识）
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// 节点类型
    /// </summary>
    public NodeType Type { get; set; }

    /// <summary>
    /// 工具名称（tool_call 类型节点必须）
    /// </summary>
    public string Tool { get; set; } = string.Empty;

    /// <summary>
    /// 工具参数
    /// </summary>
    public Dictionary<string, object> Args { get; set; } = new();

    /// <summary>
    /// 超时时间（单位：ticks）
    /// </summary>
    public int Timeout { get; set; }

    /// <summary>
    /// 转移条件列表（按顺序匹配，第一个命中即跳转）
    /// </summary>
    public List<Transition> Transitions { get; set; } = new();

    /// <summary>
    /// 创建工具调用节点
    /// </summary>
    public static Node CreateToolCall(string nodeId, string tool, Dictionary<string, object>? args = null, int timeout = 1000)
    {
        return new Node
        {
            NodeId = nodeId,
            Type = NodeType.ToolCall,
            Tool = tool,
            Args = args ?? new Dictionary<string, object>(),
            Timeout = timeout,
            Transitions = new List<Transition>()
        };
    }

    /// <summary>
    /// 创建重规划节点
    /// </summary>
    public static Node CreateLlmReplan(string nodeId)
    {
        return new Node
        {
            NodeId = nodeId,
            Type = NodeType.LlmReplan,
            Tool = string.Empty,
            Args = new Dictionary<string, object>(),
            Timeout = 0,
            Transitions = new List<Transition>()
        };
    }

    /// <summary>
    /// 创建完成目标节点
    /// </summary>
    public static Node CreateFinishGoal(string nodeId)
    {
        return new Node
        {
            NodeId = nodeId,
            Type = NodeType.FinishGoal,
            Tool = string.Empty,
            Args = new Dictionary<string, object>(),
            Timeout = 0,
            Transitions = new List<Transition>()
        };
    }

    /// <summary>
    /// 创建恢复节点
    /// </summary>
    public static Node CreateRecover(string nodeId, string tool, Dictionary<string, object>? args = null, int timeout = 1000)
    {
        return new Node
        {
            NodeId = nodeId,
            Type = NodeType.Recover,
            Tool = tool,
            Args = args ?? new Dictionary<string, object>(),
            Timeout = timeout,
            Transitions = new List<Transition>()
        };
    }

    /// <summary>
    /// 添加转移条件
    /// </summary>
    public Node AddTransition(string expression, string targetNodeId)
    {
        Transitions.Add(new Transition
        {
            Expression = expression,
            TargetNodeId = targetNodeId
        });
        return this;
    }
}

/// <summary>
/// 转移条件
/// 表达式支持：
/// - "ok": tool_result.ok == true
/// - "timeout": 节点超时
/// - "err:*": 任意错误
/// - "err:code1|err:code2": 错误码在集合内
/// - "ok|err:*|timeout": 逻辑或（用于 CloseMenu 这类无关紧要步骤）
/// </summary>
public class Transition
{
    /// <summary>
    /// 转移表达式（匹配条件）
    /// 支持的格式：
    /// - "ok" - 成功
    /// - "timeout" - 超时
    /// - "err:*" - 任意错误
    /// - "err:xxx" - 特定错误码
    /// - "err:xxx|err:yyy" - 多个错误码的逻辑或
    /// - "ok|timeout|err:*" - 多个条件的逻辑或
    /// </summary>
    public string Expression { get; set; } = string.Empty;

    /// <summary>
    /// 目标节点ID（转移到的节点）
    /// </summary>
    public string TargetNodeId { get; set; } = string.Empty;
}
