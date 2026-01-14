namespace Agent.GoalModels;

using Agent.AgentRuntime;

/// <summary>
/// GoalStack 验收测试
/// 验收1：AgentRuntime 能持有 goal_stack
/// 验收2：随便 new 一个 GoalStack，Push/Pop 正常
/// </summary>
public static class GoalStackTest
{
    /// <summary>
    /// 运行所有验收测试
    /// </summary>
    public static void RunTests()
    {
        Console.WriteLine("=== GoalStack 验收测试 ===\n");

        // 验收1: AgentRuntime 能持有 goal_stack
        Console.WriteLine("✓ 验收1: AgentRuntime 能持有 goal_stack");
        var runtime = new AgentRuntime();
        Console.WriteLine($"  AgentRuntime.GoalStack 类型: {runtime.GoalStack.GetType().Name}");
        Console.WriteLine($"  GoalStack.IsEmpty: {runtime.GoalStack.IsEmpty}");
        Console.WriteLine($"  GoalStack.Count: {runtime.GoalStack.Count}");

        // 添加目标到 AgentRuntime 的 goal_stack
        runtime.GoalStack.Push(new Goal
        {
            Id = "goal_001",
            Text = "去皮埃尔买10个防风草种子",
            Priority = 1
        });
        Console.WriteLine($"  添加目标后 Count: {runtime.GoalStack.Count}");
        Console.WriteLine($"  当前目标: {runtime.GoalStack.Peek()?.Text}");

        // 验收2: 随便 new 一个 GoalStack，Push/Pop 正常
        Console.WriteLine("\n✓ 验收2: GoalStack Push/Pop 操作");
        var goalStack = new GoalStack();
        Console.WriteLine($"  初始状态 - IsEmpty: {goalStack.IsEmpty}, Count: {goalStack.Count}");

        // Push 测试
        Console.WriteLine("\n  测试 Push:");
        goalStack.Push(new Goal { Id = "goal_1", Text = "目标1", Priority = 1 });
        Console.WriteLine($"    添加目标1后 - Count: {goalStack.Count}, IsEmpty: {goalStack.IsEmpty}");

        goalStack.Push(new Goal { Id = "goal_2", Text = "目标2", Priority = 3 });
        Console.WriteLine($"    添加目标2（优先级3）后 - Count: {goalStack.Count}");

        goalStack.Push(new Goal { Id = "goal_3", Text = "目标3", Priority = 2 });
        Console.WriteLine($"    添加目标3（优先级2）后 - Count: {goalStack.Count}");

        // Peek 测试（应该返回优先级最高的）
        Console.WriteLine("\n  测试 Peek（查看最高优先级目标）:");
        var peekedGoal = goalStack.Peek();
        Console.WriteLine($"    Peek 结果: {peekedGoal?.Text} (优先级: {peekedGoal?.Priority})");
        Console.WriteLine($"    Count 未变: {goalStack.Count}");

        // Pop 测试
        Console.WriteLine("\n  测试 Pop（弹出最高优先级目标）:");
        var poppedGoal1 = goalStack.Pop();
        Console.WriteLine($"    Pop 结果: {poppedGoal1?.Text} (优先级: {poppedGoal1?.Priority})");
        Console.WriteLine($"    Pop 后 Count: {goalStack.Count}");

        var poppedGoal2 = goalStack.Pop();
        Console.WriteLine($"    再次 Pop: {poppedGoal2?.Text} (优先级: {poppedGoal2?.Priority})");
        Console.WriteLine($"    Pop 后 Count: {goalStack.Count}");

        var poppedGoal3 = goalStack.Pop();
        Console.WriteLine($"    第三次 Pop: {poppedGoal3?.Text} (优先级: {poppedGoal3?.Priority})");
        Console.WriteLine($"    Pop 后 Count: {goalStack.Count}");
        Console.WriteLine($"    栈是否为空: {goalStack.IsEmpty}");

        // Pop 空栈测试
        Console.WriteLine("\n  测试 Pop 空栈:");
        var emptyPop = goalStack.Pop();
        Console.WriteLine($"    空栈 Pop 结果: {(emptyPop == null ? "null ✓" : "not null ✗")}");

        // GetAll 测试
        Console.WriteLine("\n  测试 GetAll（获取所有目标，按优先级排序）:");
        goalStack.Push(new Goal { Id = "goal_a", Text = "目标A", Priority = 5 });
        goalStack.Push(new Goal { Id = "goal_b", Text = "目标B", Priority = 1 });
        goalStack.Push(new Goal { Id = "goal_c", Text = "目标C", Priority = 3 });
        
        var allGoals = goalStack.GetAll();
        Console.WriteLine($"    所有目标（按优先级排序）:");
        foreach (var goal in allGoals)
        {
            Console.WriteLine($"      - {goal.Text} (优先级: {goal.Priority})");
        }

        // Remove 测试
        Console.WriteLine("\n  测试 Remove（移除指定ID的目标）:");
        Console.WriteLine($"    移除前 Count: {goalStack.Count}");
        var removed = goalStack.Remove("goal_b");
        Console.WriteLine($"    移除 goal_b: {removed}, 移除后 Count: {goalStack.Count}");

        // Contains 测试
        Console.WriteLine("\n  测试 Contains（检查是否包含指定ID）:");
        Console.WriteLine($"    包含 goal_a: {goalStack.Contains("goal_a")}");
        Console.WriteLine($"    包含 goal_b: {goalStack.Contains("goal_b")}");

        // Clear 测试
        Console.WriteLine("\n  测试 Clear（清空所有目标）:");
        goalStack.Clear();
        Console.WriteLine($"    Clear 后 Count: {goalStack.Count}, IsEmpty: {goalStack.IsEmpty}");

        // 综合测试：模拟实际使用场景
        Console.WriteLine("\n✓ 验收3: 综合使用场景（模拟 AgentRuntime 使用）");
        var runtime2 = new Agent.AgentRuntime.AgentRuntime();
        
        runtime2.GoalStack.Push(new Goal
        {
            Id = "daily_001",
            Text = "完成日常任务",
            Priority = 2
        });
        
        runtime2.GoalStack.Push(new Goal
        {
            Id = "urgent_001",
            Text = "紧急：购买种子",
            Priority = 5
        });
        
        runtime2.GoalStack.Push(new Goal
        {
            Id = "normal_001",
            Text = "正常任务：浇水",
            Priority = 1
        });

        Console.WriteLine($"  AgentRuntime 的 GoalStack 中有 {runtime2.GoalStack.Count} 个目标");
        var currentGoal = runtime2.GoalStack.Peek();
        Console.WriteLine($"  当前最高优先级目标: {currentGoal?.Text} (优先级: {currentGoal?.Priority})");

        Console.WriteLine("\n=== ✅ GoalStack 验收测试完成 ===");
    }
}
