using Agent.GoalModels;
using Agent.PlanGraphModels;

namespace Agent.DecisionProtocol;

/// <summary>
/// Decision JSON协议 - LLM输出的决策JSON结构
/// </summary>
public class DecisionJson
{
    public string ProtocolVersion { get; set; } = "agent-json-v1";
    public int TickId { get; set; }
    public string AgentId { get; set; } = string.Empty;
    public string Mode { get; set; } = string.Empty; // "observe" | "plan" | "act" | "recover"
    public Goal Goal { get; set; } = new();
    public PlanGraph PlanGraph { get; set; } = new();
    public Constraints Constraints { get; set; } = new();
}

/// <summary>
/// 约束条件
/// </summary>
public class Constraints
{
    public int MaxReplans { get; set; } = 3;
    public int MaxTotalTicks { get; set; } = 4000;
    public int MaxGoldSpend { get; set; } = 400;
}
