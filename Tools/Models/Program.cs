using Tools.Models;

namespace Tools.Models;

/// <summary>
/// ToolResult 验证程序
/// 验收标准：能构造 ToolResult(ok/err/timeout) 并打印 JSON
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        ToolResultTest.RunTests();
    }
}
