using System.Text.Json;
using Agent.DecisionProtocol;
using Agent.GoalModels;
using Agent.PlanGraphModels;

namespace Agent.DecisionProtocol;

/// <summary>
/// DecisionResponse 验收测试
/// 验收：能把 DecisionResponse 序列化为 JSON（或至少能构造对象）
/// </summary>
public static class DecisionResponseTest
{
    /// <summary>
    /// 运行所有验收测试
    /// </summary>
    public static void RunTests()
    {
        Console.WriteLine("=== DecisionResponse 验收测试 ===\n");

        // 测试1: 构造基本 DecisionResponse 对象
        Console.WriteLine("✓ 测试1: 构造 DecisionResponse 对象");
        var response = new DecisionResponse
        {
            ProtocolVersion = "agent-json-v1",
            TickId = 100,
            AgentId = "player_1",
            Mode = "act",
            Goal = new Goal
            {
                Id = "goal_001",
                Text = "去皮埃尔买10个防风草种子并回农场",
                Priority = 1,
                DeadlineGameTime = "Spring 5 18:00"
            },
            PlanGraph = PlanGraphExample.CreateBuySeedsExample(),
            Constraints = new Constraints
            {
                MaxReplans = 3,
                MaxTotalTicks = 4000,
                MaxGoldSpend = 400
            },
            MemoryWrite = new List<MemoryWriteItem>()
        };

        Console.WriteLine($"  ProtocolVersion: {response.ProtocolVersion}");
        Console.WriteLine($"  TickId: {response.TickId}");
        Console.WriteLine($"  AgentId: {response.AgentId}");
        Console.WriteLine($"  Mode: {response.Mode}");
        Console.WriteLine($"  Goal.Id: {response.Goal.Id}");
        Console.WriteLine($"  Goal.Text: {response.Goal.Text}");
        Console.WriteLine($"  PlanGraph.PlanId: {response.PlanGraph.PlanId}");
        Console.WriteLine($"  PlanGraph.Nodes.Count: {response.PlanGraph.Nodes.Count}");
        Console.WriteLine($"  Constraints.MaxReplans: {response.Constraints.MaxReplans}");
        Console.WriteLine($"  MemoryWrite.Count: {response.MemoryWrite.Count}");

        // 测试2: JSON 序列化
        Console.WriteLine("\n✓ 测试2: JSON 序列化");
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(response, jsonOptions);
        Console.WriteLine("  JSON 输出:");
        Console.WriteLine(json);

        // 测试3: JSON 反序列化
        Console.WriteLine("\n✓ 测试3: JSON 反序列化");
        var deserialized = JsonSerializer.Deserialize<DecisionResponse>(json, jsonOptions);
        if (deserialized != null)
        {
            Console.WriteLine($"  反序列化成功:");
            Console.WriteLine($"    ProtocolVersion: {deserialized.ProtocolVersion}");
            Console.WriteLine($"    TickId: {deserialized.TickId}");
            Console.WriteLine($"    AgentId: {deserialized.AgentId}");
            Console.WriteLine($"    Mode: {deserialized.Mode}");
            Console.WriteLine($"    Goal.Id: {deserialized.Goal.Id}");
            Console.WriteLine($"    PlanGraph.PlanId: {deserialized.PlanGraph.PlanId}");
            Console.WriteLine($"    PlanGraph.Nodes.Count: {deserialized.PlanGraph.Nodes.Count}");
        }
        else
        {
            Console.WriteLine("  ✗ 反序列化失败");
        }

        // 测试4: 包含 MemoryWrite 的完整示例
        Console.WriteLine("\n✓ 测试4: 包含 MemoryWrite 的完整示例");
        var responseWithMemory = new DecisionResponse
        {
            ProtocolVersion = "agent-json-v1",
            TickId = 200,
            AgentId = "player_1",
            Mode = "act",
            Goal = new Goal
            {
                Id = "goal_002",
                Text = "完成日常任务",
                Priority = 2
            },
            PlanGraph = PlanGraphExample.CreateMinimalExample(),
            Constraints = new Constraints
            {
                MaxReplans = 5,
                MaxTotalTicks = 5000,
                MaxGoldSpend = 1000
            },
            MemoryWrite = new List<MemoryWriteItem>
            {
                new MemoryWriteItem
                {
                    Type = "fact",
                    Content = "皮埃尔的商店在上午9点开门",
                    Keys = new List<string> { "Pierre", "shop", "hours" }
                },
                new MemoryWriteItem
                {
                    Type = "preference",
                    Content = "优先购买防风草种子",
                    Keys = new List<string> { "seeds", "preference" }
                }
            }
        };

        var jsonWithMemory = JsonSerializer.Serialize(responseWithMemory, jsonOptions);
        Console.WriteLine("  包含 MemoryWrite 的 JSON:");
        Console.WriteLine(jsonWithMemory);

        // 测试5: 不同 Mode 值
        Console.WriteLine("\n✓ 测试5: 验证不同 Mode 值");
        var modes = new[] { "observe", "plan", "act", "recover" };
        foreach (var mode in modes)
        {
            var testResponse = new DecisionResponse
            {
                Mode = mode,
                Goal = new Goal { Id = $"goal_{mode}", Text = $"Test goal for {mode}" },
                PlanGraph = PlanGraphExample.CreateMinimalExample()
            };
            Console.WriteLine($"  Mode={mode}: ✓");
        }

        Console.WriteLine("\n=== ✅ DecisionResponse 验收测试完成 ===");
    }
}
