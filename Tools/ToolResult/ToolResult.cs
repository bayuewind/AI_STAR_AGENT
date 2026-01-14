namespace Tools.ToolResult;

/// <summary>
/// ToolResult - 工具执行结果
/// </summary>
public class ToolResult
{
    public string ActionNodeId { get; set; } = string.Empty;
    public string Tool { get; set; } = string.Empty;
    public bool Ok { get; set; }
    public ErrorInfo? Error { get; set; }
    public Dictionary<string, object> Telemetry { get; set; } = new();
}

/// <summary>
/// 错误信息
/// </summary>
public class ErrorInfo
{
    public string Code { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
}

/// <summary>
/// 错误码枚举（建议）
/// </summary>
public static class ErrorCodes
{
    // 导航相关
    public const string Unreachable = "unreachable";
    public const string WarpFailed = "warp_failed";
    public const string BlockedDynamic = "blocked_dynamic";
    public const string Stuck = "stuck";

    // 交互/商店相关
    public const string Closed = "closed";
    public const string NpcMissing = "npc_missing";
    public const string MenuNotOpen = "menu_not_open";
    public const string ItemNotFound = "item_not_found";
    public const string InsufficientGold = "insufficient_gold";

    // 通用
    public const string Timeout = "timeout";
    public const string InternalError = "internal_error";
}
