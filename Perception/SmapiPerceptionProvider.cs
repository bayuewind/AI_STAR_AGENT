using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using Core.Models;
using Core.Interfaces;
using System;
using System.Collections.Generic;

namespace Perception;

public class SmapiPerceptionProvider : IPerceptionProvider
{
    private readonly IMonitor _monitor;

    public SmapiPerceptionProvider(IMonitor monitor)
    {
        _monitor = monitor;
    }

    public PerceptionSnapshot GetSnapshot(string agentId, int tickId)
    {
        // 安全检查：游戏世界是否准备好
        if (!Context.IsWorldReady)
        {
            return CreateEmptySnapshot(tickId);
        }

        var player = Game1.player;
        var location = Game1.currentLocation;

        return new PerceptionSnapshot
        {
            TickId = tickId,
            
            // 玩家位置（格子坐标，不是像素）
            TileX = player.TilePoint.X,
            TileY = player.TilePoint.Y,
            Location = location?.NameOrUniqueName ?? "Unknown",
            IsIndoors = location != null && !location.IsOutdoors,
            
            // 玩家资源
            Gold = player.Money,
            Energy = player.Stamina,
            MaxEnergy = player.MaxStamina,
            
            // 世界状态
            World = GetWorldState(),
            
            // 菜单状态
            MenuState = GetMenuState(),
            
            // 背包
            Inventory = GetInventory(player),
            
            // 附近 NPC
            NearbyNPCs = GetNearbyNPCs(player, location),
            
            // 上次错误（从工具结果传入，不在此处填充）
            LastErrorCode = null
        };
    }

    private WorldState GetWorldState()
    {
        // 在 Stub 模式下 Utility.isFestivalDay 可能不可用，这里做个简单的包装
        bool isFestival = false;
        try { isFestival = StardewValley.Utility.isFestivalDay(Game1.dayOfMonth, Game1.season); } catch {}

        return new WorldState
        {
            TimeOfDayRaw = Game1.timeOfDay,
            TimeOfDay = FormatTime(Game1.timeOfDay),
            Season = Game1.currentSeason,
            DayOfMonth = Game1.dayOfMonth,
            Year = Game1.year,
            DayOfWeek = GetDayOfWeek(Game1.dayOfMonth),
            Weather = GetWeather(),
            IsFestivalDay = isFestival,
            IsRaining = Game1.isRaining
        };
    }

    private MenuState GetMenuState()
    {
        var menu = Game1.activeClickableMenu;
        if (menu == null)
        {
            return new MenuState { IsOpen = false };
        }

        string menuType = menu.GetType().Name;
        return new MenuState
        {
            IsOpen = true,
            MenuType = menuType,
            IsShopMenu = menu is ShopMenu,
            IsDialogue = menu is DialogueBox
        };
    }

    private Inventory GetInventory(Farmer player)
    {
        var inventory = new Inventory
        {
            MaxSlots = player.MaxItems
        };

        if (player.Items != null)
        {
            foreach (var item in player.Items)
            {
                if (item != null)
                {
                    inventory.Items.Add(new Core.Models.Item
                    {
                        Id = item.ItemId,
                        Name = item.DisplayName,
                        Stack = item.Stack,
                        Category = item.Category.ToString(),
                        Price = item.salePrice()
                    });
                }
            }
        }

        return inventory;
    }

    private List<Core.Models.NPC> GetNearbyNPCs(Farmer player, GameLocation? location)
    {
        var npcs = new List<Core.Models.NPC>();
        if (location == null || location.characters == null) return npcs;

        foreach (var character in location.characters)
        {
            if (character is StardewValley.NPC npc)
            {
                // 只获取附近 10 格内的 NPC
                int distance = Math.Abs(npc.TilePoint.X - player.TilePoint.X) 
                             + Math.Abs(npc.TilePoint.Y - player.TilePoint.Y);
                
                if (distance <= 10)
                {
                    npcs.Add(new Core.Models.NPC
                    {
                        Name = npc.Name,
                        Location = location.NameOrUniqueName,
                        TileX = npc.TilePoint.X,
                        TileY = npc.TilePoint.Y,
                        IsVisible = true,
                        FriendshipLevel = player.getFriendshipLevelForNPC(npc.Name),
                        TalkedToday = player.hasPlayerTalkedToNPC(npc.Name),
                        GiftsGivenThisWeek = player.giftedItems != null && player.giftedItems.ContainsKey(npc.Name) 
                            ? player.giftedItems[npc.Name].Count : 0
                    });
                }
            }
        }

        return npcs;
    }

    private string FormatTime(int timeOfDay)
    {
        int hours = timeOfDay / 100;
        int minutes = timeOfDay % 100;
        return $"{hours:D2}:{minutes:D2}";
    }

    private string GetDayOfWeek(int dayOfMonth)
    {
        string[] days = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
        if (dayOfMonth < 1) return "Mon";
        return days[(dayOfMonth - 1) % 7];
    }

    private string GetWeather()
    {
        if (Game1.isRaining) return "Rain";
        if (Game1.isSnowing) return "Snow";
        if (Game1.isLightning) return "Storm";
        return "Sun";
    }

    private PerceptionSnapshot CreateEmptySnapshot(int tickId)
    {
        return new PerceptionSnapshot
        {
            TickId = tickId,
            TileX = 0,
            TileY = 0,
            Location = "Loading",
            Gold = 0,
            World = new WorldState(),
            MenuState = new MenuState { IsOpen = false },
            Inventory = new Inventory()
        };
    }
}
