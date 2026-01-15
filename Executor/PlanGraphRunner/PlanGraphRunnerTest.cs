using Core.Models;
using Executor.PlanGraphRunner;
using Tools.Implementations;
using Core.Interfaces;

namespace Executor.PlanGraphRunner;

class PlanGraphRunnerTest
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== PlanGraphRunner 测试开始 ===\n");

        var planGraph = CreateTestPlanGraph();
        Console.WriteLine($"计划图: {planGraph.PlanId}");
        Console.WriteLine($"起始节点: {planGraph.StartNodeId}");
        Console.WriteLine($"节点数量: {planGraph.Nodes.Count}");
        Console.WriteLine($"验证结果: {(planGraph.Validate() ? "✓ 通过" : "✗ 失败")}\n");

        var toolDispatcher = new ToolDispatcherStub();
        var runner = new PlanGraphRunner(planGraph, toolDispatcher);

        Console.WriteLine("=== 开始执行计划图 ===\n");
        int tickId = 0;
        int maxTicks = 200;

        while (tickId < maxTicks)
        {
            var status = runner.Tick(tickId);

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

    static PlanGraph CreateTestPlanGraph()
    {
        var plan = new PlanGraph
        {
            PlanId = "plan_test_001",
            StartNodeId = "step1"
        };

        var step1 = Node.CreateToolCall("step1", "NavigateTo", new Dictionary<string, object> { { "location", "Town" } }, timeout: 100);
        step1.AddTransition("ok", "step2");
        step1.AddTransition("timeout", "step2");
        step1.AddTransition("err:*", "step2");
        plan.Nodes.Add(step1);

        var step2 = Node.CreateToolCall("step2", "OpenMenu", new Dictionary<string, object> { { "menu", "shop" } }, timeout: 50);
        step2.AddTransition("ok", "step3");
        step2.AddTransition("err:*", "done");
        plan.Nodes.Add(step2);

        var step3 = Node.CreateToolCall("step3", "ShopBuy", new Dictionary<string, object> { { "item", "ParsnipSeeds" }, { "count", 10 } }, timeout: 50);
        step3.AddTransition("ok", "done");
        step3.AddTransition("err:*", "done");
        plan.Nodes.Add(step3);

        plan.Nodes.Add(Node.CreateFinishGoal("done"));

        return plan;
    }
}
