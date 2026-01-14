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
        var handle1 = dispatcher.Begin("node_nav", "NavigateTo", new Dictionary<string, object> { { "location", "Town" } });
        Console.WriteLine($"  开始 tick: {dispatcher.CurrentTick}, handle: {handle1}");
        
        bool navReady = false;
        ToolResult? navResult = null;
        for (int tick = 0; tick < 70; tick++)
        {
            dispatcher.Tick();
            var (ready, result) = dispatcher.Poll(handle1);
            if (ready && !navReady)
            {
                navReady = true;
                navResult = result;
                Console.WriteLine($"  ✓ 在第 {tick + 1} tick 就绪（期望 60 ticks）");
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
        var handle2 = dispatcher.Begin("node_buy", "ShopBuy", new Dictionary<string, object> { { "item", "ParsnipSeeds" }, { "count", 10 } });
        Console.WriteLine($"  开始 tick: {dispatcher.CurrentTick}, handle: {handle2}");
        
        bool buyReady = false;
        ToolResult? buyResult = null;
        int startTick2 = dispatcher.CurrentTick;
        for (int tick = 0; tick < 20; tick++)
        {
            dispatcher.Tick();
            var (ready, result) = dispatcher.Poll(handle2);
            if (ready && !buyReady)
            {
                buyReady = true;
                buyResult = result;
                int elapsed = dispatcher.CurrentTick - startTick2;
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
        var handle3 = dispatcher.Begin("node_buy_error", "ShopBuy", new Dictionary<string, object>());
        Console.WriteLine($"  开始 tick: {dispatcher.CurrentTick}, handle: {handle3}");
        
        bool buyErrorReady = false;
        ToolResult? buyErrorResult = null;
        int startTick3 = dispatcher.CurrentTick;
        for (int tick = 0; tick < 20; tick++)
        {
            dispatcher.Tick();
            var (ready, result) = dispatcher.Poll(handle3);
            if (ready && !buyErrorReady)
            {
                buyErrorReady = true;
                buyErrorResult = result;
                int elapsed = dispatcher.CurrentTick - startTick3;
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

        // 测试4: ShopBuy - 配置错误情况（insufficient_gold）
        Console.WriteLine("\n测试4: ShopBuy - 配置错误情况（insufficient_gold）");
        dispatcher.ConfigureShopBuyResult(ok: false, errorCode: "insufficient_gold", errorDetail: "Not enough gold");
        var handle4 = dispatcher.Begin("node_buy_gold", "ShopBuy", new Dictionary<string, object>());
        Console.WriteLine($"  开始 tick: {dispatcher.CurrentTick}, handle: {handle4}");
        
        bool buyGoldReady = false;
        ToolResult? buyGoldResult = null;
        int startTick4 = dispatcher.CurrentTick;
        for (int tick = 0; tick < 20; tick++)
        {
            dispatcher.Tick();
            var (ready, result) = dispatcher.Poll(handle4);
            if (ready && !buyGoldReady)
            {
                buyGoldReady = true;
                buyGoldResult = result;
                int elapsed = dispatcher.CurrentTick - startTick4;
                Console.WriteLine($"  ✓ 在第 {elapsed} tick 就绪（期望 10 ticks）");
                if (result != null && !result.Ok && result.Error != null && result.Error.Code == "insufficient_gold")
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
        if (!buyGoldReady)
        {
            Console.WriteLine("  ✗ 10 ticks 后仍未就绪");
            failed++;
        }

        // 测试5: 其它工具 - 5 ticks 后 ok
        Console.WriteLine("\n测试5: 其它工具（OpenMenu）- 5 ticks 后 ok");
        dispatcher.ResetShopBuyConfig();
        var handle5 = dispatcher.Begin("node_open", "OpenMenu", new Dictionary<string, object> { { "menu", "shop" } });
        Console.WriteLine($"  开始 tick: {dispatcher.CurrentTick}, handle: {handle5}");
        
        bool otherReady = false;
        ToolResult? otherResult = null;
        int startTick5 = dispatcher.CurrentTick;
        for (int tick = 0; tick < 15; tick++)
        {
            dispatcher.Tick();
            var (ready, result) = dispatcher.Poll(handle5);
            if (ready && !otherReady)
            {
                otherReady = true;
                otherResult = result;
                int elapsed = dispatcher.CurrentTick - startTick5;
                Console.WriteLine($"  ✓ 在第 {elapsed} tick 就绪（期望 5 ticks）");
                if (result != null && result.Ok && result.Tool == "OpenMenu")
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
        if (!otherReady)
        {
            Console.WriteLine("  ✗ 5 ticks 后仍未就绪");
            failed++;
        }

        // 测试6: 多个并发任务
        Console.WriteLine("\n测试6: 多个并发任务");
        dispatcher.ResetShopBuyConfig();
        var handles = new List<string>
        {
            dispatcher.Begin("node1", "NavigateTo", new Dictionary<string, object>()),
            dispatcher.Begin("node2", "ShopBuy", new Dictionary<string, object>()),
            dispatcher.Begin("node3", "OpenMenu", new Dictionary<string, object>())
        };
        Console.WriteLine($"  创建了 3 个任务: NavigateTo(60), ShopBuy(10), OpenMenu(5)");
        
        var completed = new HashSet<string>();
        int maxTicks = 70;
        for (int tick = 0; tick < maxTicks; tick++)
        {
            dispatcher.Tick();
            foreach (var handle in handles)
            {
                if (!completed.Contains(handle))
                {
                    var (ready, result) = dispatcher.Poll(handle);
                    if (ready && result != null)
                    {
                        completed.Add(handle);
                        Console.WriteLine($"  Tick {dispatcher.CurrentTick}: {result.Tool} 完成 (Ok={result.Ok})");
                    }
                }
            }
            if (completed.Count == handles.Count)
            {
                break;
            }
        }
        if (completed.Count == handles.Count)
        {
            Console.WriteLine("  ✓ 所有任务都完成了");
            passed++;
        }
        else
        {
            Console.WriteLine($"  ✗ 只有 {completed.Count}/{handles.Count} 个任务完成");
            failed++;
        }

        // 测试7: Poll 在未就绪时返回 (false, null)
        Console.WriteLine("\n测试7: Poll 在未就绪时返回 (false, null)");
        dispatcher.ResetShopBuyConfig();
        var handle7 = dispatcher.Begin("node_test", "NavigateTo", new Dictionary<string, object>());
        var (ready7, result7) = dispatcher.Poll(handle7);
        if (!ready7 && result7 == null)
        {
            Console.WriteLine("  ✓ 未就绪时返回 (false, null)");
            passed++;
        }
        else
        {
            Console.WriteLine($"  ✗ 期望 (false, null)，实际 (ready={ready7}, result={result7})");
            failed++;
        }

        // 总结
        Console.WriteLine("\n=== 测试总结 ===");
        Console.WriteLine($"通过: {passed} 个");
        Console.WriteLine($"失败: {failed} 个");
        Console.WriteLine($"总计: {passed + failed} 个");

        if (failed == 0)
        {
            Console.WriteLine("\n✅ 所有测试通过！ToolDispatcherStub 能正确延迟几 tick 返回结果。");
        }
        else
        {
            Console.WriteLine($"\n❌ 有 {failed} 个测试失败，请检查实现。");
        }
    }
}
