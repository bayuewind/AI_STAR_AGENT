namespace Agent.PlanGraphModels;

/// <summary>
/// 计划图模型
/// </summary>
public class PlanGraph
{
    public string PlanId { get; set; } = string.Empty;
    public string StartNodeId { get; set; } = string.Empty;
    public List<Node> Nodes { get; set; } = new();
}

/// <summary>
/// 节点类型
/// </summary>
public enum NodeType
{
    ToolCall,
    Recover,
    LlmReplan,
    FinishGoal
}

/// <summary>
/// 计划节点
/// </summary>
public class Node
{
    public string NodeId { get; set; } = string.Empty;
    public NodeType Type { get; set; }
    public string Tool { get; set; } = string.Empty;
    public Dictionary<string, object> Args { get; set; } = new();
    public int Timeout { get; set; }
    public List<Transition> Transitions { get; set; } = new();
}

/// <summary>
/// 转移条件
/// </summary>
public class Transition
{
    public string Expression { get; set; } = string.Empty;
    public string TargetNodeId { get; set; } = string.Empty;
}
