using Agent.PlanGraphModels;

namespace Agent.PlanGraphModels;

/// <summary>
/// PlanGraph 使用示例
/// 用于验证可以 new 一个 PlanGraph，包含 nodes & transitions，不报错
/// </summary>
public static class PlanGraphExample
{
    /// <summary>
    /// 创建一个示例计划图："去皮埃尔买10个防风草种子并回农场"
    /// </summary>
    public static PlanGraph CreateBuySeedsExample()
    {
        var plan = new PlanGraph
        {
            PlanId = "plan_buy_seeds_001",
            StartNodeId = "a_nav_shop"
        };

        // 节点1: 导航到商店
        var navShop = Node.CreateToolCall("a_nav_shop", "NavigateTo", new Dictionary<string, object>
        {
            { "location", "SeedShop" },
            { "target_tile", new { x = 5, y = 17 } }
        }, timeout: 2000);
        navShop.AddTransition("ok", "a_open_shop");
        navShop.AddTransition("err:*|timeout", "a_nav_recover");
        plan.Nodes.Add(navShop);

        // 节点2: 打开商店菜单
        var openShop = Node.CreateToolCall("a_open_shop", "Interact", new Dictionary<string, object>
        {
            { "npc", "Pierre" }
        }, timeout: 500);
        openShop.AddTransition("ok", "a_buy");
        openShop.AddTransition("err:closed|err:npc_missing", "a_wait_open");
        openShop.AddTransition("err:*", "a_nav_recover");
        plan.Nodes.Add(openShop);

        // 节点3: 购买种子
        var buy = Node.CreateToolCall("a_buy", "ShopBuy", new Dictionary<string, object>
        {
            { "item", "Parsnip Seeds" },
            { "quantity", 10 }
        }, timeout: 1000);
        buy.AddTransition("ok", "a_close_menu");
        buy.AddTransition("err:menu_not_open", "a_open_shop");
        buy.AddTransition("err:insufficient_gold", "llm_replan");
        buy.AddTransition("err:*", "a_nav_recover");
        plan.Nodes.Add(buy);

        // 节点4: 关闭菜单
        var closeMenu = Node.CreateToolCall("a_close_menu", "CloseMenu", new Dictionary<string, object>(), timeout: 200);
        closeMenu.AddTransition("ok|err:*|timeout", "a_nav_farm"); // 无关紧要的步骤
        plan.Nodes.Add(closeMenu);

        // 节点5: 导航回农场
        var navFarm = Node.CreateToolCall("a_nav_farm", "NavigateTo", new Dictionary<string, object>
        {
            { "location", "Farm" }
        }, timeout: 2000);
        navFarm.AddTransition("ok", "done");
        navFarm.AddTransition("err:*|timeout", "a_nav_recover");
        plan.Nodes.Add(navFarm);

        // 节点6: 恢复节点
        var navRecover = Node.CreateRecover("a_nav_recover", "NavigateTo", new Dictionary<string, object>
        {
            { "location", "Farm" }
        }, timeout: 3000);
        navRecover.AddTransition("ok", "llm_replan");
        navRecover.AddTransition("err:*|timeout", "llm_replan");
        plan.Nodes.Add(navRecover);

        // 节点7: 等待商店开门
        var waitOpen = Node.CreateToolCall("a_wait_open", "WaitUntil", new Dictionary<string, object>
        {
            { "condition", "shop_open" },
            { "timeout", 600 }
        }, timeout: 600);
        waitOpen.AddTransition("ok", "a_open_shop");
        waitOpen.AddTransition("timeout", "llm_replan");
        plan.Nodes.Add(waitOpen);

        // 节点8: 重规划节点
        var replan = Node.CreateLlmReplan("llm_replan");
        plan.Nodes.Add(replan);

        // 节点9: 完成目标节点
        var done = Node.CreateFinishGoal("done");
        plan.Nodes.Add(done);

        return plan;
    }

    /// <summary>
    /// 创建一个最小示例计划图（用于快速测试）
    /// </summary>
    public static PlanGraph CreateMinimalExample()
    {
        var plan = new PlanGraph
        {
            PlanId = "plan_minimal_001",
            StartNodeId = "node_start"
        };

        var startNode = Node.CreateToolCall("node_start", "TestTool", new Dictionary<string, object>(), timeout: 1000);
        startNode.AddTransition("ok", "node_finish");
        startNode.AddTransition("err:*", "node_finish");
        plan.Nodes.Add(startNode);

        var finishNode = Node.CreateFinishGoal("node_finish");
        plan.Nodes.Add(finishNode);

        return plan;
    }
}
