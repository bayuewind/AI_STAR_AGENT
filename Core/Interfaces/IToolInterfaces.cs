using Core.Models;

namespace Core.Interfaces;

public interface IToolDispatcher
{
    string Begin(string nodeId, string toolName, Dictionary<string, object> args, int tickId);
    (bool ready, ToolResult? result) Poll(string handle, int tickId);
    void Cancel(string handle, int tickId);
}
