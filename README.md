# Stardew AI Agent Framework (v1)

> A decision AI framework for playing Stardew Valley autonomously through intelligent planning and execution.

## Overview

The Stardew AI Agent Framework is a robust system that enables autonomous gameplay through a closed-loop architecture combining **perception**, **decision-making (LLM)**, and **execution**. The framework translates natural language goals into structured plans, executes them through a graph-based runner, and adapts to failures through memory-driven replanning.

### Key Features

- **Plan Graph Execution**: Execute complex, branching plans with robust error handling
- **Memory-Driven Learning**: Learn from failures and adapt future decisions
- **Async Planning**: Non-blocking AI decision-making with "Thinking" state
- **Persistence**: Save and restore complete agent state
- **Budget & Stuck Detection**: Prevent infinite loops and resource waste
- **Transition-Based Control**: Flexible state transitions supporting retry, recovery, and replan flows
- **Decoupled Architecture**: Clean separation between game logic (SMAPI) and AI framework

## Architecture

The framework consists of **5 interconnected projects**:

```
┌─────────────────────────────────────────────────────────────┐
│                         Agent                                │
│  - AgentRuntime (Main coordinator)                          │
│  - Goal management & state machine                         │
└─────────────────────────────────────────────────────────────┘
         │                │                │
         ▼                ▼                ▼
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│     AI      │  │  Executor    │  │   Memory     │
│  - Planning │  │  - Execution │  │  - Facts     │
│  - Prompts  │  │  - Safety    │  │  - History   │
└──────────────┘  └──────────────┘  └──────────────┘
         │                │
         ▼                ▼
┌──────────────┐  ┌──────────────┐
│    Core      │  │    Tools     │
│  - Models    │  │  - Dispatcher│
│  - Interfaces│  │  - Schemas   │
│  - Registry  │  └──────────────┘
└──────────────┘
```

### Component Responsibilities

| Project | Purpose |
|---------|---------|
| **Core** | Shared data models, interfaces, tool registry |
| **AI** | LLM integration, prompt building, plan generation, embeddings |
| **Executor** | Plan graph execution, safety mechanisms, transition matching |
| **Tools** | Tool dispatching interface, stub implementations for testing |
| **Memory** | Fact storage, retrieval, vector search, learning from execution traces |
| **Agent** | Runtime orchestration, state machine, persistence |

## Quick Start

### Prerequisites

- .NET 8.0 or higher
- C# development environment (Visual Studio, Rider, or VS Code)

### Running the Demo

```bash
# Build the solution
dotnet build AI_STAR_AGENT.sln

# Run the demo simulation
dotnet run --project Agent/Agent.csproj
```

The demo simulates a "Full Day" autonomous cycle where the agent:
1. Starts with multiple goals (Socialize → Buy Seeds → Go Home)
2. Uses semantic memory retrieval for planning
3. Demonstrates LLM resilience with retry wrapper
4. Simulates time progression from 06:00 to 18:00
5. Performs save/load to verify persistence
6. Completes all daily tasks autonomously

### Basic Usage

```csharp
//1. Initialize components
var toolDispatcher = new ToolDispatcherStub();
var perceptionProvider = new MyPerceptionProvider(); // Your SMAPI implementation

//2. Setup LLM with retry resilience
var llmProvider = new GeminiProviderStub();
var robustLLM = new LLMRetryWrapper(llmProvider, maxRetries: 3, delayMs: 1000);
var plannerClient = new PlannerClient(robustLLM);

//3. Setup Embedding provider for semantic memory
var embeddingProvider = new EmbeddingProviderStub();

//4. Create agent with all components
var agent = new AgentRuntime("player_1", toolDispatcher, plannerClient, perceptionProvider, embeddingProvider);

//5. Set goal
agent.GoalStack.Push(new Goal
{
    Id = "goal_1",
    Text = "Buy 10 parsnip seeds from Pierre",
    Priority = 10
});

//6. Run in game tick loop
int tick = 0;
while (agent.Status != AgentStatus.Idle)
{
    agent.Update(tick++);
    await Task.Delay(10); // Simulate tick timing
}

//7. Save/Restore
string stateJson = agent.SaveState();
var newAgent = new AgentRuntime("player_1", toolDispatcher, plannerClient, perceptionProvider, embeddingProvider);
newAgent.LoadState(stateJson);
```

## Key Mechanisms

### 1. Async Planning

The framework uses a "Thinking" state to handle async LLM planning without blocking game ticks:

```
Idle → NeedPlan → Thinking (async) → Running → ...
                       ↓
                 planningTask running
```

**Benefits:**
- Non-blocking during LLM calls
- Game ticks continue during planning
- Clean state transitions

### 2. Plan Graph Execution

Plans are directed graphs of nodes with transitions:

