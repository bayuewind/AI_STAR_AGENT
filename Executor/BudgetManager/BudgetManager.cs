namespace Executor.BudgetManager;

/// <summary>
/// BudgetManager - 预算管理器
/// 职责：限制最大重规划次数、最大总ticks、最大金币花费
/// </summary>
public class BudgetManager
{
    public int MaxReplans { get; set; } = 3;
    public int MaxTotalTicks { get; set; } = 4000;
    public int MaxGoldSpend { get; set; } = 400;

    // TODO: 实现BudgetManager逻辑
}
