using Core.Models;

namespace Core.Interfaces;

/// <summary>
/// IPerceptionProvider - Interface for providing world state to the Agent.
/// In a real mod, this would be implemented by a class that reads from SMAPI.
/// </summary>
public interface IPerceptionProvider
{
    /// <summary>
    /// Gets a snapshot of the world for a specific agent at a specific tick.
    /// </summary>
    PerceptionSnapshot GetSnapshot(string agentId, int tickId);
}
