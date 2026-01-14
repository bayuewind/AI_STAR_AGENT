using Agent.PlanGraphModels;

namespace Agent.PlanGraphModels;

/// <summary>
/// PlanGraph 验证程序
/// 验收标准：能在代码里 new 一个 PlanGraph，包含 nodes & transitions，不报错
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Phase 0: PlanGraphModels 验证 ===\n");

        // 验收测试：创建 PlanGraph，包含 nodes & transitions
        Console.WriteLine("✓ 测试1: 创建最小 PlanGraph");
        var minimalPlan = PlanGraphExample.CreateMinimalExample();
        Console.WriteLine($"  PlanId: {minimalPlan.PlanId}");
        Console.WriteLine($"  StartNodeId: {minimalPlan.StartNodeId}");
        Console.WriteLine($"  Nodes数量: {minimalPlan.Nodes.Count}");
        Console.WriteLine($"  验证通过: {minimalPlan.Validate()}");

        Console.WriteLine("\n✓ 测试2: 创建完整 PlanGraph（买种子示例）");
        var buySeedsPlan = PlanGraphExample.CreateBuySeedsExample();
        Console.WriteLine($"  PlanId: {buySeedsPlan.PlanId}");
        Console.WriteLine($"  StartNodeId: {buySeedsPlan.StartNodeId}");
        Console.WriteLine($"  Nodes数量: {buySeedsPlan.Nodes.Count}");
        Console.WriteLine($"  验证通过: {buySeedsPlan.Validate()}");

        Console.WriteLine("\n✓ 测试3: 验证节点类型");
        foreach (var node in buySeedsPlan.Nodes)
        {
            Console.WriteLine($"  节点 {node.NodeId}: 类型={node.Type}, 工具={node.Tool}, 转移数={node.Transitions.Count}");
        }

        Console.WriteLine("\n✓ 测试4: 验证转移表达式");
        var testExpressions = new[] { "ok", "timeout", "err:*", "err:code1|err:code2", "ok|err:*|timeout" };
        foreach (var expr in testExpressions)
        {
            var testNode = Node.CreateToolCall("test", "TestTool");
            testNode.AddTransition(expr, "target");
            Console.WriteLine($"  表达式 '{expr}' -> 已添加转移条件");
        }

        Console.WriteLine("\n=== ✅ Phase 0 验收通过：PlanGraphModels 可以正常创建和使用 ===");
    }
}
