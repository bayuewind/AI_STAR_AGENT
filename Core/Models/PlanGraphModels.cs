using System.Text.Json.Serialization;

namespace Core.Models;

public enum NodeType
{
    ToolCall,
    Recover,
    LlmReplan,
    FinishGoal
}

public class Node
{
    [JsonPropertyName("node_id")]
    public string NodeId { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public NodeType Type { get; set; }

    [JsonPropertyName("tool")]
    public string Tool { get; set; } = string.Empty;

    [JsonPropertyName("args")]
    public Dictionary<string, object> Args { get; set; } = new();

    [JsonPropertyName("timeout")]
    public int Timeout { get; set; }

    [JsonPropertyName("transitions")]
    public List<Transition> Transitions { get; set; } = new();

    public static Node CreateToolCall(string nodeId, string tool, Dictionary<string, object>? args = null, int timeout = 1000)
    {
        return new Node { NodeId = nodeId, Type = NodeType.ToolCall, Tool = tool, Args = args ?? new(), Timeout = timeout };
    }

    public static Node CreateLlmReplan(string nodeId)
    {
        return new Node { NodeId = nodeId, Type = NodeType.LlmReplan };
    }

    public static Node CreateFinishGoal(string nodeId)
    {
        return new Node { NodeId = nodeId, Type = NodeType.FinishGoal };
    }

    public static Node CreateRecover(string nodeId, string tool, Dictionary<string, object>? args = null, int timeout = 1000)
    {
        return new Node { NodeId = nodeId, Type = NodeType.Recover, Tool = tool, Args = args ?? new(), Timeout = timeout };
    }

    public Node AddTransition(string expression, string targetNodeId)
    {
        Transitions.Add(new Transition { Expression = expression, TargetNodeId = targetNodeId });
        return this;
    }
}

public class Transition
{
    [JsonPropertyName("expression")]
    public string Expression { get; set; } = string.Empty;

    [JsonPropertyName("target_node_id")]
    public string TargetNodeId { get; set; } = string.Empty;
}

public class PlanGraph
{
    [JsonPropertyName("plan_id")]
    public string PlanId { get; set; } = string.Empty;

    [JsonPropertyName("start_node_id")]
    public string StartNodeId { get; set; } = string.Empty;

    [JsonPropertyName("nodes")]
    public List<Node> Nodes { get; set; } = new();

    public Node? FindNode(string nodeId) => Nodes.FirstOrDefault(n => n.NodeId == nodeId);

    public bool Validate()
    {
        if (string.IsNullOrEmpty(PlanId) || string.IsNullOrEmpty(StartNodeId) || Nodes.Count == 0) return false;
        if (FindNode(StartNodeId) == null) return false;
        foreach (var node in Nodes)
        {
            foreach (var t in node.Transitions)
            {
                if (FindNode(t.TargetNodeId) == null) return false;
            }
        }
        return true;
    }
}
