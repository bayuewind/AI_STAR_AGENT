using Core.Models;
using Executor.PlanGraphRunner;
using Tools.Implementations;

namespace TestMVP0;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== MVP-0: 固定图 → 跑通 测试 ===\n");

        // 创建买种子的固定 PlanGraph
        var planGraph = CreateBuySeedsPlan();
        Console.WriteLine($"计划: {planGraph.PlanId}");
        Console.WriteLine($"起始节点: {planGraph.StartNodeId}");
        Console.WriteLine($"节点数量: {planGraph.Nodes.Count}");
        Console.WriteLine($"验证: {(planGraph.Validate() ? "✓" : "✗")}\n");

        // 创建工具调度器（Stub）
        var toolDispatcher = new ToolDispatcherStub();
        var runner = new PlanGraphRunner(planGraph, toolDispatcher);

        Console.WriteLine("=== 场景1: 正常流程（所有工具成功） ===\n");
        RunSimulation(runner, toolDispatcher, maxTicks: 200);

        Console.WriteLine("\n\n=== 场景2: ShopBuy 失败 menu_not_open → 重试 ===\n");
        var planGraph2 = CreateBuySeedsPlan();
        var toolDispatcher2 = new ToolDispatcherStub();
        toolDispatcher2.ConfigureToolResult("ShopBuy", ok: false, errorCode: "menu_not_open", errorDetail: "Shop menu is not open");
        var runner2 = new PlanGraphRunner(planGraph2, toolDispatcher2);
        
        RunSimulation(runner2, toolDispatcher2, maxTicks: 300);

        Console.WriteLine("\n=== MVP-0 测试完成 ===");
    }

    static void RunSimulation(PlanGraphRunner runner, ToolDispatcherStub dispatcher, int maxTicks)
    {
        int tickId = 0;
        while (tickId < maxTicks)
        {
            var status = runner.Tick(tickId);

            if (status == RunnerStatus.Done)
            {
                Console.WriteLine($"\n[Tick {tickId}] ✅ 目标完成！");
                break;
            }
            else if (status == RunnerStatus.NeedReplan)
            {
                Console.WriteLine($"\n[Tick {tickId}] ⚠️ 需要重新规划");
                break;
            }

            tickId++;
        }

        if (tickId >= maxTicks)
        {
            Console.WriteLine($"\n⚠️ 超时 ({maxTicks} ticks)");
        }
    }

    static PlanGraph CreateBuySeedsPlan()
    {
        // a_nav_shop (NavigateTo) → ok → a_open_shop
        // a_open_shop (Interact) → ok → a_buy
        // a_buy (ShopBuy) → ok → a_nav_farm
        //                 → err:menu_not_open → a_open_shop (重试)
        // a_nav_farm (NavigateTo) → ok → done

        var plan = new PlanGraph
        {
            PlanId = "buy_seeds_plan",
            StartNodeId = "a_nav_shop",
            Nodes = new List<Node>()
        };

        // 节点1: 导航到商店
        plan.Nodes.Add(Node.CreateToolCall("a_nav_shop", "NavigateTo", new Dictionary<string, object>
        {
            ["Location"] = "SeedShop"
        }, timeout: 100)
        .AddTransition("ok", "a_open_shop"));

        // 节点2: 打开商店菜单
        plan.Nodes.Add(Node.CreateToolCall("a_open_shop", "Interact", new Dictionary<string, object>
        {
            ["Target"] = "ShopCounter"
        }, timeout: 20)
        .AddTransition("ok", "a_buy"));

        // 节点3: 购买种子（关键节点：可能失败）
        plan.Nodes.Add(Node.CreateToolCall("a_buy", "ShopBuy", new Dictionary<string, object>
        {
            ["ItemId"] = "ParsnipSeeds",
            ["Amount"] = 10
        }, timeout: 20)
        .AddTransition("ok", "a_nav_farm")
        .AddTransition("err:menu_not_open", "a_open_shop")); // 失败重试

        // 节点4: 返回农场
        plan.Nodes.Add(Node.CreateToolCall("a_nav_farm", "NavigateTo", new Dictionary<string, object>
        {
            ["Location"] = "Farm"
        }, timeout: 100)
        .AddTransition("ok", "done"));

        // 节点5: 完成
        plan.Nodes.Add(Node.CreateFinishGoal("done"));

        return plan;
    }
}
