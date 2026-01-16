using StardewValley;
using StardewValley.Tools;
using StardewValley.Pathfinding;

namespace Tools.WaterBot;

/// <summary>
/// Status of the WaterBot execution.
/// </summary>
public enum WaterBotStatus
{
    Idle,
    LoadingMap,
    FindingGroups,
    Planning,
    Watering,
    Refilling,
    Completed,
    Stopped,
    Exhausted,
    NoWater,
    Error
}

/// <summary>
/// Defines the process of the Bot being active.
/// Redesigned for AI Agent interaction - provides async status polling.
/// </summary>
public class WaterBotController
{
    private readonly SmapiMonitor _monitor;
    private readonly WaterBotConfig _config;
    
    public bool IsActive { get; private set; }
    public bool IsCompleted { get; private set; }
    public WaterBotStatus Status { get; private set; }
    public string StatusMessage { get; private set; } = "";
    public int CropsWatered { get; private set; } = 0;
    public int TotalCrops { get; private set; } = 0;

    private CropMap _map;
    private int _currentGroup;
    private List<Group> _path;
    private int _currentTile;
    private List<ActionableTile> _order;
    private ActionableTile? _refillStation;

    public WaterBotController(SmapiMonitor monitor, WaterBotConfig? config = null)
    {
        _monitor = monitor;
        _config = config ?? new WaterBotConfig();
        _map = new CropMap();
        _path = new List<Group>();
        _order = new List<ActionableTile>();
        Status = WaterBotStatus.Idle;
        _console = msg => Log(msg);  // Wrapper for ConsoleLog delegate
    }

    // Wrapper for CropMap methods that need ConsoleLog delegate
    private readonly ConsoleLog _console;

    /// <summary>
    /// Start the watering process. Called by AI Agent.
    /// </summary>
    public bool Start()
    {
        if (IsActive)
        {
            Log("WaterBot is already active");
            return false;
        }

        // Check if player has watering can equipped
        if (Game1.player?.CurrentItem is not WateringCan)
        {
            StatusMessage = "Please equip a watering can first";
            Status = WaterBotStatus.Error;
            Log(StatusMessage, SmapiLogLevel.Warn);
            return false;
        }

        IsActive = true;
        IsCompleted = false;
        CropsWatered = 0;
        Status = WaterBotStatus.LoadingMap;
        StatusMessage = "Loading map data...";
        Log("Starting WaterBot");

        try
        {
            // Load map data
            _map.loadMap();
            TotalCrops = _map.waterableTiles.Count;

            if (TotalCrops == 0)
            {
                StatusMessage = "No crops need watering";
                Status = WaterBotStatus.Completed;
                IsCompleted = true;
                IsActive = false;
                Log(StatusMessage);
                return true;
            }

            Status = WaterBotStatus.FindingGroups;
            StatusMessage = $"Found {TotalCrops} crops, finding groups...";

            // Group waterable tiles
            List<Group> groupings;
            if (_config.UseSmallGrouping)
            {
                groupings = _map.getMinimalCoverGroups();
            }
            else
            {
                groupings = _map.findGroupings(_console);
            }

            if (!IsActive) return false;

            _currentGroup = 0;
            _currentTile = 0;

            Status = WaterBotStatus.Planning;
            StatusMessage = "Planning optimal path...";

            _path = _map.findGroupPath(_console, groupings);

            if (_path.Count == 0)
            {
                StatusMessage = "Unable to find path to crops";
                Status = WaterBotStatus.Error;
                IsActive = false;
                return false;
            }

            Status = WaterBotStatus.Watering;
            StatusMessage = $"Watering crops... (0/{TotalCrops})";

            _order = _map.findFillPath(_path[_currentGroup], _console);

            if (!IsActive) return false;

            // Check if we need to refill first
            if (((WateringCan)Game1.player.CurrentTool).WaterLeft <= 0)
            {
                RefillWater();
                return true;
            }

            // Start moving to first tile
            Game1.player.controller = new PathFindController(
                Game1.player, 
                Game1.currentLocation, 
                _order[_currentTile].getStand(), 
                2, 
                StartWatering
            );

            return true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            Status = WaterBotStatus.Error;
            IsActive = false;
            Log($"Error starting WaterBot: {ex}", SmapiLogLevel.Error);
            return false;
        }
    }

