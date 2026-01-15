using Core.Models;
using Executor.TransitionMatcher;

namespace Executor.TransitionMatcher;

class TransitionMatcherTest
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== TransitionMatcher 测试开始 ===\n");

        int passed = 0;
        int failed = 0;

        // 测试1: "ok" 表达式 - 成功情况
        Console.WriteLine("测试1: 'ok' 表达式 - 成功情况");
        var transitions1 = new List<Transition>
        {
            new Transition { Expression = "ok", TargetNodeId = "next_success" },
            new Transition { Expression = "err:*", TargetNodeId = "next_error" }
        };
        var result1 = ToolResult.Success("node1", "TestTool");
        var matched1 = TransitionMatcher.Match(transitions1, result1, isTimeout: false);
        if (matched1 == "next_success")
        {
            Console.WriteLine("  ✓ 通过: 匹配到 'next_success'");
            passed++;
        }
        else
        {
            Console.WriteLine($"  ✗ 失败: 期望 'next_success'，实际 '{matched1}'");
            failed++;
        }

        // 测试2: "timeout" 表达式 - 超时情况
        Console.WriteLine("\n测试2: 'timeout' 表达式 - 超时情况");
        var transitions2 = new List<Transition>
        {
            new Transition { Expression = "ok", TargetNodeId = "next_success" },
            new Transition { Expression = "timeout", TargetNodeId = "next_timeout" },
            new Transition { Expression = "err:*", TargetNodeId = "next_error" }
        };
        var result2 = ToolResult.Success("node2", "TestTool");
        var matched2 = TransitionMatcher.Match(transitions2, result2, isTimeout: true);
        if (matched2 == "next_timeout")
        {
            Console.WriteLine("  ✓ 通过: 匹配到 'next_timeout'");
            passed++;
        }
        else
        {
            Console.WriteLine($"  ✗ 失败: 期望 'next_timeout'，实际 '{matched2}'");
            failed++;
        }

        // 测试3: "err:*" 表达式 - 任意错误
        Console.WriteLine("\n测试3: 'err:*' 表达式 - 任意错误");
        var transitions3 = new List<Transition>
        {
            new Transition { Expression = "ok", TargetNodeId = "next_success" },
            new Transition { Expression = "err:*", TargetNodeId = "next_error" }
        };
        var result3 = ToolResult.Failure("node3", "TestTool", "unreachable", "无法到达目标位置");
        var matched3 = TransitionMatcher.Match(transitions3, result3, isTimeout: false);
        if (matched3 == "next_error")
        {
            Console.WriteLine("  ✓ 通过: 匹配到 'next_error'");
            passed++;
        }
        else
        {
            Console.WriteLine($"  ✗ 失败: 期望 'next_error'，实际 '{matched3}'");
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
