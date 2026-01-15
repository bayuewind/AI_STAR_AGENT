using Core.Models;
using Core.Interfaces;
using Core.Registry;
using Executor.PlanGraphRunner;
using Executor.BudgetManager;
using Executor.StuckDetector;
using AI.PlannerClient;
using Memory.MemoryStore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Agent;

public enum AgentStatus
{
    Idle,
    NeedPlan,
    Thinking,
    Running,
    WaitingTool,
    NeedReplan,
    Done,
    Error
}

public class AgentRuntime
{
    public string AgentId { get; }
    public GoalStack GoalStack { get; } = new();
    public AgentStatus Status { get; private set; } = AgentStatus.Idle;
    public MemoryStore Memory { get; } = new();
    public ToolRegistry ToolRegistry { get; } = new();

    private readonly IToolDispatcher _toolDispatcher;
    private readonly PlannerClient _plannerClient;
    private readonly IPerceptionProvider _perceptionProvider;
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly BudgetManager _budgetManager = new();
    private readonly StuckDetector _stuckDetector = new();
    private readonly List<string> _planHistory = new();
    
    private PlanGraphRunner? _runner;
    private Goal? _currentGoal;
    private int _tickId = 0;
    private PerceptionSnapshot? _lastSnapshot;
    private Task<PlanGraph?>? _planningTask;

    public AgentRuntime(string agentId, IToolDispatcher toolDispatcher, PlannerClient plannerClient, IPerceptionProvider perceptionProvider, IEmbeddingProvider embeddingProvider)
    {
        AgentId = agentId;
        _toolDispatcher = toolDispatcher;
        _plannerClient = plannerClient;
        _perceptionProvider = perceptionProvider;
        _embeddingProvider = embeddingProvider;
    }

    public void Update(int tickId)
    {
        _tickId = tickId;
        var snapshot = _perceptionProvider.GetSnapshot(AgentId, tickId);
        _lastSnapshot = snapshot;

        if (Status == AgentStatus.Thinking)
        {
            if (_planningTask != null && _planningTask.IsCompleted)
            {
                OnPlanningTaskCompleted(_planningTask.Result);
                _planningTask = null;
            }
            return;
        }

        UpdateStatus(snapshot);
        ExecuteStateAction(snapshot);
    }

    private void UpdateStatus(PerceptionSnapshot snapshot)
    {
        if (GoalStack.IsEmpty && _currentGoal == null)
        {
            if (Status != AgentStatus.Idle)
            {
                Console.WriteLine($"[AgentRuntime] No goals. Entering Idle state.");
                Status = AgentStatus.Idle;
            }
            return;
        }

        if (_currentGoal == null)
        {
            _currentGoal = GoalStack.Pop();
            if (_currentGoal != null)
            {
                Console.WriteLine($"[AgentRuntime] New Goal popped: {_currentGoal.Text} (Priority: {_currentGoal.Priority})");
                _budgetManager.Reset(_tickId, snapshot.Gold);
                _stuckDetector.Reset();
                _planHistory.Clear();
                Status = AgentStatus.NeedPlan;
            }
            return;
        }

        if (Status == AgentStatus.Running || Status == AgentStatus.WaitingTool)
        {
            if (_budgetManager.IsExceeded(_tickId, snapshot.Gold, out var budgetReason))
            {
                Console.WriteLine($"[AgentRuntime] Budget exceeded: {budgetReason}. Aborting goal.");
                Status = AgentStatus.Error;
                return;
            }

            bool isWaitingTask = _runner?.CurrentToolName == "WaitUntil";
            _stuckDetector.Update(snapshot.TileX, snapshot.TileY, snapshot.Location, snapshot.LastErrorCode, _runner?.CurrentNodeId);
            if (_stuckDetector.IsStuck(out var stuckReason, ignorePosition: isWaitingTask, ignoreErrors: isWaitingTask))
            {
                Console.WriteLine($"[AgentRuntime] Stuck detected: {stuckReason}. Forcing replan.");
                TriggerReplan();
                return;
            }
        }
    }

    private void TriggerReplan()
    {
        Status = AgentStatus.NeedReplan;
        
        var lastResult = _runner?.LastToolResult;
        if (lastResult != null && !lastResult.Ok && lastResult.Error != null)
        {
            string errorCode = lastResult.Error.Code;
            var tags = new List<string> { lastResult.Tool, errorCode, "execution_failure" };
            
            if (lastResult.Telemetry.TryGetValue("location", out var loc)) tags.Add(loc.ToString() ?? "");

            Memory.AddEntry(new MemoryEntry
            {
                Content = $"Failed execution: Tool '{lastResult.Tool}' failed with code '{errorCode}' at node '{lastResult.ActionNodeId}'.",
                Type = "execution_trace",
                Tags = tags,
                Importance = 4
            });
            Console.WriteLine($"[AgentRuntime] Logged execution trace: {lastResult.Tool} failed -> {errorCode}.");
        }
    }

