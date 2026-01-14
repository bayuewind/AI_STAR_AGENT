using Tools.Models;

namespace Tools.Interfaces;

/// <summary>
/// IToolDispatcher - 工具调度器接口
/// 职责：定义Begin / Poll / Cancel（Begin/Poll模式）
/// tick 驱动统一由 Runner 控制，工具层只接受当前 tick
/// </summary>
public interface IToolDispatcher
{
    /// <summary>
    /// 开始执行工具调用
    /// </summary>
    /// <param name="nodeId">节点ID（用于对齐结果）</param>
    /// <param name="toolName">工具名称</param>
    /// <param name="args">工具参数</param>
    /// <param name="tickId">当前 tick ID</param>
    /// <returns>工具执行句柄（handle）</returns>
    string Begin(string nodeId, string toolName, Dictionary<string, object> args, int tickId);

    /// <summary>
    /// 轮询工具执行结果
    /// </summary>
    /// <param name="handle">工具执行句柄</param>
    /// <param name="tickId">当前 tick ID</param>
    /// <returns>元组：(是否就绪, 工具结果)。如果未就绪，ready=false，result=null；如果就绪，ready=true，result包含结果</returns>
    (bool ready, ToolResult? result) Poll(string handle, int tickId);

    /// <summary>
    /// 取消工具执行
    /// </summary>
    /// <param name="handle">工具执行句柄</param>
    /// <param name="tickId">当前 tick ID</param>
    void Cancel(string handle, int tickId);
}
