using Tools.Interfaces;
using Tools.Models;

namespace Tools.Implementations;

/// <summary>
/// ToolDispatcherStub - 工具调度器存根实现（用于测试）
/// 随机返回ok/err
/// </summary>
public class ToolDispatcherStub : IToolDispatcher
{
    private readonly Random _random = new();
    private readonly Dictionary<string, ToolResult> _results = new();

    public string Begin(string tool, Dictionary<string, object> args)
    {
        var handle = Guid.NewGuid().ToString();
        var ok = _random.Next(2) == 0;
        
        _results[handle] = new ToolResult
        {
            ActionNodeId = string.Empty,
            Tool = tool,
            Ok = ok,
            Error = ok ? null : new ToolError
            {
                Code = "stub_error",
                Detail = "Random stub error"
            },
            Telemetry = new Dictionary<string, object>()
        };

        return handle;
    }

    public ToolResult? Poll(string handle)
    {
        return _results.TryGetValue(handle, out var result) ? result : null;
    }

    public void Cancel(string handle)
    {
        _results.Remove(handle);
    }
}
