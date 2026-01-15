using System.Collections.Generic;

namespace Core.Models;

public class DecisionDTO
{
    public string ProtocolVersion { get; set; } = "agent-json-v1";
    public int TickId { get; set; }
    public string AgentId { get; set; } = string.Empty;
    public string Mode { get; set; } = "act"; // observe, plan, act, recover
    public Goal Goal { get; set; } = new();
    public PlanGraph PlanGraph { get; set; } = new();
    public DecisionConstraints Constraints { get; set; } = new();
}

public class DecisionConstraints
{
    public int MaxReplans { get; set; } = 3;
    public int MaxTotalTicks { get; set; } = 4000;
    public int MaxGoldSpend { get; set; } = 400;
}
