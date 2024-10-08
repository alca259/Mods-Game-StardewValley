using StardewModdingAPI;

namespace BetterSprinklersPlus.Framework;

public static class Logger
{
    private static IMonitor Monitor;

    public static void init(IMonitor monitor)
    {
        Monitor = monitor;
    }

    public static void Verbose(string message)
    {
        if (Monitor == null) throw new Exception("Logger not yet initialized");
        Monitor.VerboseLog(message);
    }

    public static void Debug(string message)
    {
        if (Monitor == null) throw new Exception("Logger not yet initialized");
        Monitor.Log(message, LogLevel.Debug);
    }

    public static void Info(string message)
    {
        if (Monitor == null) throw new Exception("Logger not yet initialized");
        Monitor.Log(message, LogLevel.Info);
    }

    public static void Warn(string message)
    {
        if (Monitor == null) throw new Exception("Logger not yet initialized");
        Monitor.Log(message, LogLevel.Warn);
    }

    public static void Error(string message)
    {
        if (Monitor == null) throw new Exception("Logger not yet initialized");
        Monitor.Log(message, LogLevel.Error);
    }

}