    /// <summary>
    /// Begins the process of watering current actionable tile.
    /// </summary>
    private void StartWatering(Character c, SdvGameLocation location)
    {
        Game1.player.controller = null;

        if (!IsActive) return;

        if (Game1.player.Stamina <= 2f)
        {
            Exhausted();
            return;
        }

        if (((WateringCan)Game1.player.CurrentTool).WaterLeft <= 0)
        {
            RefillWater();
            return;
        }

        XnaPoint point = _order[_currentTile].Pop();

        if (point.X != -1)
        {
            Water(point);
            CropsWatered++;
            StatusMessage = $"Watering crops... ({CropsWatered}/{TotalCrops})";
            Task.Delay(800).ContinueWith(o => { StartWatering(c, location); });
        }
        else
        {
            Navigate();
        }
    }

    /// <summary>
    /// Waters a tile.
    /// </summary>
    private void Water(XnaPoint tile)
    {
        if (Game1.player.TilePoint.Y > tile.Y)
        {
            Game1.player.FacingDirection = 0;
        }
        else if (Game1.player.TilePoint.Y < tile.Y)
        {
            Game1.player.FacingDirection = 2;
        }
        else if (Game1.player.TilePoint.X > tile.X)
        {
            Game1.player.FacingDirection = 3;
        }
        else if (Game1.player.TilePoint.X < tile.X)
        {
            Game1.player.FacingDirection = 1;
        }

        if (Game1.player.isEmoteAnimating)
        {
            Game1.player.EndEmoteAnimation();
        }

        Game1.player.FarmerSprite.SetOwner(Game1.player);
        Game1.player.CanMove = false;
        Game1.player.UsingTool = true;
        Game1.player.canReleaseTool = true;

        Game1.player.Halt();
        Game1.player.CurrentTool.Update(Game1.player.FacingDirection, 0, Game1.player);

        Game1.player.stopJittering();
        Game1.player.canReleaseTool = false;

        int addedAnimationMultiplayer = ((!(Game1.player.Stamina <= 0f)) ? 1 : 2);
        if (Game1.isAnyGamePadButtonBeingPressed() || !Game1.player.IsLocalPlayer)
        {
            Game1.player.lastClick = Game1.player.GetToolLocation();
        }

        if (((WateringCan)Game1.player.CurrentTool).WaterLeft > 0 && Game1.player.ShouldHandleAnimationSound())
        {
            Game1.player.currentLocation.localSound("wateringCan");
        }

        Game1.player.lastClick = new XnaVector2(tile.X * Game1.tileSize, tile.Y * Game1.tileSize);

        switch (Game1.player.FacingDirection)
        {
            case 2:
                ((FarmerSprite)Game1.player.Sprite).animateOnce(164, 125f * (float)addedAnimationMultiplayer, 3);
                break;
            case 1:
                ((FarmerSprite)Game1.player.Sprite).animateOnce(172, 125f * (float)addedAnimationMultiplayer, 3);
                break;
            case 0:
                ((FarmerSprite)Game1.player.Sprite).animateOnce(180, 125f * (float)addedAnimationMultiplayer, 3);
                break;
            case 3:
                ((FarmerSprite)Game1.player.Sprite).animateOnce(188, 125f * (float)addedAnimationMultiplayer, 3);
                break;
        }

        _map.map[tile.Y][tile.X].waterable = false;
    }

    /// <summary>
    /// Navigates to the next point.
    /// </summary>
    private void Navigate()
    {
        if (!IsActive) return;

        _currentTile += 1;

        if (_currentTile >= _order.Count)
        {
            _currentGroup += 1;
            _currentTile = 0;

            if (_currentGroup >= _path.Count)
            {
                if (_config.RefillOnFinish && _config.RefillIfLower * 0.01 > (float)((WateringCan)Game1.player.CurrentTool).WaterLeft / (float)((WateringCan)Game1.player.CurrentTool).waterCanMax)
                {
                    RefillWater();
                    return;
                }
                End();
                return;
            }

            _order = _map.findFillPath(_path[_currentGroup], _console);
        }

        Game1.player.controller = new PathFindController(
            Game1.player, 
            Game1.currentLocation, 
            _order[_currentTile].getStand(), 
            2, 
            StartWatering
        );
    }

