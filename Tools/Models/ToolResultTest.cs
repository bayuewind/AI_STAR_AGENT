using System.Text.Json;
using Tools.Models;

namespace Tools.Models;

/// <summary>
/// ToolResult 验收测试
/// 验收用例：
/// - Success → Ok=true, Error=null
/// - Fail(menu_not_open) → Ok=false, Error.Code=menu_not_open
/// - Timeout → Ok=false, Error.Code=timeout
/// </summary>
public static class ToolResultTest
{
    /// <summary>
    /// 运行所有验收测试
    /// </summary>
    public static void RunTests()
    {
        Console.WriteLine("=== ToolResult 验收测试 ===\n");

        // 测试1: Success → Ok=true, Error=null
        Console.WriteLine("✓ 测试1: Success 结果");
        var successResult = ToolResult.Success("node_001", "NavigateTo", new Dictionary<string, object>
        {
            { "location", "Farm" },
            { "tile", new { x = 10, y = 20 } }
        });
        Console.WriteLine($"  ActionNodeId: {successResult.ActionNodeId}");
        Console.WriteLine($"  Tool: {successResult.Tool}");
        Console.WriteLine($"  Ok: {successResult.Ok}");
        Console.WriteLine($"  Error: {(successResult.Error == null ? "null ✓" : "not null ✗")}");
        Console.WriteLine($"  Telemetry: {(successResult.Telemetry != null ? $"有数据 ({successResult.Telemetry.Count}项) ✓" : "null")}");

        // 测试2: Fail(menu_not_open) → Ok=false, Error.Code=menu_not_open
        Console.WriteLine("\n✓ 测试2: Fail 结果（menu_not_open）");
        var failResult = ToolResult.Fail("node_002", "ShopBuy", ErrorCodes.MenuNotOpen, "Shop menu is not open");
        Console.WriteLine($"  ActionNodeId: {failResult.ActionNodeId}");
        Console.WriteLine($"  Tool: {failResult.Tool}");
        Console.WriteLine($"  Ok: {failResult.Ok}");
        Console.WriteLine($"  Error.Code: {failResult.Error?.Code}");
        Console.WriteLine($"  Error.Detail: {failResult.Error?.Detail}");
        Console.WriteLine($"  验证: Ok={failResult.Ok}, Error.Code={failResult.Error?.Code} {(failResult.Ok == false && failResult.Error?.Code == ErrorCodes.MenuNotOpen ? "✓" : "✗")}");

        // 测试3: Timeout → Ok=false, Error.Code=timeout
        Console.WriteLine("\n✓ 测试3: Timeout 结果");
        var timeoutResult = ToolResult.Timeout("node_003", "NavigateTo", new Dictionary<string, object>
        {
            { "time", "Spring 5 10:58" }
        });
        Console.WriteLine($"  ActionNodeId: {timeoutResult.ActionNodeId}");
        Console.WriteLine($"  Tool: {timeoutResult.Tool}");
        Console.WriteLine($"  Ok: {timeoutResult.Ok}");
        Console.WriteLine($"  Error.Code: {timeoutResult.Error?.Code}");
        Console.WriteLine($"  Error.Detail: {timeoutResult.Error?.Detail}");
        Console.WriteLine($"  验证: Ok={timeoutResult.Ok}, Error.Code={timeoutResult.Error?.Code} {(timeoutResult.Ok == false && timeoutResult.Error?.Code == ErrorCodes.Timeout ? "✓" : "✗")}");

        // 测试4: JSON 序列化（可选）
        Console.WriteLine("\n✓ 测试4: JSON 序列化");
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var successJson = JsonSerializer.Serialize(successResult, jsonOptions);
        Console.WriteLine("  Success JSON:");
        Console.WriteLine(successJson);

        var failJson = JsonSerializer.Serialize(failResult, jsonOptions);
        Console.WriteLine("\n  Fail JSON:");
        Console.WriteLine(failJson);

        var timeoutJson = JsonSerializer.Serialize(timeoutResult, jsonOptions);
        Console.WriteLine("\n  Timeout JSON:");
        Console.WriteLine(timeoutJson);

        Console.WriteLine("\n=== ✅ ToolResult 验收测试完成 ===");
    }
}
