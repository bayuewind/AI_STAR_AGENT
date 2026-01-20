using System;
using System.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using Agent;
using AI.PlannerClient;
using Perception;
using Tools.Implementations;
using Core.Interfaces;
using Core.Models;

namespace StardewAgentMod
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        private AgentRuntime? _agent;
        private SmapiToolDispatcher? _tools;
        private ModConfig _config = new();
        private int _tickCounter = 0;
        private string? _debugToolHandle = null;

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            this._config = helper.ReadConfig<ModConfig>();

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.Display.RenderedWorld += OnRenderedWorld;
            
            this.Monitor.Log("Registering console commands...", LogLevel.Info);
            helper.ConsoleCommands.Add("agent_tool", "Run a tool directly. Usage: agent_tool <ToolName> [Key=Value]...", OnAgentToolCommand);
            helper.ConsoleCommands.Add("agent_goal", "Give AI a goal. Usage: agent_goal <goal text>", OnAgentGoalCommand);
            helper.ConsoleCommands.Add("ai_world", "Dump world state. Usage: ai_world [filter]", OnWorldStateCommand);
            this.Monitor.Log("Console commands registered: agent_tool, agent_goal, ai_world", LogLevel.Info);
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            // Initialize Core Components
            var monitor = this.Monitor;
            
            // 1. Perception & Tools (Real SMAPI Implementations)
            // 1. Perception & Tools (Real SMAPI Implementations)
            var perception = new SmapiPerceptionProvider(monitor);
            _tools = new SmapiToolDispatcher(monitor, this.Helper.DirectoryPath, this.Helper.Reflection); 

            // 2. Brain (LLM)
            ILLMProvider llmProvider;
            if (!string.IsNullOrEmpty(this._config.OpenAiApiKey))
            {
                monitor.Log("🧠 AI Agent: Using OpenAI Brain", LogLevel.Info);
                llmProvider = new OpenAIProvider(this._config.OpenAiApiKey, this._config.ModelName);
            }
            else
            {
                monitor.Log("🧠 AI Agent: No API Key found. Using Stub Brain (Offline Mode)", LogLevel.Alert);
                llmProvider = new GeminiProviderStub();
            }
            var robustBrain = new LLMRetryWrapper(llmProvider);
            var planner = new PlannerClient(robustBrain);

            // 3. Embedding (Stub for now, or Real if implemented)
            var embedding = new EmbeddingProviderStub();

            // 4. Create Runtime
            // 4. Create Runtime
            _agent = new AgentRuntime("player_1", _tools, planner, perception, embedding);
            
            monitor.Log("🤖 Stardew AI Agent initialized!", LogLevel.Info);
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            // Reset agent state on load
            _agent?.GoalStack.Clear();
            
            // Hook events for world state cache invalidation
            this.Helper.Events.Player.Warped += OnPlayerWarped;
            this.Helper.Events.World.ObjectListChanged += OnObjectListChanged;
            this.Helper.Events.World.TerrainFeatureListChanged += OnTerrainFeatureListChanged;
            this.Helper.Events.World.BuildingListChanged += OnBuildingListChanged;
            this.Helper.Events.World.FurnitureListChanged += OnFurnitureListChanged;
            
            this.Monitor.Log("AI Agent ready. Press F5 to give a command.", LogLevel.Info);
        }

        private void OnPlayerWarped(object? sender, WarpedEventArgs e)
        {
            _tools?.InvalidateWorldCache();
        }

        private void OnObjectListChanged(object? sender, ObjectListChangedEventArgs e)
        {
            if (e.Location == Game1.currentLocation)
                _tools?.InvalidateWorldCache();
        }

        private void OnTerrainFeatureListChanged(object? sender, TerrainFeatureListChangedEventArgs e)
        {
            if (e.Location == Game1.currentLocation)
                _tools?.InvalidateWorldCache();
        }

        private void OnBuildingListChanged(object? sender, BuildingListChangedEventArgs e)
        {
            if (e.Location == Game1.currentLocation)
                _tools?.InvalidateWorldCache();
        }

        private void OnFurnitureListChanged(object? sender, FurnitureListChangedEventArgs e)
        {
            if (e.Location == Game1.currentLocation)
                _tools?.InvalidateWorldCache();
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady || _agent == null)
                return;

            // Run agent update every 30 ticks (approx 0.5s) to save performance
            // or every tick if precise control is needed.
            // SmapiToolDispatcher needs frequent polling for smooth movement.
            _tickCounter++;
            
            // Poll tools every tick
            // In the architecture, AgentRuntime.Update() calls dispatcher.Poll() internally via Runner.
            // So we just call Update().
            
            _agent.Update(_tickCounter);
            
            // Poll Debug Tool
            if (_debugToolHandle != null && _tools != null)
            {
                var (ready, result) = _tools.Poll(_debugToolHandle, _tickCounter);
                if (ready)
                {
                    this.Monitor.Log($"[DebugTool] Finished: {(result?.Ok == true ? "Success" : "Failed")} - {(result?.Error?.Detail ?? "Completed")}", LogLevel.Info);
                    
                    // Print return data for verification
                    if (result?.Telemetry != null && result.Telemetry.Count > 0)
                    {
                         this.Monitor.Log("  Data:", LogLevel.Info);
                         foreach(var kvp in result.Telemetry)
                         {
                             if (kvp.Value is System.Collections.IEnumerable collection && !(kvp.Value is string))
                             {
                                 this.Monitor.Log($"    {kvp.Key}:", LogLevel.Info);
                                 int count = 0;
                                 foreach(var item in collection)
                                 {
                                     if (item is Dictionary<string, object> dict)
                                     {
                                         // Special formatting for Item Info
                                         string slot = dict.ContainsKey("slotIndex") ? $"[{dict["slotIndex"]}]" : "";
                                         string name = dict.ContainsKey("name") ? dict["name"].ToString() : "Empty";
                                         string stack = dict.ContainsKey("stack") ? $"x{dict["stack"]}" : "";
                                         string active = dict.ContainsKey("isActive") && (bool)dict["isActive"] ? "(Active)" : "";
                                         
                                         if (name != "Empty")
                                             this.Monitor.Log($"      - {slot} {name} {stack} {active}", LogLevel.Info);
                                     }
                                     else
                                     {
                                         this.Monitor.Log($"      - {item}", LogLevel.Info);
                                     }
                                     count++;
                                 }
                                 if (count == 0) this.Monitor.Log("      (StartEmpty)", LogLevel.Info);
                             }
                             else
                             {
                                 this.Monitor.Log($"    {kvp.Key}: {kvp.Value}", LogLevel.Info);
                             }
                         }
                    }
                    
                    _debugToolHandle = null;
                }
            }
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady || _agent == null) return;

            // Simple Interaction: F5 to add a test goal
            if (e.Button == SButton.F5)
            {
                this.Monitor.Log("Commander: Giving order 'Buy Parsnip Seeds'", LogLevel.Info);
                _agent.GoalStack.Push(new Goal 
                { 
                    Id = Guid.NewGuid().ToString(), 
                    Text = "Go to Pierre's and buy 5 Parsnip Seeds", 
                    Priority = 10 
                });
            }
            
            // F6 to Stop
            if (e.Button == SButton.F6)
            {
                this.Monitor.Log("Commander: STOP ALL", LogLevel.Warn);
                _agent.GoalStack.Clear();
                // We might need an Agent.Stop() method to cancel running tools immediately
            }
        }

        private void OnRenderedWorld(object? sender, StardewModdingAPI.Events.RenderedWorldEventArgs e)
        {
            if (!Context.IsWorldReady || _tools == null)
                return;
            
            // Draw path visualization
            _tools.DrawPathIndicator(e.SpriteBatch);
        }

        private void OnAgentToolCommand(string command, string[] args)
        {
            if (_tools == null)
            {
                this.Monitor.Log("Tools not initialized.", LogLevel.Error);
                return;
            }
            if (args.Length < 1)
            {
                this.Monitor.Log("Usage: agent_tool <ToolName> [Key=Value]...", LogLevel.Error);
                return;
            }

            string toolName = args[0];
            var toolArgs = new Dictionary<string, object>();
            
            for(int i=1; i<args.Length; i++)
            {
                var parts = args[i].Split('=');
                if (parts.Length == 2)
                {
                    toolArgs[parts[0]] = parts[1];
                }
            }

            try 
            {
                _debugToolHandle = _tools.Begin("debug_node", toolName, toolArgs, _tickCounter);
                this.Monitor.Log($"[DebugTool] Started {toolName} (Handle: {_debugToolHandle})", LogLevel.Info);
            }
            catch(Exception ex)
            {
                this.Monitor.Log($"[DebugTool] Error starting tool: {ex.Message}", LogLevel.Error);
            }
        }

        private void OnAgentGoalCommand(string command, string[] args)
        {
            if (_agent == null)
            {
                this.Monitor.Log("Agent not initialized.", LogLevel.Error);
                return;
            }
            
            if (args.Length < 1)
            {
                this.Monitor.Log("Usage: agent_goal <goal text>", LogLevel.Error);
                this.Monitor.Log("Example: agent_goal Go to Pierre's and buy 5 Parsnip Seeds", LogLevel.Info);
                return;
            }

            // Join all arguments as the goal text
            string goalText = string.Join(" ", args);
            
            var goal = new Goal 
            { 
                Id = Guid.NewGuid().ToString(), 
                Text = goalText, 
                Priority = 10 
            };
            
            _agent.GoalStack.Push(goal);
            this.Monitor.Log($"✅ Goal added: {goalText}", LogLevel.Info);
        }

        private void OnWorldStateCommand(string command, string[] args)
        {
            if (_tools == null)
            {
                this.Monitor.Log("Tools not initialized.", LogLevel.Error);
                return;
            }

            string? filter = args.Length > 0 ? args[0] : null;
            
            try
            {
                var snapshot = _tools.GetWorldStateSnapshot(filter);
                
                if (snapshot == null)
                {
                    this.Monitor.Log("Failed to get world state snapshot.", LogLevel.Error);
                    return;
                }
                
                this.Monitor.Log($"=== World State: {snapshot.Location} ({snapshot.Width}x{snapshot.Height}) ===", LogLevel.Info);
                this.Monitor.Log($"Player at: ({snapshot.PlayerX}, {snapshot.PlayerY})", LogLevel.Info);
                this.Monitor.Log($"Tick: {snapshot.Tick}", LogLevel.Info);
                
                // Show legend
                this.Monitor.Log($"Legend: {string.Join(", ", snapshot.Legend.Select(kv => $"{kv.Key}={kv.Value}"))}", LogLevel.Info);
                
                // Show entity count per type
                var groupedEntities = snapshot.Entities
                    .GroupBy(e => e.Kind)
                    .OrderByDescending(g => g.Count())
                    .Take(10);
                    
                this.Monitor.Log($"Entities ({snapshot.Entities.Count} total):", LogLevel.Info);
                foreach (var group in groupedEntities)
                {
                    this.Monitor.Log($"  {group.Key}: {group.Count()}", LogLevel.Info);
                }
                
                // Show first 15 entities with details
                this.Monitor.Log("Sample entities:", LogLevel.Info);
                foreach (var entity in snapshot.Entities.Take(15))
                {
                    string meta = "";
                    if (entity.Meta != null && entity.Meta.Count > 0)
                    {
                        meta = " [" + string.Join(", ", entity.Meta.Take(3).Select(kv => $"{kv.Key}={kv.Value}")) + "]";
                    }
                    this.Monitor.Log($"  ({entity.X},{entity.Y}) {entity.Kind}: {entity.Name ?? entity.Id}{meta}", LogLevel.Info);
                }
                
                if (snapshot.Entities.Count > 15)
                {
                    this.Monitor.Log($"  ... and {snapshot.Entities.Count - 15} more", LogLevel.Info);
                }
            }
            catch (Exception ex)
            {
                this.Monitor.Log($"Error getting world state: {ex.Message}", LogLevel.Error);
            }
        }
    }

    public class ModConfig
    {
        public string OpenAiApiKey { get; set; } = "";
        public string ModelName { get; set; } = "gpt-4o";
    }
}

