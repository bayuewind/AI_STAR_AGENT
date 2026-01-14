using Agent.PlanGraphModels;
using Tools.Models;

namespace Executor.TransitionMatcher;

/// <summary>
/// TransitionMatcher - 转移条件匹配器
/// 支持表达式：ok, timeout, err:*, err:xxx|err:yyy, ok|timeout|err:*
/// </summary>
public class TransitionMatcher
{
    /// <summary>
    /// 匹配转移条件，返回第一个匹配的目标节点ID
    /// </summary>
    /// <param name="transitions">转移条件列表（按顺序匹配）</param>
    /// <param name="toolResult">工具执行结果</param>
    /// <param name="isTimeout">是否超时</param>
    /// <returns>匹配到的目标节点ID，如果没有匹配则返回null</returns>
    public static string? Match(List<Transition> transitions, ToolResult toolResult, bool isTimeout)
    {
        if (transitions == null || transitions.Count == 0)
            return null;

        foreach (var transition in transitions)
        {
            if (MatchesExpression(transition.Expression, toolResult, isTimeout))
            {
                return transition.TargetNodeId;
            }
        }

        return null;
    }

    /// <summary>
    /// 检查表达式是否匹配当前结果
    /// </summary>
    private static bool MatchesExpression(string expression, ToolResult toolResult, bool isTimeout)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return false;

        // 如果表达式包含 |，则按逻辑或处理
        if (expression.Contains('|'))
        {
            var parts = expression.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var part in parts)
            {
                if (MatchesSingleCondition(part, toolResult, isTimeout))
                {
                    return true;
                }
            }
            return false;
        }

        // 单个条件
        return MatchesSingleCondition(expression, toolResult, isTimeout);
    }

    /// <summary>
    /// 检查单个条件是否匹配
    /// </summary>
    private static bool MatchesSingleCondition(string condition, ToolResult toolResult, bool isTimeout)
    {
        condition = condition.Trim();

        // "ok" - 成功且未超时
        if (condition == "ok")
        {
            return toolResult.Ok && !isTimeout;
        }

        // "timeout" - 超时
        if (condition == "timeout")
        {
            return isTimeout;
        }

        // "err:*" - 任意错误
        if (condition == "err:*")
        {
            return !toolResult.Ok && toolResult.Error != null;
        }

        // "err:xxx" - 特定错误码
        if (condition.StartsWith("err:", StringComparison.OrdinalIgnoreCase))
        {
            var errorCode = condition.Substring(4); // 跳过 "err:"
            return !toolResult.Ok && toolResult.Error != null && toolResult.Error.Code == errorCode;
        }

        // 未知条件格式，返回false
        return false;
    }
}
