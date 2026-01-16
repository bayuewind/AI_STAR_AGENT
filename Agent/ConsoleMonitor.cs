using StardewModdingAPI;
using VerboseLogStringHandler = StardewModdingAPI.Framework.Logging.VerboseLogStringHandler;

namespace Agent;

public class ConsoleMonitor : IMonitor
{
    public bool IsVerbose { get; set; } = false;

    public void Log(string msg, LogLevel level = LogLevel.Debug)
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

    public void LogOnce(string message, LogLevel level = LogLevel.Debug)
    {
        Log(message, level);
    }

    public void VerboseLog(string message)
    {
        if (IsVerbose)
            Log(message, LogLevel.Trace);
    }

    public void VerboseLog(ref VerboseLogStringHandler message)
    {
        if (IsVerbose)
            Log(message.ToString(), LogLevel.Trace);
    }
}
