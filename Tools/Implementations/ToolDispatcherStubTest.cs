using Core.Models;
using Tools.Implementations;

namespace Tools.Implementations;

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
        dispatcher.ResetToolConfig("ShopBuy"); 
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
        dispatcher.ConfigureToolResult("ShopBuy", ok: false, errorCode: "menu_not_open", errorDetail: "Shop menu is not open");
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

        // 总结
        Console.WriteLine("\n=== 测试总结 ===");
        Console.WriteLine($"通过: {passed} 个");
        Console.WriteLine($"失败: {failed} 个");
        
        if (failed == 0) Console.WriteLine("\n✅ 所有测试通过！");
        else Console.WriteLine($"\n❌ 有 {failed} 个测试失败。");
    }
}
