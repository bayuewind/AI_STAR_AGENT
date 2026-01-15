using AI.PlannerClient;
using Core.Models;
using Core.Interfaces;
using Tools.Implementations;

namespace Agent;

class MockPerceptionProvider : IPerceptionProvider
{
    public string CurrentLocation { get; set; } = "Farm";
    public int Gold { get; set; } = 1000;
    public int X { get; set; } = 10;
    public int Y { get; set; } = 10;
    public string? LastError { get; set; }
    public Inventory Inventory { get; set; } = new();

    public PerceptionSnapshot GetSnapshot(string agentId, int tickId)
    {
        var snapshot = new PerceptionSnapshot
        {
            TickId = tickId,
            Gold = this.Gold,
            TileX = this.X,
            TileY = this.Y,
            Location = this.CurrentLocation,
            LastErrorCode = this.LastError,
            Inventory = this.Inventory,
            World = new WorldState { TimeOfDay = "08:00" }
        };

        return snapshot;
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Stardew AI Agent: The Ultimate 'Full Day' Demo ===\n");

        // 1. Setup Framework Components
        var toolDispatcher = new ToolDispatcherStub();
        var perception = new MockPerceptionProvider();
        var embedding = new EmbeddingProviderStub();
        var geminiStub = new GeminiProviderStub();
        var robustBrain = new LLMRetryWrapper(geminiStub);
        var plannerClient = new PlannerClient(robustBrain);
        
        var agent = new AgentRuntime("player_1", toolDispatcher, plannerClient, perception, embedding);

        // 2. Inject Knowledge into Memory
        agent.Memory.AddEntry(new MemoryEntry {
            Content = "Pierre's Seed Shop is located in the Town center. It opens at 09:00.",
            Type = "fact",
            Tags = new List<string> { "SeedShop", "location", "hours" },
            Importance = 5
        });
        agent.Memory.AddEntry(new MemoryEntry {
            Content = "Abigail often hangs out in the Town or near the Seed Shop in the afternoon.",
            Type = "schedule",
            Tags = new List<string> { "Abigail", "location" },
            Importance = 4
        });

        // 3. Queue Daily Goals (Priority based)
        Console.WriteLine("[System] Queueing Goals: 1. Socialize, 2. Buy Seeds, 3. Go Home.");
        agent.GoalStack.Push(new Goal { Id = "home", Text = "Return to Farm", Priority = 1 });
        agent.GoalStack.Push(new Goal { Id = "seeds", Text = "Buy seeds from Pierre", Priority = 5 });
        agent.GoalStack.Push(new Goal { Id = "social", Text = "Talk to Abigail", Priority = 10 });

        // 4. Day Simulation (06:00 to 18:00)
        Console.WriteLine("\n[System] Starting Morning Routine...");
        int currentX = 10;
        
        for (int tick = 0; tick < 300; tick++)
        {
            // Simulate Time progression
            int hour = 6 + (tick / 20); // Accelerated time
            int min = (tick % 20) * 3;
            string currentTime = $"{hour:D2}:{min:D2}";
            perception.CurrentLocation = (currentX < 40) ? "Farm" : "Town";
            perception.X = currentX;

            // Trigger Abigail appearing in Town around 10:00
            if (hour >= 10 && perception.CurrentLocation == "Town")
            {
                // Note: In real Mod, this is handled by IPerceptionProvider.GetSnapshot
            }

            agent.Update(tick);

            // Simulation Side Effects (Mocking movement speed)
            if (agent.Status == AgentStatus.Running || agent.Status == AgentStatus.WaitingTool)
            {
                currentX += 2;
            }

            // Periodically Save/Load to verify persistence doesn't break the cycle
            if (tick == 50)
            {
                Console.WriteLine("\n[System] --- Simulating Persistence Check: Save & Reload ---");
                string snapshot = agent.SaveState();
                agent = new AgentRuntime("player_1", toolDispatcher, plannerClient, perception, embedding);
                agent.LoadState(snapshot);
                Console.WriteLine("[System] --- Agent restored successfully ---\n");
            }

            if (agent.GoalStack.IsEmpty && agent.Status == AgentStatus.Idle && tick > 100)
            {
                Console.WriteLine($"\n[Tick {tick}] All daily tasks completed autonomously!");
                break;
            }

            await Task.Delay(5); // Smooth simulation
        }

        Console.WriteLine("\n=== Final Agent Status ===");
        Console.WriteLine($"Location: {perception.CurrentLocation} (X: {perception.X})");
        Console.WriteLine($"Memories Learned: {agent.Memory.Query(new MemoryQuery { Limit = 100 }).Count}");

        Console.WriteLine("\n=== Demo Finished ===");
    }
}
