namespace Agent.GoalModels;

/// <summary>
/// Goal - 目标模型（轻量即可）
/// </summary>
public sealed class Goal
{
    /// <summary>
    /// 目标ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 目标文本描述
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// 优先级（数字越大优先级越高）
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 截止时间（游戏时间，可选）
    /// </summary>
    public string? DeadlineGameTime { get; set; }
}

/// <summary>
/// GoalStack - 目标栈（List<Goal> 的封装）
/// 支持 Push/Pop 操作，用于管理多个目标
/// </summary>
public sealed class GoalStack
{
    private readonly List<Goal> _goals = new();

    /// <summary>
    /// 获取目标数量
    /// </summary>
    public int Count => _goals.Count;

    /// <summary>
    /// 是否为空
    /// </summary>
    public bool IsEmpty => _goals.Count == 0;

    /// <summary>
    /// 获取当前最高优先级的目标（不移除）
    /// </summary>
    public Goal? Peek()
    {
        if (_goals.Count == 0)
            return null;

        // 按优先级排序，返回最高优先级的
        return _goals.OrderByDescending(g => g.Priority).First();
    }

    /// <summary>
    /// 获取所有目标（按优先级排序）
    /// </summary>
    public IReadOnlyList<Goal> GetAll()
    {
        return _goals.OrderByDescending(g => g.Priority).ToList();
    }

    /// <summary>
    /// 推送目标到栈中
    /// </summary>
    public void Push(Goal goal)
    {
        if (goal == null)
            throw new ArgumentNullException(nameof(goal));

        _goals.Add(goal);
    }

    /// <summary>
    /// 弹出最高优先级的目标
    /// </summary>
    public Goal? Pop()
    {
        if (_goals.Count == 0)
            return null;

        // 找到最高优先级的目标
        var highestPriority = _goals.Max(g => g.Priority);
        var goalToRemove = _goals.FirstOrDefault(g => g.Priority == highestPriority);

        if (goalToRemove != null)
        {
            _goals.Remove(goalToRemove);
        }

        return goalToRemove;
    }

    /// <summary>
    /// 移除指定ID的目标
    /// </summary>
    public bool Remove(string goalId)
    {
        var goal = _goals.FirstOrDefault(g => g.Id == goalId);
        if (goal != null)
        {
            _goals.Remove(goal);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 清空所有目标
    /// </summary>
    public void Clear()
    {
        _goals.Clear();
    }

    /// <summary>
    /// 检查是否包含指定ID的目标
    /// </summary>
    public bool Contains(string goalId)
    {
        return _goals.Any(g => g.Id == goalId);
    }
}
