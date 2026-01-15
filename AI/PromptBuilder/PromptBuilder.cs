using Core.Models;
using System.Text.Json;

namespace AI.PromptBuilder;

public class PromptBuilder
{
    public string BuildPrompt(string agentId, Goal goal, PerceptionSnapshot snapshot, List<MemoryEntry> memories, List<ToolSchema> toolSchemas)
    {
        var input = new
        {
            tick_id = snapshot.TickId,
            agent_id = agentId,
            goal = new { id = goal.Id, text = goal.Text, priority = goal.Priority },
            world = new
            {
                location = snapshot.Location,
                gold = snapshot.Gold,
                position = new { x = snapshot.TileX, y = snapshot.TileY },
                last_error = snapshot.LastErrorCode,
                time = snapshot.World.TimeOfDay,
                season = snapshot.World.Season,
                day = snapshot.World.DayOfMonth,
                weather = snapshot.World.Weather,
                is_festival = snapshot.World.IsFestivalDay
            },
            inventory = new
            {
                slots_used = snapshot.Inventory.Items.Count,
                max_slots = snapshot.Inventory.MaxSlots,
                items = snapshot.Inventory.Items.Select(i => new { i.Name, i.Stack, i.Category })
            },
            nearby_entities = snapshot.NearbyNPCs.Select(n => new { n.Name, n.Location, distance = Math.Sqrt(Math.Pow(n.TileX - snapshot.TileX, 2) + Math.Pow(n.TileY - snapshot.TileY, 2)) }),
            memory = memories.Select(m => new { m.Content, m.Type, m.Tags }),
            available_tools = toolSchemas.Select(s => new
            {
                s.Name,
                s.Description,
                parameters = s.Parameters.Select(p => new { p.Name, p.Type, p.Description, p.Required }),
                possible_errors = s.PossibleErrors.Select(e => new { e.Code, e.Description })
            })
        };

        return JsonSerializer.Serialize(input, new JsonSerializerOptions { WriteIndented = true });
    }
}
