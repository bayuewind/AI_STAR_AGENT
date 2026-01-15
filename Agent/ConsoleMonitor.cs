using StardewModdingAPI;

namespace Agent;

public class ConsoleMonitor : IMonitor
{
    public void Log(string msg, LogLevel level)
    {
        var color = level switch
        {
            LogLevel.Trace => ConsoleColor.Gray,
            LogLevel.Debug => ConsoleColor.DarkGray,
            LogLevel.Info => ConsoleColor.White,
            LogLevel.Warn => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            LogLevel.Alert => ConsoleColor.DarkRed,
            _ => ConsoleColor.White
        };

        var prevColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine($"[{level}] {msg}");
        Console.ForegroundColor = prevColor;
    }
}
