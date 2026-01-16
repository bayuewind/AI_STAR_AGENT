namespace Tools.WaterBot;

/// <summary>
/// Static method of printing to <see cref="SmapiMonitor"/>.
/// </summary>
public static class WaterBotLogger
{
    /// <summary>
    /// Static reference to <see cref="SmapiMonitor"/>.
    /// </summary>
    private static SmapiMonitor? Monitor;

    /// <summary>
    /// Sets static reference to <see cref="SmapiMonitor"/>.
    /// </summary>
    public static void SetMonitor(SmapiMonitor monitor)
    {
        if (WaterBotLogger.Monitor == null)
        {
            WaterBotLogger.Monitor = monitor;
        }
    }

    /// <summary>
    /// Prints to a given <see cref="SmapiLogLevel"/> of <see cref="SmapiMonitor"/>.
    /// </summary>
    public static void Log(string message, SmapiLogLevel level = SmapiLogLevel.Debug)
    {
        WaterBotLogger.Monitor?.Log($"[WaterBot] {message}", level);
    }

    /// <summary>
    /// Prints to Debug level.
    /// </summary>
    public static void Debug(string message)
    {
        Log(message, SmapiLogLevel.Debug);
    }

    /// <summary>
    /// Prints to Info level.
    /// </summary>
    public static void Info(string message)
    {
        Log(message, SmapiLogLevel.Info);
    }

    /// <summary>
    /// Prints to Trace level.
    /// </summary>
    public static void Trace(string message)
    {
        Log(message, SmapiLogLevel.Trace);
    }

    /// <summary>
    /// Prints to Error level.
    /// </summary>
    public static void Error(string message)
    {
        Log(message, SmapiLogLevel.Error);
    }
}
