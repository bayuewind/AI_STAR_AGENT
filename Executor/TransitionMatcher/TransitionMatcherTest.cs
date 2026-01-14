using Agent.PlanGraphModels;
using Tools.Models;
using Executor.TransitionMatcher;

namespace Executor.TransitionMatcher;

/// <summary>
/// TransitionMatcher 测试程序
/// 验收：6-8 个测试用例，能正确匹配 next id
/// </summary>
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
        var result2 = ToolResult.Success("node2", "TestTool"); // 即使Ok=true，但isTimeout=true时应该匹配timeout
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
        var result3 = ToolResult.Fail("node3", "TestTool", "unreachable", "无法到达目标位置");
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

        // 测试4: "err:code1|err:code2" 表达式 - 多个错误码的逻辑或
        Console.WriteLine("\n测试4: 'err:code1|err:code2' 表达式 - 多个错误码的逻辑或");
        var transitions4 = new List<Transition>
        {
            new Transition { Expression = "ok", TargetNodeId = "next_success" },
            new Transition { Expression = "err:unreachable|err:warp_failed", TargetNodeId = "next_nav_error" },
            new Transition { Expression = "err:*", TargetNodeId = "next_other_error" }
        };
        var result4 = ToolResult.Fail("node4", "TestTool", "warp_failed", "传送失败");
        var matched4 = TransitionMatcher.Match(transitions4, result4, isTimeout: false);
        if (matched4 == "next_nav_error")
        {
            Console.WriteLine("  ✓ 通过: 匹配到 'next_nav_error'");
            passed++;
        }
        else
        {
            Console.WriteLine($"  ✗ 失败: 期望 'next_nav_error'，实际 '{matched4}'");
            failed++;
        }

        // 测试5: "ok|timeout|err:*" 表达式 - 多个条件的逻辑或
        Console.WriteLine("\n测试5: 'ok|timeout|err:*' 表达式 - 多个条件的逻辑或");
        var transitions5 = new List<Transition>
        {
            new Transition { Expression = "ok|timeout|err:*", TargetNodeId = "next_any" },
            new Transition { Expression = "err:unreachable", TargetNodeId = "next_specific" }
        };
        var result5 = ToolResult.Success("node5", "TestTool");
        var matched5 = TransitionMatcher.Match(transitions5, result5, isTimeout: false);
        if (matched5 == "next_any")
        {
            Console.WriteLine("  ✓ 通过: 匹配到 'next_any'");
            passed++;
        }
        else
        {
            Console.WriteLine($"  ✗ 失败: 期望 'next_any'，实际 '{matched5}'");
            failed++;
        }

        // 测试6: 顺序匹配 - 第一个匹配的应该被返回
        Console.WriteLine("\n测试6: 顺序匹配 - 第一个匹配的应该被返回");
        var transitions6 = new List<Transition>
        {
            new Transition { Expression = "err:*", TargetNodeId = "first_match" },
            new Transition { Expression = "err:unreachable", TargetNodeId = "second_match" }
        };
        var result6 = ToolResult.Fail("node6", "TestTool", "unreachable", "无法到达");
        var matched6 = TransitionMatcher.Match(transitions6, result6, isTimeout: false);
        if (matched6 == "first_match")
        {
            Console.WriteLine("  ✓ 通过: 匹配到第一个 'first_match'（顺序优先）");
            passed++;
        }
        else
        {
            Console.WriteLine($"  ✗ 失败: 期望 'first_match'，实际 '{matched6}'");
            failed++;
        }

        // 测试7: 无匹配情况 - 应该返回 null
        Console.WriteLine("\n测试7: 无匹配情况 - 应该返回 null");
        var transitions7 = new List<Transition>
        {
            new Transition { Expression = "ok", TargetNodeId = "next_success" },
            new Transition { Expression = "timeout", TargetNodeId = "next_timeout" }
        };
        var result7 = ToolResult.Fail("node7", "TestTool", "menu_not_open", "菜单未打开");
        var matched7 = TransitionMatcher.Match(transitions7, result7, isTimeout: false);
        if (matched7 == null)
        {
            Console.WriteLine("  ✓ 通过: 无匹配返回 null");
            passed++;
        }
        else
        {
            Console.WriteLine($"  ✗ 失败: 期望 null，实际 '{matched7}'");
            failed++;
        }

        // 测试8: 空列表 - 应该返回 null
        Console.WriteLine("\n测试8: 空列表 - 应该返回 null");
        var transitions8 = new List<Transition>();
        var result8 = ToolResult.Success("node8", "TestTool");
        var matched8 = TransitionMatcher.Match(transitions8, result8, isTimeout: false);
        if (matched8 == null)
        {
            Console.WriteLine("  ✓ 通过: 空列表返回 null");
            passed++;
        }
        else
        {
            Console.WriteLine($"  ✗ 失败: 期望 null，实际 '{matched8}'");
            failed++;
        }

        // 测试9: 特定错误码匹配
        Console.WriteLine("\n测试9: 特定错误码匹配 - 'err:menu_not_open'");
        var transitions9 = new List<Transition>
        {
            new Transition { Expression = "err:menu_not_open", TargetNodeId = "next_menu_error" },
            new Transition { Expression = "err:*", TargetNodeId = "next_other_error" }
        };
        var result9 = ToolResult.Fail("node9", "TestTool", "menu_not_open", "菜单未打开");
        var matched9 = TransitionMatcher.Match(transitions9, result9, isTimeout: false);
        if (matched9 == "next_menu_error")
        {
            Console.WriteLine("  ✓ 通过: 匹配到特定错误码 'next_menu_error'");
            passed++;
        }
        else
        {
            Console.WriteLine($"  ✗ 失败: 期望 'next_menu_error'，实际 '{matched9}'");
            failed++;
        }

        // 总结
        Console.WriteLine("\n=== 测试总结 ===");
        Console.WriteLine($"通过: {passed} 个");
        Console.WriteLine($"失败: {failed} 个");
        Console.WriteLine($"总计: {passed + failed} 个");

        if (failed == 0)
        {
            Console.WriteLine("\n✅ 所有测试通过！TransitionMatcher 实现正确。");
        }
        else
        {
            Console.WriteLine($"\n❌ 有 {failed} 个测试失败，请检查实现。");
        }
    }
}
