# Stardew AI Agent Framework (v2)

> A decision AI framework for playing Stardew Valley autonomously through intelligent planning and execution.

## Overview

The Stardew AI Agent Framework is a robust system that enables autonomous gameplay through a closed-loop architecture combining **perception**, **decision-making (LLM)**, and **execution**. The framework translates natural language goals into structured plans, executes them through a graph-based runner, and adapts to failures through memory-driven replanning.

### Key Features

- **Plan Graph Execution**: Execute complex, branching plans with robust error handling
- **Memory-Driven Learning**: Learn from failures and adapt future decisions
- **Async Planning**: Non-blocking AI decision-making with "Thinking" state
- **Persistence**: Save and restore complete agent state
- **Budget & Stuck Detection**: Prevent infinite loops and resource waste
- **Real Perception**: Reads actual game state (Location, Inventory, Menu, Energy) via SMAPI
- **Real Action**: Executes game logic (Navigation, Interactions, Waiting) via SMAPI
- **Hybrid Intelligence**: Seamlessly switches between OpenAI (Online) and Stub (Offline) modes

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
         │                │                │
         ▼                ▼                ▼
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│    Core      │  │    Tools     │  │ Perception   │
│  - Models    │  │  - Dispatcher│  │  - Snapshot  │
│  - Interfaces│  │  - Schemas   │  │  - SMAPI     │
└──────────────┘  └──────────────┘  └──────────────┘
```

## Quick Start

### Prerequisites

- .NET 6.0 or higher (compatible with SMAPI)
- C# development environment (Visual Studio, Rider, or VS Code)

### Running the Integrated Demo

The project now includes a fully integrated simulation that mimics the Stardew Valley game loop.

1. **Offline Mode (Default)**: Uses a simulated "Stub" brain. Perfect for testing logic without spending API credits.
   ```bash
   dotnet run --project Agent/Agent.csproj
   ```

2. **Online Mode (Real Intelligence)**: Connects to OpenAI (or compatible) API.
   ```bash
   # Linux/macOS
   export OPENAI_API_KEY="sk-your-api-key"
   dotnet run --project Agent/Agent.csproj

   # Windows PowerShell
   $env:OPENAI_API_KEY="sk-your-api-key"
   dotnet run --project Agent/Agent.csproj
   ```

## Status

**Current Version:** v2 (Phases 1-3 Complete)

**Implemented:**
- ✅ **Real Perception**: `SmapiPerceptionProvider` reads actual game state (Location, Inventory, Menu, Energy).
- ✅ **Real Action**: `SmapiToolDispatcher` executes real game logic (Navigation, Interactions, Waiting).
- ✅ **Real Intelligence**: `OpenAIProvider` connects to GPT-4o (or compatible APIs).
- ✅ **Resilience**: Automatic retry policies and error handling for LLM calls.
- ✅ **Offline Mode**: Auto-fallback to Stub brain if no API key is provided.
- ✅ Core architecture with 5 projects (Core, AI, Executor, Tools, Perception).
- ✅ AgentRuntime with state machine & async planning.
- ✅ Plan graph execution with TransitionMatcher.
- ✅ Memory-driven learning from execution traces.
- ✅ Budget and stuck detection.
- ✅ Persistence (Save/Load).

**Next Steps:**
- [ ] Drop `Perception` project into a real SMAPI Mod folder to run in-game.
- [ ] Tune prompts for specific game scenarios.
- [ ] Expand toolset (fishing, farming, combat).

## License

See LICENSE file for details.
