namespace Core.Models;

public class ScheduleEntry
{
    public string Owner { get; set; } = string.Empty; // NPC Name or "SeedShop"
    public string DayOfWeek { get; set; } = "Any"; // Monday, Tuesday, etc. or Any
    public string StartTime { get; set; } = "00:00";
    public string EndTime { get; set; } = "23:59";
    public string Location { get; set; } = string.Empty;
    public string Activity { get; set; } = string.Empty;
}
