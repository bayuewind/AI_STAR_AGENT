using Core.Interfaces;
using Core.Models;
using System.Text.Json;

namespace AI.PlannerClient;

public class GeminiProviderStub : ILLMProvider
{
    private bool _simulateError;

    public GeminiProviderStub(bool simulateError = false)
    {
        _simulateError = simulateError;
    }

    public void SetError(bool simulateError)
    {
        _simulateError = simulateError;
    }

    public async Task<string> GetCompletionAsync(string prompt, string? systemInstruction = null)
    {
        await Task.Delay(100); // Simulate network latency

        if (_simulateError)
        {
            throw new Exception("Gemini API Error: Rate limit exceeded.");
        }

        // Logic simulation: AI reads the prompt and context to decide on a plan
        if (prompt.Contains("\"closed\"") || prompt.Contains("closed right now"))
        {
            return GetWaitAndBuyPlan();
        }
        
        if (prompt.Contains("Talk to Abigail"))
        {
            return GetTalkToAbigailPlan();
        }
        else if (prompt.Contains("Buy seeds from Pierre"))
        {
            return GetBuySeedsPlan();
        }
        else if (prompt.Contains("Return to Farm"))
        {
            return GetReturnHomePlan();
        }

        return GetTalkToAbigailPlan(); // Default
    }

    private string GetWaitAndBuyPlan()
    {
        return @"
        {
            ""protocol_version"": ""agent-json-v1"",
            ""mode"": ""act"",
            ""plan_graph"": {
                ""plan_id"": ""plan_recovery_wait"",
                ""start_node_id"": ""wait_step"",
                ""nodes"": [
                    {
                        ""node_id"": ""wait_step"",
                        ""type"": ""ToolCall"",
                        ""tool"": ""WaitUntil"",
                        ""args"": { ""Time"": ""09:00"" },
                        ""transitions"": [
                            { ""expression"": ""ok"", ""target_node_id"": ""nav_to_shop"" }
                        ]
                    },
                    {
                        ""node_id"": ""nav_to_shop"",
                        ""type"": ""ToolCall"",
                        ""tool"": ""NavigateTo"",
                        ""args"": { ""Location"": ""SeedShop"" },
                        ""transitions"": [
                            { ""expression"": ""ok"", ""target_node_id"": ""finish"" }
                        ]
                    },
                    { ""node_id"": ""finish"", ""type"": ""FinishGoal"" }
                ]
            }
        }";
    }

    private string GetTalkToAbigailPlan()
    {
        return @"
        {
            ""protocol_version"": ""agent-json-v1"",
            ""mode"": ""act"",
            ""plan_graph"": {
                ""plan_id"": ""plan_social_abigail"",
                ""start_node_id"": ""nav_to_target"",
                ""nodes"": [
                    {
                        ""node_id"": ""nav_to_target"",
                        ""type"": ""ToolCall"",
                        ""tool"": ""NavigateTo"",
                        ""args"": { ""Location"": ""Town"" },
                        ""timeout"": 100,
                        ""transitions"": [
                            { ""expression"": ""ok"", ""target_node_id"": ""talk_to_npc"" },
                            { ""expression"": ""err:*"", ""target_node_id"": ""replan"" }
                        ]
                    },
                    {
                        ""node_id"": ""talk_to_npc"",
                        ""type"": ""ToolCall"",
                        ""tool"": ""TalkTo"",
                        ""args"": { ""NPCName"": ""Abigail"" },
                        ""transitions"": [
                            { ""expression"": ""ok"", ""target_node_id"": ""finish"" }
                        ]
                    },
                    { ""node_id"": ""finish"", ""type"": ""FinishGoal"" },
                    { ""node_id"": ""replan"", ""type"": ""LlmReplan"" }
                ]
            }
        }";
    }

    private string GetBuySeedsPlan()
    {
        return @"
        {
            ""protocol_version"": ""agent-json-v1"",
            ""mode"": ""act"",
            ""plan_graph"": {
                ""plan_id"": ""plan_buy_seeds"",
                ""start_node_id"": ""nav_to_shop"",
                ""nodes"": [
                    {
                        ""node_id"": ""nav_to_shop"",
                        ""type"": ""ToolCall"",
                        ""tool"": ""NavigateTo"",
                        ""args"": { ""Location"": ""SeedShop"" },
                        ""transitions"": [
                            { ""expression"": ""ok"", ""target_node_id"": ""perform_buy"" },
                            { ""expression"": ""err:closed"", ""target_node_id"": ""replan"" }
                        ]
                    },
                    {
                        ""node_id"": ""perform_buy"",
                        ""type"": ""ToolCall"",
                        ""tool"": ""ShopBuy"",
                        ""args"": { ""ItemId"": ""ParsnipSeeds"", ""Amount"": 10 },
                        ""transitions"": [
                            { ""expression"": ""ok"", ""target_node_id"": ""finish"" }
                        ]
                    },
                    { ""node_id"": ""finish"", ""type"": ""FinishGoal"" },
                    { ""node_id"": ""replan"", ""type"": ""LlmReplan"" }
                ]
            }
        }";
    }

    private string GetReturnHomePlan()
    {
        return @"
        {
            ""protocol_version"": ""agent-json-v1"",
            ""mode"": ""act"",
            ""plan_graph"": {
                ""plan_id"": ""plan_return_home"",
                ""start_node_id"": ""go_home"",
                ""nodes"": [
                    {
                        ""node_id"": ""go_home"",
                        ""type"": ""ToolCall"",
                        ""tool"": ""NavigateTo"",
                        ""args"": { ""Location"": ""Farm"" },
                        ""transitions"": [
                            { ""expression"": ""ok"", ""target_node_id"": ""finish"" }
                        ]
                    },
                    { ""node_id"": ""finish"", ""type"": ""FinishGoal"" }
                ]
            }
        }";
    }
}
