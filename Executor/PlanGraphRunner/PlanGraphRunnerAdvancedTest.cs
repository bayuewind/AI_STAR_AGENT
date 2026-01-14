using Agent.PlanGraphModels;
using Executor.PlanGraphRunner;
using Tools.Implementations;

namespace Executor.PlanGraphRunner;

/// <summary>
/// PlanGraphRunner 高级功能测试
/// 测试超时 Cancel 和 ReplacePlan 功能
/// </summary>
class PlanGraphRunnerAdvancedTest
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== PlanGraphRunner 高级功能测试 ===\n");

        TestTimeoutCancellation();
        Console.WriteLine("\n" + new string('-', 60) + "\n");
        TestReplacePlan();

        Console.WriteLine("\n=== 所有高级功能测试完成 ===");
    }

    /// <summary>
    /// 测试1: 超时后自动 Cancel
    /// </summary>
    static void TestTimeoutCancellation()
    {
        Console.WriteLine("测试1: 超时后自动 Cancel\n");

        var plan = new PlanGraph
        {
            PlanId = "plan_timeout_test",
            StartNodeId = "step1"
        };

        // 创建一个会超时的节点（超时时间 20 ticks，但 NavigateTo 需要 60 ticks）
        var step1 = Node.CreateToolCall("step1", "NavigateTo", new Dictionary<string, object>
        {
            { "location", "Town" }
        }, timeout: 20);
        step1.AddTransition("ok", "done");
        step1.AddTransition("timeout", "step2"); // 超时后跳转到 step2
        plan.Nodes.Add(step1);

        var step2 = Node.CreateToolCall("step2", "OpenMenu", new Dictionary<string, object>(), timeout: 50);
        step2.AddTransition("ok", "done");
        plan.Nodes.Add(step2);

        var done = Node.CreateFinishGoal("done");
        plan.Nodes.Add(done);

        var toolDispatcher = new ToolDispatcherStub();
        var runner = new PlanGraphRunner(plan, toolDispatcher);

        Console.WriteLine("开始执行...");
        int tickId = 0;
        int maxTicks = 100;

        while (tickId < maxTicks)
        {
            var status = runner.Tick(tickId);

            if (status == RunnerStatus.Done)
            {
                Console.WriteLine($"\n✅ 测试通过：超时后正确 Cancel 并跳转到 step2，最终完成");
                break;
            }
            else if (status == RunnerStatus.NeedReplan)
            {
                Console.WriteLine($"\n❌ 测试失败：不应该进入 NeedReplan");
                break;
            }

            tickId++;
        }

        if (tickId >= maxTicks)
        {
            Console.WriteLine($"\n⚠️ 达到最大 tick 数");
        }
    }

    /// <summary>
    /// 测试2: ReplacePlan 功能
    /// </summary>
    static void TestReplacePlan()
    {
        Console.WriteLine("测试2: ReplacePlan 功能\n");

        // 创建初始计划
        var plan1 = new PlanGraph
        {
            PlanId = "plan_original",
            StartNodeId = "step1"
        };
        var step1 = Node.CreateToolCall("step1", "NavigateTo", new Dictionary<string, object>(), timeout: 100);
        step1.AddTransition("ok", "done");
        plan1.Nodes.Add(step1);
        plan1.Nodes.Add(Node.CreateFinishGoal("done"));

        var toolDispatcher = new ToolDispatcherStub();
        var runner = new PlanGraphRunner(plan1, toolDispatcher);

        Console.WriteLine($"初始计划: {plan1.PlanId}, 起始节点: {plan1.StartNodeId}");

        // 开始执行
        var status1 = runner.Tick(0);
        Console.WriteLine($"Tick 0: 状态 = {status1}");

        // 在执行过程中替换计划
        Console.WriteLine("\n执行到一半，触发 ReplacePlan...");
        var plan2 = new PlanGraph
        {
            PlanId = "plan_new",
            StartNodeId = "new_step1"
        };
        var newStep1 = Node.CreateToolCall("new_step1", "OpenMenu", new Dictionary<string, object>(), timeout: 50);
        newStep1.AddTransition("ok", "new_done");
        plan2.Nodes.Add(newStep1);
        plan2.Nodes.Add(Node.CreateFinishGoal("new_done"));

        runner.ReplacePlan(plan2, tickId: 1);
        Console.WriteLine($"新计划: {plan2.PlanId}, 起始节点: {plan2.StartNodeId}");

        // 继续执行新计划
        Console.WriteLine("\n继续执行新计划...");
        int tickId = 1;
        int maxTicks = 50;

        while (tickId < maxTicks)
        {
            var status = runner.Tick(tickId);

            if (status == RunnerStatus.Done)
            {
                Console.WriteLine($"\n✅ 测试通过：ReplacePlan 成功，新计划正常执行并完成");
                break;
            }
            else if (status == RunnerStatus.NeedReplan)
            {
                Console.WriteLine($"\n❌ 测试失败：不应该进入 NeedReplan");
                break;
            }

            tickId++;
        }

        if (tickId >= maxTicks)
        {
            Console.WriteLine($"\n⚠️ 达到最大 tick 数");
        }
    }
}
