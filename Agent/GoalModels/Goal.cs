namespace Agent.GoalModels;

/// <summary>
/// 目标模型
/// </summary>
public class Goal
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string? DeadlineGameTime { get; set; }
}
