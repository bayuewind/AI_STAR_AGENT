using Agent.PlanGraphModels;

namespace Agent.PlanGraphModels;

/// <summary>
/// PlanGraph 测试验证
/// 验收：你能在代码里 new 一个 PlanGraph，包含 nodes & transitions，不报错
/// </summary>
public static class PlanGraphTest
{
    /// <summary>
    /// 测试创建和验证 PlanGraph
    /// </summary>
    public static void RunTests()
    {
        Console.WriteLine("=== PlanGraph 测试开始 ===");

        // 测试1: 创建最小示例
        Console.WriteLine("\n测试1: 创建最小示例 PlanGraph");
        var minimalPlan = PlanGraphExample.CreateMinimalExample();
        Console.WriteLine($"PlanId: {minimalPlan.PlanId}");
        Console.WriteLine($"StartNodeId: {minimalPlan.StartNodeId}");
        Console.WriteLine($"Nodes数量: {minimalPlan.Nodes.Count}");
        Console.WriteLine($"验证结果: {(minimalPlan.Validate() ? "✓ 通过" : "✗ 失败")}");

        // 测试2: 创建完整示例
        Console.WriteLine("\n测试2: 创建完整示例 PlanGraph（买种子）");
        var buySeedsPlan = PlanGraphExample.CreateBuySeedsExample();
        Console.WriteLine($"PlanId: {buySeedsPlan.PlanId}");
        Console.WriteLine($"StartNodeId: {buySeedsPlan.StartNodeId}");
        Console.WriteLine($"Nodes数量: {buySeedsPlan.Nodes.Count}");
        Console.WriteLine($"验证结果: {(buySeedsPlan.Validate() ? "✓ 通过" : "✗ 失败")}");

        // 测试3: 手动创建 PlanGraph
        Console.WriteLine("\n测试3: 手动创建 PlanGraph");
        var manualPlan = new PlanGraph
        {
            PlanId = "plan_manual_001",
            StartNodeId = "step1"
        };

        var step1 = Node.CreateToolCall("step1", "NavigateTo", new Dictionary<string, object>
        {
            { "location", "Town" }
        }, timeout: 1000);
        step1.AddTransition("ok", "step2");
        step1.AddTransition("err:*", "step3");
        manualPlan.Nodes.Add(step1);

        var step2 = Node.CreateFinishGoal("step2");
        manualPlan.Nodes.Add(step2);

        var step3 = Node.CreateLlmReplan("step3");
        manualPlan.Nodes.Add(step3);

        Console.WriteLine($"PlanId: {manualPlan.PlanId}");
        Console.WriteLine($"Nodes数量: {manualPlan.Nodes.Count}");
        Console.WriteLine($"验证结果: {(manualPlan.Validate() ? "✓ 通过" : "✗ 失败")}");

        // 测试4: 验证节点查找
        Console.WriteLine("\n测试4: 验证节点查找功能");
        var foundNode = buySeedsPlan.FindNode("a_buy");
        if (foundNode != null)
        {
            Console.WriteLine($"找到节点: {foundNode.NodeId}, 类型: {foundNode.Type}, 工具: {foundNode.Tool}");
            Console.WriteLine($"转移条件数量: {foundNode.Transitions.Count}");
        }
        else
        {
            Console.WriteLine("✗ 未找到节点");
        }

        // 测试5: 验证转移表达式
        Console.WriteLine("\n测试5: 验证转移表达式");
        var testNode = Node.CreateToolCall("test", "TestTool");
        testNode.AddTransition("ok", "next1");
        testNode.AddTransition("timeout", "next2");
        testNode.AddTransition("err:*", "next3");
        testNode.AddTransition("err:code1|err:code2", "next4");
        testNode.AddTransition("ok|err:*|timeout", "next5");

        Console.WriteLine($"转移条件数量: {testNode.Transitions.Count}");
        foreach (var transition in testNode.Transitions)
        {
            Console.WriteLine($"  - {transition.Expression} -> {transition.TargetNodeId}");
        }

        Console.WriteLine("\n=== PlanGraph 测试完成 ===");
    }
}
