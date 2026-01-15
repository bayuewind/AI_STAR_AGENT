namespace Core.Models;

public class ToolResult
{
    public string ActionNodeId { get; set; } = string.Empty;
    public string Tool { get; set; } = string.Empty;
    public bool Ok { get; set; }
    public ErrorInfo? Error { get; set; }
    public Dictionary<string, object> Telemetry { get; set; } = new();
    public ActionDelta? Delta { get; set; }

    public static ToolResult Success(string nodeId, string tool) => new() { ActionNodeId = nodeId, Tool = tool, Ok = true };
    public static ToolResult Failure(string nodeId, string tool, string code, string detail) => new() { ActionNodeId = nodeId, Tool = tool, Ok = false, Error = new ErrorInfo { Code = code, Detail = detail } };
    public static ToolResult Timeout(string nodeId, string tool) => Failure(nodeId, tool, "timeout", "Tool execution timed out");
}

public class ActionDelta
{
    public int GoldChanged { get; set; }
    public List<ItemDelta> ItemsChanged { get; set; } = new();
    public string? LocationChanged { get; set; }
}

public class ItemDelta
{
    public string ItemId { get; set; } = string.Empty;
    public int CountChanged { get; set; }
}

public class ErrorInfo
{
    public string Code { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
}
