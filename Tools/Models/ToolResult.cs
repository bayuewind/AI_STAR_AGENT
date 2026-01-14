namespace Tools.Models;

/// <summary>
/// ToolResult - 工具执行结果（执行器和工具层的唯一契约）
/// 支持三种结果：成功 / 失败 / 超时
/// </summary>
public sealed class ToolResult
{
    /// <summary>
    /// 对应的节点ID（Runner用来对齐结果）
    /// </summary>
    public string ActionNodeId { get; set; } = string.Empty;

    /// <summary>
    /// 工具名
    /// </summary>
    public string Tool { get; set; } = string.Empty;

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Ok { get; set; }

    /// <summary>
    /// 错误信息（失败时必须有）
    /// </summary>
    public ToolError? Error { get; set; }

    /// <summary>
    /// 遥测数据（可选：location/tile/time等）
    /// </summary>
    public Dictionary<string, object>? Telemetry { get; set; }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static ToolResult Success(string nodeId, string tool, Dictionary<string, object>? telemetry = null)
        => new() 
        { 
            ActionNodeId = nodeId, 
            Tool = tool, 
            Ok = true, 
            Telemetry = telemetry 
        };

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static ToolResult Fail(string nodeId, string tool, string code, string detail = "", Dictionary<string, object>? telemetry = null)
        => new() 
        { 
            ActionNodeId = nodeId, 
            Tool = tool, 
            Ok = false, 
            Error = new ToolError { Code = code, Detail = detail }, 
            Telemetry = telemetry 
        };

    /// <summary>
    /// 创建超时结果（超时用 error.code = "timeout" 表达）
    /// </summary>
    public static ToolResult Timeout(string nodeId, string tool, Dictionary<string, object>? telemetry = null)
        => Fail(nodeId, tool, "timeout", "node execution timeout", telemetry);
}

/// <summary>
/// 工具错误信息
/// </summary>
public sealed class ToolError
{
    /// <summary>
    /// 错误码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 错误详情
    /// </summary>
    public string Detail { get; set; } = string.Empty;
}

/// <summary>
/// 错误码常量（建议）
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