    private void NavigateNoUpdate()
    {
        Game1.player.controller = new PathFindController(
            Game1.player, 
            Game1.currentLocation, 
            _order[_currentTile].getStand(), 
            2, 
            StartWatering
        );
    }

    private void RefillWater()
    {
        if (!IsActive) return;

        Status = WaterBotStatus.Refilling;
        StatusMessage = "Refilling watering can...";

        Tile playerLocation = _map.map[Game1.player.TilePoint.Y][Game1.player.TilePoint.X];
        _refillStation = _map.getClosestRefill(playerLocation, _console);

        if (!IsActive) return;

        if (_refillStation != null)
        {
            Game1.player.controller = new PathFindController(
                Game1.player, 
                Game1.currentLocation, 
                _refillStation.getStand(), 
                2, 
                StartRefilling
            );
        }
        else
        {
            NoWater();
        }
    }

    private void StartRefilling(Character c, SdvGameLocation location)
    {
        Game1.player.controller = null;

        if (!IsActive) return;

        if (Game1.player.Stamina <= 2f)
        {
            Exhausted();
            return;
        }

        XnaPoint point = _refillStation?.Pop() ?? new XnaPoint(-1, -1);

        if (point.X != -1)
        {
            Water(point);
            Task.Delay(800).ContinueWith(o =>
            {
                Status = WaterBotStatus.Watering;
                if (_config.RedoPathOnRefill)
                {
                    Start();
                }
                else
                {
                    NavigateNoUpdate();
                }
            });
        }
    }

    /// <summary>
    /// Stop the bot (can be called by AI Agent).
    /// </summary>
    public void Stop()
    {
        IsActive = false;
        Game1.player.controller = null;
        Status = WaterBotStatus.Stopped;
        StatusMessage = "Watering stopped by user";
        Log("WaterBot stopped by request");
        DisplayMessage("Watering stopped", 1);
    }

    private void Exhausted()
    {
        Log("Bot interrupted by lack of stamina");
        IsActive = false;
        IsCompleted = true;
        Game1.player.controller = null;
        Status = WaterBotStatus.Exhausted;
        StatusMessage = "Stopped - low stamina";
        DisplayMessage("Too tired to continue watering!", 3);
    }

    private void NoWater()
    {
        Log("Bot could not find suitable refill tile");
        IsActive = false;
        IsCompleted = true;
        Game1.player.controller = null;
        Status = WaterBotStatus.NoWater;
        StatusMessage = "Unable to find water source";
        DisplayMessage("Can't find water to refill!", 3);
    }

    private void End()
    {
        Log($"Bot finished watering {CropsWatered} crops");
        IsActive = false;
        IsCompleted = true;
        Game1.player.controller = null;
        Status = WaterBotStatus.Completed;
        StatusMessage = $"Finished watering {CropsWatered} crops";
        DisplayMessage("All crops watered!", 1);
    }

    private void DisplayMessage(string message, int type)
    {
        Game1.addHUDMessage(new HUDMessage(message, type));
    }

    private void Log(string message, SmapiLogLevel level = SmapiLogLevel.Debug)
    {
        _monitor.Log($"[WaterBot] {message}", level);
    }

    /// <summary>
    /// Get current status for AI Agent polling.
    /// </summary>
    public WaterBotResult GetResult()
    {
        return new WaterBotResult
        {
            IsActive = this.IsActive,
            IsCompleted = this.IsCompleted,
            Status = this.Status,
            StatusMessage = this.StatusMessage,
            CropsWatered = this.CropsWatered,
            TotalCrops = this.TotalCrops
        };
    }
}

/// <summary>
/// Result object for AI Agent polling.
/// </summary>
public class WaterBotResult
{
    public bool IsActive { get; set; }
    public bool IsCompleted { get; set; }
    public WaterBotStatus Status { get; set; }
    public string StatusMessage { get; set; } = "";
    public int CropsWatered { get; set; }
    public int TotalCrops { get; set; }
}
