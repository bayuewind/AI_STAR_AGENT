namespace Core.Models;

public sealed class Goal
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string? DeadlineGameTime { get; set; }
}

public sealed class GoalStack
{
    private readonly List<Goal> _goals = new();
    public int Count => _goals.Count;
    public bool IsEmpty => _goals.Count == 0;

    public Goal? Peek()
    {
        if (_goals.Count == 0) return null;
        return _goals.OrderByDescending(g => g.Priority).First();
    }

    public IReadOnlyList<Goal> GetAll()
    {
        return _goals.OrderByDescending(g => g.Priority).ToList();
    }

    public void Push(Goal goal)
    {
        if (goal == null) throw new ArgumentNullException(nameof(goal));
        _goals.Add(goal);
    }

    public Goal? Pop()
    {
        if (_goals.Count == 0) return null;
        var highestPriority = _goals.Max(g => g.Priority);
        var goalToRemove = _goals.FirstOrDefault(g => g.Priority == highestPriority);
        if (goalToRemove != null) _goals.Remove(goalToRemove);
        return goalToRemove;
    }

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

    public void Clear() => _goals.Clear();
    public bool Contains(string goalId) => _goals.Any(g => g.Id == goalId);
}
