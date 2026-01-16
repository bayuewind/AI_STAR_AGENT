namespace Tools.Movement;

public class MovementConfig
{
    public bool EnablePathFinding { get; set; } = true;
    public int PathFindLimit { get; set; } = 500;
    public bool ShowPathIndicator { get; set; } = false;
    public int HoldTickCount { get; set; } = 15;
}
