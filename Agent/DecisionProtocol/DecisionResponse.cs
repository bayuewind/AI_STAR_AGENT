using Agent.GoalModels;
using Agent.PlanGraphModels;

namespace Agent.DecisionProtocol;

/// <summary>
/// DecisionResponse - LLM输出的决策JSON结构（执行器和LLM层的唯一契约）
/// 定义 LLM 输出整体结构，先不连 LLM，也要定型
/// </summary>
public sealed class DecisionResponse
{
    /// <summary>
    /// 协议版本
    /// </summary>
    public string ProtocolVersion { get; set; } = "agent-json-v1";

    /// <summary>
    /// Tick ID（回显输入 tick_id）
    /// </summary>
    public int TickId { get; set; }

    /// <summary>
    /// 智能体ID
    /// </summary>
    public string AgentId { get; set; } = "player_1";

    /// <summary>
    /// 模式：observe / plan / act / recover
    /// </summary>
    public string Mode { get; set; } = "act";

    /// <summary>
    /// 目标
    /// </summary>
    public Goal Goal { get; set; } = new();

    /// <summary>
    /// 计划图
    /// </summary>
    public PlanGraph PlanGraph { get; set; } = new();

    /// <summary>
    /// 约束条件
    /// </summary>
    public Constraints Constraints { get; set; } = new();

    /// <summary>
    /// 记忆写入项（可选，可先留空）
    /// </summary>
    public List<MemoryWriteItem> MemoryWrite { get; set; } = new();
}

/// <summary>
/// 约束条件
/// </summary>
public sealed class Constraints
{
    /// <summary>
    /// 最大重规划次数
    /// </summary>
    public int MaxReplans { get; set; } = 3;

    /// <summary>
    /// 最大总 ticks
    /// </summary>
    public int MaxTotalTicks { get; set; } = 4000;

    /// <summary>
    /// 最大金币花费
    /// </summary>
    public int MaxGoldSpend { get; set; } = 400;
}

/// <summary>
/// 记忆写入项（可选）
/// </summary>
public sealed class MemoryWriteItem
{
    /// <summary>
    /// 记忆类型（fact/skill/preference等）
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 记忆内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 关联的键（用于检索）
    /// </summary>
    public List<string> Keys { get; set; } = new();
}
