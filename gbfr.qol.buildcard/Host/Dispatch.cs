using System.Drawing;

using Reloaded.Mod.Interfaces;

namespace gbfr.qol.buildcard.Host;

// Raises an event's handlers one by one, logging each that throws.
public static class Dispatch
{
    public static void Raise(ILogger logger, string name, Action? handlers)
    {
        foreach (var handler in Delegate.EnumerateInvocationList(handlers))
        {
            try
            {
                handler();
            }
            catch (Exception e)
            {
                LogFailure(logger, name, handler, e);
            }
        }
    }

    public static void Raise<T>(ILogger logger, string name, Action<T>? handlers, T arg)
    {
        foreach (var handler in Delegate.EnumerateInvocationList(handlers))
        {
            try
            {
                handler(arg);
            }
            catch (Exception e)
            {
                LogFailure(logger, name, handler, e);
            }
        }
    }

    public static void Raise<T1, T2>(ILogger logger, string name, Action<T1, T2>? handlers, T1 arg1, T2 arg2)
    {
        foreach (var handler in Delegate.EnumerateInvocationList(handlers))
        {
            try
            {
                handler(arg1, arg2);
            }
            catch (Exception e)
            {
                LogFailure(logger, name, handler, e);
            }
        }
    }

    public static void Raise<T1, T2, T3>(ILogger logger, string name, Action<T1, T2, T3>? handlers, T1 arg1, T2 arg2,
        T3 arg3)
    {
        foreach (var handler in Delegate.EnumerateInvocationList(handlers))
        {
            try
            {
                handler(arg1, arg2, arg3);
            }
            catch (Exception e)
            {
                LogFailure(logger, name, handler, e);
            }
        }
    }

    private static void LogFailure(ILogger logger, string name, Delegate handler, Exception e) =>
        logger.WriteLine($"[gbfr.qol.buildcard] {name} handler {handler.Method.DeclaringType?.Name}.{handler.Method.Name} failed: {e}",
            Color.Red);
}
