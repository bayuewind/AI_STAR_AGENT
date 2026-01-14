using Tools.Models;

namespace Tools.Interfaces;

/// <summary>
/// IToolDispatcher - 工具调度器接口
/// 职责：定义Begin / Poll / Cancel
/// </summary>
public interface IToolDispatcher
{
    /// <summary>
    /// 开始执行工具调用
    /// </summary>
    string Begin(string tool, Dictionary<string, object> args);

    /// <summary>
    /// 轮询工具执行结果
    /// </summary>
    ToolResult? Poll(string handle);

    /// <summary>
    /// 取消工具执行
    /// </summary>
    void Cancel(string handle);
}
