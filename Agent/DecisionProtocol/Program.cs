using Agent.DecisionProtocol;

namespace Agent.DecisionProtocol;

/// <summary>
/// DecisionResponse 验证程序
/// 验收标准：能把 DecisionResponse 序列化为 JSON（或至少能构造对象）
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        DecisionResponseTest.RunTests();
    }
}
