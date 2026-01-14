using Agent.GoalModels;

namespace Agent.AgentRuntime;

/// <summary>
/// AgentRuntime - 每个AI智能体的运行时实例
/// 职责：保存goal_stack / 当前plan_graph / 当前node / 计数器
/// 提供Update(snapshot)，由SMAPI Tick驱动
/// </summary>
public class AgentRuntime
{
    /// <summary>
    /// 目标栈（AgentRuntime 持有 goal_stack）
    /// </summary>
    public GoalStack GoalStack { get; } = new();

    // TODO: 实现其他 AgentRuntime 逻辑
    // - 当前 plan_graph
    // - 当前 node
    // - 计数器（stuck_counter, replan_count等）
    // - Update(snapshot) 方法
}
