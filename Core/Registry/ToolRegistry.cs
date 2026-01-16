using Core.Models;
using System.Collections.Generic;

namespace Core.Registry;

public class ToolRegistry
{
    private readonly Dictionary<string, ToolSchema> _tools = new();

    public ToolRegistry()
    {
        InitializeStandardTools();
    }

    private void InitializeStandardTools()
    {
        // 1. NavigateTo
        Register(new ToolSchema
        {
            Name = "NavigateTo",
            Description = "Moves the player to a specific location or tile.",
            Parameters = new List<ToolParameter>
            {
                new ToolParameter { Name = "Location", Type = "string", Description = "Target location name (e.g., SeedShop, Farm)." },
                new ToolParameter { Name = "TileX", Type = "int", Description = "Target tile X coordinate.", Required = false },
                new ToolParameter { Name = "TileY", Type = "int", Description = "Target tile Y coordinate.", Required = false }
            },
            PossibleErrors = new List<ToolErrorSchema>
            {
                new ToolErrorSchema { Code = "unreachable", Description = "Path cannot be found." },
                new ToolErrorSchema { Code = "closed", Description = "Destination is locked or inaccessible." }
            }
        });

        // 2. ShopBuy
        Register(new ToolSchema
        {
            Name = "ShopBuy",
            Description = "Purchases items from an open shop menu.",
            Parameters = new List<ToolParameter>
            {
                new ToolParameter { Name = "ItemId", Type = "string", Description = "Internal ID of the item." },
                new ToolParameter { Name = "Amount", Type = "int", Description = "Number of items to buy." }
            },
            PossibleErrors = new List<ToolErrorSchema>
            {
                new ToolErrorSchema { Code = "menu_not_open", Description = "Shop menu is not currently active." },
                new ToolErrorSchema { Code = "insufficient_gold", Description = "Player does not have enough money." }
            }
        });

        // 3. WaitUntil
        Register(new ToolSchema
        {
            Name = "WaitUntil",
            Description = "Pauses execution until a specific game condition is met.",
            Parameters = new List<ToolParameter>
            {
                new ToolParameter { Name = "Time", Type = "string", Description = "Game time to wait for (e.g., 09:00)." }
            }
        });

        // 4. DumpItems
        Register(new ToolSchema
        {
            Name = "DumpItems",
            Description = "Destroys or drops items to free up inventory space.",
            Parameters = new List<ToolParameter>
            {
                new ToolParameter { Name = "Count", Type = "int", Description = "Number of slots to free." }
            }
        });

        // 5. Interact
        Register(new ToolSchema
        {
            Name = "Interact",
            Description = "Interacts with a nearby NPC or object.",
            Parameters = new List<ToolParameter>
            {
                new ToolParameter { Name = "TargetName", Type = "string", Description = "Name of the NPC or object." }
            },
            PossibleErrors = new List<ToolErrorSchema>
            {
                new ToolErrorSchema { Code = "too_far", Description = "Target is out of reach." },
                new ToolErrorSchema { Code = "no_response", Description = "Target did not respond." }
            }
        });

        // 6. TalkTo
        Register(new ToolSchema
        {
            Name = "TalkTo",
            Description = "Initiates a conversation with an NPC.",
            Parameters = new List<ToolParameter>
            {
                new ToolParameter { Name = "NPCName", Type = "string", Description = "Name of the NPC." }
            },
            PossibleErrors = new List<ToolErrorSchema>
            {
                new ToolErrorSchema { Code = "too_far", Description = "NPC is too far away." },
                new ToolErrorSchema { Code = "busy", Description = "NPC is currently busy." }
            }
        });

        // 7. GiveGift
        Register(new ToolSchema
        {
            Name = "GiveGift",
            Description = "Gives the currently held item to an NPC.",
            Parameters = new List<ToolParameter>
            {
                new ToolParameter { Name = "NPCName", Type = "string", Description = "Name of the NPC." },
                new ToolParameter { Name = "ItemId", Type = "string", Description = "ID of the item to give." }
            },
            PossibleErrors = new List<ToolErrorSchema>
            {
                new ToolErrorSchema { Code = "already_given", Description = "Already given 2 gifts this week." },
                new ToolErrorSchema { Code = "wrong_item", Description = "Cannot give this item." }
            }
        });

        // 8. MoveTo
        Register(new ToolSchema
        {
            Name = "MoveTo",
            Description = "Moves the player to a specific tile coordinate on the current map.",
            Parameters = new List<ToolParameter>
            {
                new ToolParameter { Name = "TileX", Type = "int", Description = "Target X tile coordinate.", Required = true },
                new ToolParameter { Name = "TileY", Type = "int", Description = "Target Y tile coordinate.", Required = true }
            },
            PossibleErrors = new List<ToolErrorSchema>
            {
                new ToolErrorSchema { Code = "unreachable", Description = "Target location is not reachable." },
                new ToolErrorSchema { Code = "timeout", Description = "Movement took too long." }
            }
        });

        // 9. MoveToNPC
        Register(new ToolSchema
        {
            Name = "MoveToNPC",
            Description = "Moves the player to the current location of an NPC.",
            Parameters = new List<ToolParameter>
            {
                new ToolParameter { Name = "NPCName", Type = "string", Description = "Name of the NPC.", Required = true }
            },
            PossibleErrors = new List<ToolErrorSchema>
            {
                new ToolErrorSchema { Code = "npc_not_found", Description = "NPC could not be found." },
                new ToolErrorSchema { Code = "unreachable", Description = "NPC location is not reachable." }
            }
        });
    }

    public void Register(ToolSchema schema)
    {
        _tools[schema.Name] = schema;
    }

    public List<ToolSchema> GetAllSchemas()
    {
        return new List<ToolSchema>(_tools.Values);
    }

    public ToolSchema? GetSchema(string name)
    {
        return _tools.TryGetValue(name, out var schema) ? schema : null;
    }
}
