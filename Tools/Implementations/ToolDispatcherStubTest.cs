using Tools.Implementations;
using Tools.Models;

namespace Tools.Implementations;

/// <summary>
/// ToolDispatcherStub 测试程序
/// 验收：能在循环里每 tick poll，最终收到 ToolResult
/// </summary>
class ToolDispatcherStubTest
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== ToolDispatcherStub Tick 测试开始 ===\n");

        var dispatcher = new ToolDispatcherStub();
        int passed = 0;
        int failed = 0;

        // 测试1: NavigateTo - 60 ticks 后 ok
        Console.WriteLine("测试1: NavigateTo - 60 ticks 后 ok");
        int tickId = 0;
        var handle1 = dispatcher.Begin("node_nav", "NavigateTo", new Dictionary<string, object> { { "location", "Town" } }, tickId);
        Console.WriteLine($"  开始 tick: {tickId}, handle: {handle1}");
        
        bool navReady = false;
        for (int tick = tickId; tick < 70; tick++)
        {
            var (ready, result) = dispatcher.Poll(handle1, tick);
            if (ready && !navReady)
            {
                navReady = true;
                Console.WriteLine($"  ✓ 在第 {tick} tick 就绪（期望 60 ticks）");
                if (result != null && result.Ok && result.Tool == "NavigateTo")
                {
                    Console.WriteLine($"    结果: Ok={result.Ok}, Tool={result.Tool}");
                    passed++;
                }
                else
                {
                    Console.WriteLine($"    ✗ 结果不正确");
                    failed++;
                }
                break;
            }
        }
        if (!navReady)
        {
            Console.WriteLine("  ✗ 60 ticks 后仍未就绪");
            failed++;
        }

        // 测试2: ShopBuy - 10 ticks 后 ok（默认）
        Console.WriteLine("\n测试2: ShopBuy - 10 ticks 后 ok（默认）");
        dispatcher.ResetShopBuyConfig(); // 确保是默认配置
        tickId = 70;
        var handle2 = dispatcher.Begin("node_buy", "ShopBuy", new Dictionary<string, object> { { "item", "ParsnipSeeds" }, { "count", 10 } }, tickId);
        Console.WriteLine($"  开始 tick: {tickId}, handle: {handle2}");
        
        bool buyReady = false;
        for (int tick = tickId; tick < tickId + 20; tick++)
        {
            var (ready, result) = dispatcher.Poll(handle2, tick);
            if (ready && !buyReady)
            {
                buyReady = true;
                int elapsed = tick - tickId;
                Console.WriteLine($"  ✓ 在第 {elapsed} tick 就绪（期望 10 ticks）");
                if (result != null && result.Ok && result.Tool == "ShopBuy")
                {
                    Console.WriteLine($"    结果: Ok={result.Ok}, Tool={result.Tool}");
                    passed++;
                }
                else
                {
                    Console.WriteLine($"    ✗ 结果不正确");
                    failed++;
                }
                break;
            }
        }
        if (!buyReady)
        {
            Console.WriteLine("  ✗ 10 ticks 后仍未就绪");
            failed++;
        }

        // 测试3: ShopBuy - 配置错误情况（menu_not_open）
        Console.WriteLine("\n测试3: ShopBuy - 配置错误情况（menu_not_open）");
        dispatcher.ConfigureShopBuyResult(ok: false, errorCode: "menu_not_open", errorDetail: "Shop menu is not open");
        tickId = 100;
        var handle3 = dispatcher.Begin("node_buy_error", "ShopBuy", new Dictionary<string, object>(), tickId);
        Console.WriteLine($"  开始 tick: {tickId}, handle: {handle3}");
        
        bool buyErrorReady = false;
        for (int tick = tickId; tick < tickId + 20; tick++)
        {
            var (ready, result) = dispatcher.Poll(handle3, tick);
            if (ready && !buyErrorReady)
            {
                buyErrorReady = true;
                int elapsed = tick - tickId;
                Console.WriteLine($"  ✓ 在第 {elapsed} tick 就绪（期望 10 ticks）");
                if (result != null && !result.Ok && result.Error != null && result.Error.Code == "menu_not_open")
                {
                    Console.WriteLine($"    结果: Ok={result.Ok}, Error.Code={result.Error.Code}");
                    passed++;
                }
                else
                {
                    Console.WriteLine($"    ✗ 结果不正确");
                    failed++;
                }
                break;
            }
        }
        if (!buyErrorReady)
        {
            Console.WriteLine("  ✗ 10 ticks 后仍未就绪");
            failed++;
        }

        // 测试4: 通用配置器 - NavigateTo 配置为失败
        Console.WriteLine("\n测试4: 通用配置器 - NavigateTo 配置为失败（unreachable）");
        dispatcher.ConfigureToolResult("NavigateTo", ok: false, errorCode: "unreachable", errorDetail: "Target is unreachable");
        tickId = 120;
        var handle4 = dispatcher.Begin("node_nav_fail", "NavigateTo", new Dictionary<string, object>(), tickId);
        Console.WriteLine($"  开始 tick: {tickId}, handle: {handle4}");
        
        bool navFailReady = false;
        for (int tick = tickId; tick < tickId + 70; tick++)
        {
            var (ready, result) = dispatcher.Poll(handle4, tick);
            if (ready && !navFailReady)
            {
                navFailReady = true;
                int elapsed = tick - tickId;
                Console.WriteLine($"  ✓ 在第 {elapsed} tick 就绪（期望 60 ticks）");
                if (result != null && !result.Ok && result.Error != null && result.Error.Code == "unreachable")
                {
                    Console.WriteLine($"    结果: Ok={result.Ok}, Error.Code={result.Error.Code}");
                    passed++;
                }
                else
                {
                    Console.WriteLine($"    ✗ 结果不正确");
                    failed++;
                }
                break;
            }
        }
        if (!navFailReady)
        {
            Console.WriteLine("  ✗ 60 ticks 后仍未就绪");
            failed++;
        }

        // 测试5: 无效 handle - 应该返回 invalid_handle
        Console.WriteLine("\n测试5: 无效 handle - 应该返回 ready=true 且 error.code=invalid_handle");
        tickId = 200;
        var (ready5, result5) = dispatcher.Poll("invalid_handle_12345", tickId);
        if (ready5 && result5 != null && !result5.Ok && result5.Error != null && result5.Error.Code == "invalid_handle")
        {
            Console.WriteLine("  ✓ 通过: 无效 handle 返回 invalid_handle 错误");
            passed++;
        }
        else
        {
            Console.WriteLine($"  ✗ 失败: 期望 ready=true 且 error.code=invalid_handle");
            failed++;
        }

        // 测试6: Cancel 语义 - 应该返回 canceled
        Console.WriteLine("\n测试6: Cancel 语义 - 应该返回 ready=true 且 error.code=canceled");
        dispatcher.ResetToolConfig(); // 重置所有配置
        tickId = 210;
        var handle6 = dispatcher.Begin("node_cancel", "OpenMenu", new Dictionary<string, object>(), tickId);
        dispatcher.Cancel(handle6, tickId);
        var (ready6, result6) = dispatcher.Poll(handle6, tickId + 1);
        if (ready6 && result6 != null && !result6.Ok && result6.Error != null && result6.Error.Code == "canceled")
        {
            Console.WriteLine("  ✓ 通过: Cancel 后返回 canceled 错误");
            passed++;
        }
        else
        {
            Console.WriteLine($"  ✗ 失败: 期望 ready=true 且 error.code=canceled");
            failed++;
        }

        // 测试7: Telemetry 验证
        Console.WriteLine("\n测试7: Telemetry 验证 - 应该包含 tick/node_id/tool");
        dispatcher.ResetToolConfig();
        tickId = 220;
        var handle7 = dispatcher.Begin("node_tele", "TestTool", new Dictionary<string, object>(), tickId);
        for (int tick = tickId; tick < tickId + 10; tick++)
        {
            var (ready, result) = dispatcher.Poll(handle7, tick);
            if (ready && result != null)
            {
                if (result.Telemetry != null && 
                    result.Telemetry.ContainsKey("tick") && 
                    result.Telemetry.ContainsKey("node_id") && 
                    result.Telemetry.ContainsKey("tool"))
                {
                    Console.WriteLine($"  ✓ 通过: Telemetry 包含必要字段: tick={result.Telemetry["tick"]}, node_id={result.Telemetry["node_id"]}, tool={result.Telemetry["tool"]}");
                    passed++;
                }
                else
                {
                    Console.WriteLine("  ✗ 失败: Telemetry 缺少必要字段");
                    failed++;
                }
                break;
            }
        }

        // 总结
        Console.WriteLine("\n=== 测试总结 ===");
        Console.WriteLine($"通过: {passed} 个");
        Console.WriteLine($"失败: {failed} 个");
        Console.WriteLine($"总计: {passed + failed} 个");

        if (failed == 0)
        {
            Console.WriteLine("\n✅ 所有测试通过！ToolDispatcherStub 改进版工作正常。");
        }
        else
        {
            Console.WriteLine($"\n❌ 有 {failed} 个测试失败，请检查实现。");
        }
    }
}
