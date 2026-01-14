using Agent.GoalModels;

namespace Agent.GoalModels;

/// <summary>
/// GoalStack 验证程序
/// 验收标准：
/// 1. AgentRuntime 能持有 goal_stack
/// 2. 随便 new 一个 GoalStack，Push/Pop 正常
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        GoalStackTest.RunTests();
    }
}