```
┌──────────────┐
│  nav_shop    │ ok ──────────→ ┌──────────────┐
└──────────────┘               │ open_shop    │
  err:closed                   └──────────────┘
    ↓                              ↓ ok
┌──────────────┐              ┌──────────────┐
│ wait_until   │              │  buy_seeds   │
└──────────────┘              └──────────────┘
```

**Node Types:**
- `ToolCall`: Execute a tool with timeout
- `Recover`: Recovery strategy
- `LlmReplan`: Request new plan from AI
- `FinishGoal`: Mark goal complete

**Transition Expressions:**
- `"ok"`: Success
- `"timeout"`: Node timed out
- `"err:*"`: Any error
- `"err:closed|err:npc_missing"`: Specific errors
- `"ok|timeout"`: Logical OR (ignore failures)

### 3. Memory-Driven Learning

The AI learns from execution traces and adapts:

```csharp
// 1. Failure is logged
Memory.AddEntry(new MemoryEntry
{
    Content = "Tool 'ShopBuy' failed with 'menu_not_open'",
    Type = "execution_trace",
    Tags = ["ShopBuy", "menu_not_open", "execution_failure"],
    Importance = 4
});

// 2. Planner queries memory during planning
var recentFailures = memory.Query(new MemoryQuery
{
    Type = "execution_trace",
    Limit = 3
});

// 3. AI generates alternative plan based on failures
if (recentFailures.Any(m => m.Tags.Contains("closed")))
{
    // Plan to wait until shop opens
}
```

### 4. Informed Decision Making

The AI makes decisions based on:

1. **Tool Schemas**: Available capabilities (from ToolRegistry)
2. **Memory Facts**: Learned information (shop hours, preferences, failures)
3. **World State**: Current perception snapshot
4. **Execution History**: Recent tool results

```
┌─────────────────────────────────────────────────┐
│           Planning Decision                      │
├─────────────────────────────────────────────────┤
│  Tool Schemas     │  Available capabilities      │
│  Memory Facts     │  Shop hours, failures        │
│  World State      │  Location, inventory, gold   │
│  Last Results     │  Recent tool outcomes        │
├─────────────────────────────────────────────────┤
│           ↓                                    │
│  Generate PlanGraph with transitions           │
└─────────────────────────────────────────────────┘
```

### 5. Persistence

Complete agent state can be serialized and restored:

```csharp
// Save
string json = agent.SaveState();
// JSON includes: goals, current plan, memory, status, position

// Load
agent.LoadState(json);
// Restores complete execution context
```

## Safety Mechanisms

### BudgetManager

Prevents resource waste:
- `MaxReplans`: Limit replan attempts (default: 3)
- `MaxTotalTicks`: Limit execution time (default: 4000)
- `MaxGoldSpend`: Limit spending per goal (default: 400g)

### StuckDetector

Detects and prevents infinite loops:
- Position unchanged for N ticks
- Same error code repeated K times
- Same node executed M times

## Integration with SMAPI

### IPerceptionProvider Interface

Decouples game logic from AI framework:

```csharp
public interface IPerceptionProvider
{
    PerceptionSnapshot GetSnapshot(string agentId, int tickId);
}
```

**Implementation:** Create a class that reads from SMAPI game state:
```csharp
class SmapiPerceptionProvider : IPerceptionProvider
{
    public PerceptionSnapshot GetSnapshot(string agentId, int tickId)
    {
        var snapshot = new PerceptionSnapshot();
        snapshot.TileX = Game1.player.TilePoint.X;
        snapshot.TileY = Game1.player.TilePoint.Y;
        snapshot.Location = Game1.currentLocation.Name;
        snapshot.Gold = Game1.player.Money;
        // ... populate from SMAPI
        return snapshot;
    }
}
```

### IToolDispatcher Interface

Tools are game-specific implementations:

```csharp
public interface IToolDispatcher
{
    string Begin(string nodeId, string tool, Dictionary<string, object> args, int tickId);
    (bool ready, ToolResult? result) Poll(string handle, int tickId);
    void Cancel(string handle, int tickId);
}
```

**Example Implementation:**
```csharp
class SmapiToolDispatcher : IToolDispatcher
{
    public string Begin(string nodeId, string tool, Dictionary<string, object> args, int tickId)
    {
        // Start navigation using SMAPI pathfinding
        if (tool == "NavigateTo")
        {
            var location = args["Location"].ToString();
            // Start pathfinding...
            return taskId;
        }
        // ... handle other tools
    }

    public (bool ready, ToolResult? result) Poll(string handle, int tickId)
    {
        // Check if task completed, return result
    }
}
```

## Tool Registry

The framework provides schemas for available tools:

```csharp
var schemas = toolRegistry.GetAllSchemas();
// Each schema includes:
// - Name
// - Description
// - Parameters (name, type, required)
// - Possible errors (code, description)
```

**Standard Tools:**
- `NavigateTo`: Move to location/tile
- `ShopBuy`: Purchase items
- `WaitUntil`: Wait until condition
- `DumpItems`: Free inventory space
- `Interact`: Interact with object
- `TalkTo`: Talk to NPC
- `GiveGift`: Give gift to NPC