    private void ExecuteStateAction(PerceptionSnapshot snapshot)
    {
        switch (Status)
        {
            case AgentStatus.NeedPlan:
                PerformPlanning(snapshot);
                break;

            case AgentStatus.Running:
            case AgentStatus.WaitingTool:
                if (_runner != null)
                {
                    var prevNodeId = _runner.CurrentNodeId;
                    var runnerStatus = _runner.Tick(_tickId);
                    
                    if (runnerStatus == RunnerStatus.NeedReplan)
                    {
                        TriggerReplan();
                    }
                    else
                    {
                        SyncRunnerStatus(runnerStatus);
                        
                        var lastResult = _runner.LastToolResult;
                        if (lastResult != null && lastResult.Ok && lastResult.Delta != null)
                        {
                            ApplyActionDelta(lastResult.Delta);
                        }
                    }

                    if (prevNodeId != _runner.CurrentNodeId && _runner.CurrentNodeId != null)
                    {
                        _stuckDetector.RecordNodeVisit(_runner.CurrentNodeId);
                    }
                }
                break;

            case AgentStatus.NeedReplan:
                _budgetManager.RecordReplan();
                PerformPlanning(snapshot);
                break;

            case AgentStatus.Done:
                Console.WriteLine($"[AgentRuntime] Goal completed: {_currentGoal?.Text}");
                _currentGoal = null;
                Status = AgentStatus.Idle;
                break;

            case AgentStatus.Error:
                Console.WriteLine($"[AgentRuntime] Goal aborted due to error or budget: {_currentGoal?.Text}");
                _currentGoal = null;
                Status = AgentStatus.Idle;
                break;

            case AgentStatus.Idle:
            default:
                break;
        }
    }

    private void ApplyActionDelta(ActionDelta delta)
    {
        if (delta.GoldChanged != 0)
        {
            Console.WriteLine($"[AgentRuntime] Delta: Gold changed by {delta.GoldChanged}");
        }
        
        foreach (var item in delta.ItemsChanged)
        {
            Memory.AddEntry(new MemoryEntry
            {
                Content = $"Inventory change: {item.ItemId} by {item.CountChanged}.",
                Type = "event",
                Tags = new List<string> { item.ItemId, "inventory_change" },
                Importance = 3
            });
            Console.WriteLine($"[AgentRuntime] Delta: Inventory changed ({item.ItemId}: {item.CountChanged})");
        }

        if (delta.LocationChanged != null)
        {
            Console.WriteLine($"[AgentRuntime] Delta: Moved to {delta.LocationChanged}");
        }
    }

    private void PerformPlanning(PerceptionSnapshot snapshot)
    {
        if (_currentGoal == null) return;

        _stuckDetector.ResetErrorCount();
        Status = AgentStatus.Thinking;
        _planningTask = _plannerClient.PlanAsync(AgentId, _currentGoal, snapshot, Memory, ToolRegistry, _tickId, _embeddingProvider);
        Console.WriteLine($"[AgentRuntime] Started asynchronous planning task for goal: {_currentGoal.Text}");
    }

    private void OnPlanningTaskCompleted(PlanGraph? plan)
    {
        if (plan != null && plan.Validate())
        {
            string planSig = $"{plan.StartNodeId}_{string.Join("-", plan.Nodes.Select(n => n.Tool))}";
            var recentFailures = Memory.Query(new MemoryQuery { Type = "execution_trace", Limit = 3 });

            if (_planHistory.Count > 0 && _planHistory.Last() == planSig && recentFailures.Any())
            {
                Console.WriteLine($"[AgentRuntime] Logic Loop Detected: Generated identical plan structure despite recent failure. Aborting goal.");
                Status = AgentStatus.Error;
                return;
            }
            
            _planHistory.Add(planSig);
            if (_planHistory.Count > 10) _planHistory.RemoveAt(0);

            if (_runner == null)
            {
                _runner = new PlanGraphRunner(plan, _toolDispatcher);
            }
            else
            {
                _runner.ReplacePlan(plan, _tickId);
            }
            Status = AgentStatus.Running;
        }
        else
        {
            Console.WriteLine("[AgentRuntime] Planning failed");
            Status = AgentStatus.Error;
        }
    }

    private void SyncRunnerStatus(RunnerStatus runnerStatus)
    {
        Status = runnerStatus switch
        {
            RunnerStatus.Running => AgentStatus.Running,
            RunnerStatus.WaitingTool => AgentStatus.WaitingTool,
            RunnerStatus.NeedReplan => AgentStatus.NeedReplan,
            RunnerStatus.Done => AgentStatus.Done,
            _ => AgentStatus.Error
        };
    }

    #region Persistence

    public string SaveState()
    {
        var dto = new AgentStateDTO
        {
            AgentId = this.AgentId,
            Goals = this.GoalStack.GetAll().ToList(),
            CurrentGoal = this._currentGoal,
            Status = this.Status,
            PlanHistory = this._planHistory,
            Memories = this.Memory.Query(new MemoryQuery { Limit = 1000 }),
            CurrentPlan = this._runner?.GetPlanGraph(),
            CurrentNodeId = this._runner?.CurrentNodeId
        };

        return JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
    }

    public void LoadState(string json)
    {
        var dto = JsonSerializer.Deserialize<AgentStateDTO>(json);
        if (dto == null) return;

        this.GoalStack.Clear();
        foreach (var g in dto.Goals) this.GoalStack.Push(g);

        this._currentGoal = dto.CurrentGoal;
        this.Status = dto.Status;
        this._planHistory.Clear();
        this._planHistory.AddRange(dto.PlanHistory);

        this.Memory.Clear();
        foreach (var m in dto.Memories) this.Memory.AddEntry(m);

        if (dto.CurrentPlan != null)
        {
            _runner = new PlanGraphRunner(dto.CurrentPlan, _toolDispatcher);
        }

        Console.WriteLine($"[AgentRuntime] State loaded for agent: {AgentId}. Status: {Status}. Plan: {dto.CurrentPlan?.PlanId ?? "None"}");
    }

    private class AgentStateDTO
    {
        public string AgentId { get; set; } = string.Empty;
        public List<Goal> Goals { get; set; } = new();
        public Goal? CurrentGoal { get; set; }
        public AgentStatus Status { get; set; }
        public List<string> PlanHistory { get; set; } = new();
        public List<MemoryEntry> Memories { get; set; } = new();
        public PlanGraph? CurrentPlan { get; set; }
        public string? CurrentNodeId { get; set; }
    }

    #endregion
}
