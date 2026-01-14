using Agent.PlanGraphModels;
using Executor.PlanGraphRunner;
using Tools.Implementations;
using Tools.Interfaces;

namespace Executor.PlanGraphRunner;

/// <summary>
/// PlanGraphRunner 测试程序
/// 验收：给它一个固定 PlanGraph，它能从 start 跑到 done，并输出日志
/// </summary>
class PlanGraphRunnerTest
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== PlanGraphRunner 测试开始 ===\n");

        // 创建一个简单的测试计划图
        var planGraph = CreateTestPlanGraph();
        Console.WriteLine($"计划图: {planGraph.PlanId}");
        Console.WriteLine($"起始节点: {planGraph.StartNodeId}");
        Console.WriteLine($"节点数量: {planGraph.Nodes.Count}");
        Console.WriteLine($"验证结果: {(planGraph.Validate() ? "✓ 通过" : "✗ 失败")}\n");

        // 创建 ToolDispatcherStub
        var toolDispatcher = new ToolDispatcherStub();

        // 创建 PlanGraphRunner
        var runner = new PlanGraphRunner(planGraph, toolDispatcher);

        // 执行循环：每 tick 调用一次
        Console.WriteLine("=== 开始执行计划图 ===\n");
        int tickId = 0;
        int maxTicks = 200; // 防止无限循环

        while (tickId < maxTicks)
        {
            // 推进 tool dispatcher 的 tick
            toolDispatcher.Tick();

            // 执行 runner 的 tick
            var status = runner.Tick(tickId);

            // 根据状态决定下一步
            if (status == RunnerStatus.Done)
            {
                Console.WriteLine($"\n[Tick {tickId}] ✅ 完成目标！");
                break;
            }
            else if (status == RunnerStatus.NeedReplan)
            {
                Console.WriteLine($"\n[Tick {tickId}] ⚠️ 需要重新规划");
                break;
            }

            tickId++;
        }

        if (tickId >= maxTicks)
        {
            Console.WriteLine($"\n⚠️ 达到最大 tick 数 ({maxTicks})，停止执行");
        }

        Console.WriteLine("\n=== PlanGraphRunner 测试完成 ===");
    }

    /// <summary>
    /// 创建一个简单的测试计划图
    /// </summary>
    static PlanGraph CreateTestPlanGraph()
    {
        var plan = new PlanGraph
        {
            PlanId = "plan_test_001",
            StartNodeId = "step1"
        };

        // 步骤1: NavigateTo（60 ticks）
        var step1 = Node.CreateToolCall("step1", "NavigateTo", new Dictionary<string, object>
        {
            { "location", "Town" }
        }, timeout: 100);
        step1.AddTransition("ok", "step2");
        step1.AddTransition("timeout", "step2"); // 超时也继续
        step1.AddTransition("err:*", "step2"); // 错误也继续（简化测试）
        plan.Nodes.Add(step1);

        // 步骤2: OpenMenu（5 ticks）
        var step2 = Node.CreateToolCall("step2", "OpenMenu", new Dictionary<string, object>
        {
            { "menu", "shop" }
        }, timeout: 50);
        step2.AddTransition("ok", "step3");
        step2.AddTransition("err:*", "done"); // 错误也完成（简化测试）
        plan.Nodes.Add(step2);

        // 步骤3: ShopBuy（10 ticks）
        var step3 = Node.CreateToolCall("step3", "ShopBuy", new Dictionary<string, object>
        {
            { "item", "ParsnipSeeds" },
            { "count", 10 }
        }, timeout: 50);
        step3.AddTransition("ok", "done");
        step3.AddTransition("err:*", "done"); // 错误也完成（简化测试）
        plan.Nodes.Add(step3);

        // 完成节点
        var done = Node.CreateFinishGoal("done");
        plan.Nodes.Add(done);

        return plan;
    }
}
