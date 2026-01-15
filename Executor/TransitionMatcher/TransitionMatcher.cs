using Core.Models;

namespace Executor.TransitionMatcher;

public class TransitionMatcher
{
    public static string? Match(List<Transition> transitions, ToolResult toolResult, bool isTimeout)
    {
        if (transitions == null || transitions.Count == 0) return null;

        foreach (var transition in transitions)
        {
            if (MatchesExpression(transition.Expression, toolResult, isTimeout)) return transition.TargetNodeId;
        }

        return null;
    }

    private static bool MatchesExpression(string expression, ToolResult toolResult, bool isTimeout)
    {
        if (string.IsNullOrWhiteSpace(expression)) return false;

        if (expression.Contains('|'))
        {
            var parts = expression.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var part in parts)
            {
                if (MatchesSingleCondition(part, toolResult, isTimeout)) return true;
            }
            return false;
        }

        return MatchesSingleCondition(expression, toolResult, isTimeout);
    }

    private static bool MatchesSingleCondition(string condition, ToolResult toolResult, bool isTimeout)
    {
        condition = condition.Trim();

        if (condition == "ok") return toolResult.Ok && !isTimeout;
        if (condition == "timeout") return isTimeout;
        if (condition == "err:*") return !toolResult.Ok && toolResult.Error != null;
        if (condition.StartsWith("err:", StringComparison.OrdinalIgnoreCase))
        {
            var errorCode = condition.Substring(4);
            return !toolResult.Ok && toolResult.Error != null && toolResult.Error.Code == errorCode;
        }

        return false;
    }
}