## Project Structure

```
AI_STAR_AGENT/
├── Core/              # Shared models and interfaces
│   ├── Models/         # Goal, PlanGraph, ToolResult, Memory, VectorEmbedding
│   ├── Interfaces/     # IPerceptionProvider, IToolDispatcher, ILLMProvider, IEmbeddingProvider
│   └── Registry/       # ToolRegistry
├── AI/                # LLM integration
│   ├── PlannerClient/  # Plan generation, LLM providers, retry logic
│   ├── PromptBuilder/  # Prompt construction
│   └── PlanParser/     # Response parsing
├── Executor/          # Execution engine
│   ├── PlanGraphRunner/    # Graph execution
│   ├── TransitionMatcher/   # Transition matching
│   ├── BudgetManager/      # Resource limits
│   └── StuckDetector/      # Loop detection
├── Tools/             # Tool interface and stubs
│   └── Implementations/    # ToolDispatcherStub
├── Memory/            # Memory system
│   └── MemoryStore/        # Fact storage, vector search, retrieval
└── Agent/             # Runtime and demo
    ├── AgentRuntime/       # Main coordinator
    └── Program.cs          # Full Day simulation demo
```

## Testing

Run test suites:

```bash
dotnet test Executor/PlanGraphRunner/PlanGraphRunnerTest.csproj
dotnet test Executor/TransitionMatcher/TransitionMatcherTest.csproj
dotnet test Tools/Implementations/ToolDispatcherStubTest.csproj
```

## Advanced Features (Phase 9-10)

### LLM Resilience (Phase 9)

The framework includes robust error handling for LLM interactions:

**ILLMProvider Interface:**
```csharp
public interface ILLMProvider
{
    Task<string> GetCompletionAsync(string prompt, string? systemInstruction = null);
}
```

**LLMRetryWrapper:**
Automatic retry logic for transient failures:
```csharp
var geminiProvider = new GeminiProviderStub();
var robustBrain = new LLMRetryWrapper(geminiProvider, maxRetries: 3, delayMs: 1000);
var plannerClient = new PlannerClient(robustBrain);
```

**Features:**
- Configurable retry count (default: 3)
- Exponential backoff delay
- Comprehensive error logging
- Pluggable providers (Gemini, GPT, Local LLMs)

### Vector/Semantic Memory (Phase 10)

Advanced semantic retrieval using vector embeddings:

**IEmbeddingProvider Interface:**
```csharp
public interface IEmbeddingProvider
{
    Task<VectorEmbedding> GetEmbeddingAsync(string text);
}
```

**Semantic Memory Search:**
```csharp
// Generate embedding for query
var goalEmbedding = await embeddingProvider.GetEmbeddingAsync("Buy seeds");

// Find semantically similar memories
var relevantContext = memory.Query(new MemoryQuery
{
    QueryEmbedding = goalEmbedding,
    MinSimilarity = 0.7,  // Cosine similarity threshold
    Limit = 5
});

// Returns memories sorted by similarity
```

**Cosine Similarity:**
- Compares vector embeddings for semantic meaning
- Range: -1 to 1 (1 = perfect match)
- Enables finding related facts even with different wording

**MemoryEntry Update:**
```csharp
public class MemoryEntry
{
    public VectorEmbedding? Embedding { get; set; }  // New field
    // ... other fields
}
```

**Benefits:**
- Finds facts by meaning, not just keywords
- Handles synonyms and related concepts
- Enables "fuzzy" memory retrieval
- Improves planning quality with relevant context

## Status

**Current Version:** v1 (Phase 10 Complete)

**Implemented:**
- ✅ Core architecture with 5 projects
- ✅ AgentRuntime with state machine
- ✅ Async planning with "Thinking" state
- ✅ Plan graph execution with TransitionMatcher
- ✅ Memory-driven learning from execution traces
- ✅ Budget and stuck detection
- ✅ Persistence (Save/Load)
- ✅ ToolRegistry with schemas
- ✅ ILLMProvider interface for pluggable LLMs
- ✅ LLMRetryWrapper for resilience
- ✅ Vector/Semantic memory with cosine similarity
- ✅ IEmbeddingProvider for embeddings
- ✅ "Full Day" simulation demo

**Next Steps:**
- [ ] Real SMAPI tool implementations (NavigateTo, ShopBuy, etc.)
- [ ] Real LLM integration (OpenAI/Anthropic/Local with embeddings)
- [ ] Multi-agent support (AI Town)
- [ ] Advanced memory (vector database, RAG)
- [ ] Schedule-aware planning

## License

See LICENSE file for details.

## Contributing

This is a research framework. Contributions welcome for:
- Real SMAPI tool implementations
- Additional tool schemas
- Improved safety mechanisms
- Advanced memory systems
- Documentation and examples
