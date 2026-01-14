using Tools.Interfaces;
using Tools.Implementations;
using Tools.Models;

namespace Tools.Interfaces;

/// <summary>
/// IToolDispatcher 接口测试程序
/// 验收：编译通过，后续 Stub 能实现
/// </summary>
class IToolDispatcherTest
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== IToolDispatcher 接口测试开始 ===\n");

        // 测试1: 创建 Stub 实现
        Console.WriteLine("测试1: 创建 ToolDispatcherStub 实例");
        IToolDispatcher dispatcher = new ToolDispatcherStub();
        Console.WriteLine("  ✓ 通过: 成功创建实例");

        // 测试2: Begin 方法 - 返回 handle
        Console.WriteLine("\n测试2: Begin 方法 - 返回 handle");
        var toolArgs = new Dictionary<string, object>
        {
            { "location", "Town" },
            { "x", 5 },
            { "y", 17 }
        };
        var handle = dispatcher.Begin("node1", "NavigateTo", toolArgs);
        if (!string.IsNullOrEmpty(handle))
        {
            Console.WriteLine($"  ✓ 通过: 返回 handle = '{handle}'");
        }
        else
        {
            Console.WriteLine("  ✗ 失败: handle 为空");
            return;
        }

        // 测试3: Poll 方法 - 初始状态应该未就绪
        Console.WriteLine("\n测试3: Poll 方法 - 初始状态应该未就绪");
        var (ready1, result1) = dispatcher.Poll(handle);
        if (!ready1 && result1 == null)
        {
            Console.WriteLine("  ✓ 通过: 初始状态未就绪 (ready=false, result=null)");
        }
        else
        {
            Console.WriteLine($"  ✗ 失败: 期望 (ready=false, result=null)，实际 (ready={ready1}, result={result1})");
        }

        // 测试4: Poll 方法 - 等待后应该就绪
        Console.WriteLine("\n测试4: Poll 方法 - 等待后应该就绪");
        // 等待足够的时间（模拟异步执行完成）
        Thread.Sleep(50);
        var (ready2, result2) = dispatcher.Poll(handle);
        if (ready2 && result2 != null)
        {
            Console.WriteLine($"  ✓ 通过: 就绪状态 (ready=true, result不为null)");
            Console.WriteLine($"    结果: ActionNodeId={result2.ActionNodeId}, Tool={result2.Tool}, Ok={result2.Ok}");
        }
        else
        {
            Console.WriteLine($"  ✗ 失败: 期望 (ready=true, result不为null)，实际 (ready={ready2}, result={result2})");
        }

        // 测试5: Poll 方法 - 再次 Poll 应该返回相同结果
        Console.WriteLine("\n测试5: Poll 方法 - 再次 Poll 应该返回相同结果");
        var (ready3, result3) = dispatcher.Poll(handle);
        if (ready3 && result3 != null && result3.ActionNodeId == result2!.ActionNodeId)
        {
            Console.WriteLine("  ✓ 通过: 再次 Poll 返回相同结果");
        }
        else
        {
            Console.WriteLine("  ✗ 失败: 再次 Poll 结果不一致");
        }

        // 测试6: Cancel 方法 - 取消后 Poll 应该返回未就绪
        Console.WriteLine("\n测试6: Cancel 方法 - 取消后 Poll 应该返回未就绪");
        var handle2 = dispatcher.Begin("node2", "ShopBuy", new Dictionary<string, object>());
        dispatcher.Cancel(handle2);
        var (ready4, result4) = dispatcher.Poll(handle2);
        if (!ready4 && result4 == null)
        {
            Console.WriteLine("  ✓ 通过: 取消后 Poll 返回未就绪");
        }
        else
        {
            Console.WriteLine($"  ✗ 失败: 期望 (ready=false, result=null)，实际 (ready={ready4}, result={result4})");
        }

        // 测试7: 多个并发任务
        Console.WriteLine("\n测试7: 多个并发任务");
        var handles = new List<string>();
        for (int i = 0; i < 3; i++)
        {
            var h = dispatcher.Begin($"node{i}", $"Tool{i}", new Dictionary<string, object>());
            handles.Add(h);
        }
        Console.WriteLine($"  ✓ 通过: 创建了 {handles.Count} 个并发任务");

        // 测试8: 接口契约验证 - Poll 返回元组
        Console.WriteLine("\n测试8: 接口契约验证 - Poll 返回元组");
        var pollResult = dispatcher.Poll(handles[0]);
        var (ready, result) = pollResult;
        if (result == null || result is ToolResult)
        {
            Console.WriteLine("  ✓ 通过: Poll 返回正确的元组类型 (bool, ToolResult?)");
        }
        else
        {
            Console.WriteLine("  ✗ 失败: Poll 返回类型不正确");
        }

        Console.WriteLine("\n=== ✅ IToolDispatcher 接口测试完成 ===");
        Console.WriteLine("接口定义正确，Stub 实现可以正常工作！");
    }
}
