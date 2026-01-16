namespace Tools.WaterBot;

/// <summary>
/// Configuration for WaterBot behavior.
/// </summary>
public class WaterBotConfig
{
    /// <summary>
    /// Use smaller groupings for watering (more precise but slower).
    /// </summary>
    public bool UseSmallGrouping { get; set; } = false;

    /// <summary>
    /// Refill watering can after finishing all crops.
    /// </summary>
    public bool RefillOnFinish { get; set; } = false;

    /// <summary>
    /// Refill if water level is below this percentage (0-100).
    /// </summary>
    public int RefillIfLower { get; set; } = 95;

    /// <summary>
    /// Recalculate path after refilling water.
    /// </summary>
    public bool RedoPathOnRefill { get; set; } = false;
}
