using AI.PlannerClient;
using Core.Models;
using Core.Interfaces;
using Tools.Implementations;
using Perception;
using StardewValley;
using StardewValley.Menus;
using StardewModdingAPI;

namespace Agent;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Stardew AI Agent: Integrated System Demo ===\n");
        Console.WriteLine("[System] Initializing SMAPI Environment Stubs...");

        // 1. Initialize Game State (Simulating the Game Engine)
        InitializeGameState();

        // 2. Setup Framework Components (Using REAL Implementations)
        var monitor = new ConsoleMonitor();
        var toolDispatcher = new SmapiToolDispatcher(monitor);
        var perceptionProvider = new SmapiPerceptionProvider(monitor);
        var embedding = new EmbeddingProviderStub();

        // LLM Setup: Try to use OpenAI if key exists, otherwise fallback to Stub
        ILLMProvider llmProvider;
        string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        
        if (!string.IsNullOrEmpty(apiKey))
        {
            Console.WriteLine("[System] 🧠 Using Real OpenAI API");
            llmProvider = new OpenAIProvider(apiKey, model: "gpt-4o");
        }
        else
        {
            Console.WriteLine("[System] 🧠 OPENAI_API_KEY not found. Using Gemini Stub (Offline Mode)");
            llmProvider = new GeminiProviderStub();
        }

        var robustBrain = new LLMRetryWrapper(llmProvider);
        var plannerClient = new PlannerClient(robustBrain);
        
        var agent = new AgentRuntime("player_1", toolDispatcher, plannerClient, perceptionProvider, embedding);

        // 3. Inject Knowledge
        agent.Memory.AddEntry(new MemoryEntry {
            Content = "Pierre's Seed Shop is located in Town. It opens at 09:00.",
            Type = "fact",
            Tags = new List<string> { "SeedShop", "location", "hours" },
            Importance = 5
        });

        // 4. Set Goal
        Console.WriteLine("[System] Setting Goal: Buy seeds from Pierre");
        agent.GoalStack.Push(new Goal { Id = "mission_1", Text = "Buy 5 Parsnip Seeds from Pierre", Priority = 10 });

        // 5. Run Game Loop
        Console.WriteLine("\n[System] Starting Game Loop (08:30 AM)...");
        
        // Simulation parameters
        int tick = 0;
        bool goalComplete = false;

        while (tick < 1000 && !goalComplete)
        {
            // --- Game Engine Simulation Step ---
            
            // 1. Time passing (Accelerated: every 20 ticks = 10 mins)
            if (tick % 20 == 0)
            {
                Game1.timeOfDay += 10;
                if (Game1.timeOfDay % 100 >= 60) Game1.timeOfDay = (Game1.timeOfDay / 100 + 1) * 100;
                // Console.WriteLine($"[Game] Time: {Game1.timeOfDay:D4}");
            }

            // 2. Physics/Movement Simulation
            // In real game, SMAPI/Game handles this. Here we simulate movement if player has no controller blocking it.
            // But SmapiToolDispatcher stops player when calling halt(), so we don't auto-move here.
            // We'll let the "NavigateTo" tool logic inside Dispatcher handle logic success.
            // To make "NavigateTo" actually work in this stub environment, we need to cheat a bit and 
            // teleport player if the tool is running properly (simulating movement).
            
            // We can inspect internal dispatcher state via Reflection or just assume for this demo
            // that if Agent is in Running state, we simulate world changes based on intent.
            
            // SIMULATION CHEAT: If goal is to go to SeedShop, and we are Navigating, move us there eventually.
            // This logic replaces the real game's pathfinding engine for this console demo.
            SimulateWorldPhysics(agent, tick);

            // --- Agent Update Step ---
            agent.Update(tick);

            // Check completion
            if (agent.Status == AgentStatus.Idle && agent.GoalStack.IsEmpty)
            {
                Console.WriteLine($"\n[Tick {tick}] ✅ Mission Accomplished!");
                goalComplete = true;
            }

            tick++;
            await Task.Delay(10); // Throttle for readability
        }

        Console.WriteLine("\n=== Final Status ===");
        Console.WriteLine($"Location: {Game1.currentLocation.NameOrUniqueName}");
        Console.WriteLine($"Gold: {Game1.player.Money}");
        Console.WriteLine($"Inventory: {Game1.player.Items.Count} items");
        foreach(var item in Game1.player.Items)
        {
            Console.WriteLine($" - {item.Stack}x {item.DisplayName}");
        }
    }

    static void InitializeGameState()
    {
        // Player
        Game1.player = new Farmer
        {
            Money = 500,
            Stamina = 270,
            MaxStamina = 270,
            TilePoint = new Microsoft.Xna.Framework.Point { X = 10, Y = 10 },
            MaxItems = 12,
            Items = new List<StardewValley.Item>()
        };

        // World
        Game1.currentLocation = new GameLocation { NameOrUniqueName = "Farm", IsOutdoors = true };
        Game1.timeOfDay = 830;
        Game1.dayOfMonth = 1;
        Game1.currentSeason = "spring";
        Context.IsWorldReady = true;
    }

    static void SimulateWorldPhysics(AgentRuntime agent, int tick)
    {
        // This method acts as the "Game Engine" responding to the Agent's actions.
        
        // 1. Simulate Navigation Arrival
        // If agent is running NavigateTo(SeedShop) and some time passed, arrive.
        // In real game, pathfinder moves player pixel by pixel.
        if (agent.Status == AgentStatus.Running)
        {
            // Check if we are "traveling"
            // Since we can't easily peek into Agent's current tool without exposing it,
            // we rely on the PlannerClientStub to give a specific plan we know.
            // Plan: NavigateTo(SeedShop) -> WaitUntil(900) -> ShopBuy(...)
            
            if (Game1.currentLocation.NameOrUniqueName == "Farm" && tick > 50)
            {
                // Simulate arrival at town
                // Game1.currentLocation.NameOrUniqueName = "Town"; 
                // Console.WriteLine("[Game] Player walked to Town.");
            }
            
            // Hardcoded simulation for the specific plan we expect from PlannerStub
            // The stub plan usually does: Wait -> Nav -> Finish
            // Let's assume the Nav tool effectively teleports us in this simulation after delay
            
            // If we see the agent is calling NavigateTo in logs... 
            // Actually, SmapiToolDispatcher.PollNavigation checks:
            // if (Game1.currentLocation.Name == target) -> Success
            
            // So we must update Game1.currentLocation externally to let the tool succeed!
            
            // HEURISTIC: If we are in Farm and time > 700, move to SeedShop (simulating travel time)
            if (Game1.timeOfDay >= 700 && Game1.currentLocation.NameOrUniqueName == "Farm")
            {
                Game1.currentLocation = new GameLocation { NameOrUniqueName = "SeedShop", IsOutdoors = false };
                Game1.player.TilePoint = new Microsoft.Xna.Framework.Point { X = 5, Y = 5 };
                Console.WriteLine("[Game] World Event: Player arrived at SeedShop.");
            }
        }

        // 2. Simulate Shop Interaction
        // If we are at SeedShop and time >= 900, open the shop menu automatically 
        // (or wait for Interact tool? In real game, player must click. 
        // SmapiToolDispatcher Interact tool will just succeed instantly. 
        // But ShopBuy needs menu open.)
        
        if (Game1.currentLocation.NameOrUniqueName == "SeedShop" && Game1.timeOfDay >= 900)
        {
            if (Game1.activeClickableMenu == null)
            {
                // Simulate player clicking counter / Shop owner opening shop
                Game1.activeClickableMenu = new ShopMenu();
                Console.WriteLine("[Game] World Event: Shop Menu opened.");
            }
        }
        
        // 3. Simulate Item Transaction
        // SmapiToolDispatcher checks money, but doesn't actually deduct it in the stub logic unless we uncomment it.
        // Let's update the SmapiToolDispatcher logic to actually modify Game1 state later, 
        // or simulating it here if Dispatcher is read-only.
        // Currently SmapiToolDispatcher PollShopBuy is read-only (returns success but doesn't modify).
        // Let's fix that in SmapiToolDispatcher next, or just mock the result here?
        // Better to let Dispatcher modify state if it's "Real".
        // I will update SmapiToolDispatcher to actually deduct money and add item.
    }
}
