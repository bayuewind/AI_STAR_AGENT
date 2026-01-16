using System;
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
            
            helper.ConsoleCommands.Add("agent_tool", "Run a tool directly. Usage: agent_tool <ToolName> [Key=Value]...", OnAgentToolCommand);
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            // Initialize Core Components
            var monitor = this.Monitor;
            
            // 1. Perception & Tools (Real SMAPI Implementations)
            // 1. Perception & Tools (Real SMAPI Implementations)
            var perception = new SmapiPerceptionProvider(monitor);
            _tools = new SmapiToolDispatcher(monitor, this.Helper.Reflection); 

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
            this.Monitor.Log("AI Agent ready. Press F5 to give a command.", LogLevel.Info);
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
                    this.Monitor.Log($"[DebugTool] Finished: {result?.Status} - {result?.Message}", LogLevel.Info);
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
            }
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
    }

    public class ModConfig
    {
        public string OpenAiApiKey { get; set; } = "";
        public string ModelName { get; set; } = "gpt-4o";
    }
}
