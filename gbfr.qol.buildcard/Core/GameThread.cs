using System.Collections.Concurrent;

using Reloaded.Mod.Interfaces;

namespace gbfr.qol.buildcard.Core;

// Runs posted work and Tick on the game thread once per presented frame.
public class GameThread
{
    private readonly ILogger _logger;
    private readonly ConcurrentQueue<Action> _posted = new();
    private long _presents;
    private long _tickedAt = -1;

    public event Action? Tick;

    public GameThread(ILogger logger)
    {
        _logger = logger;
    }

    // From any thread: runs action on the game thread before the next Tick.
    public void Post(Action action) => _posted.Enqueue(action);

    // On the render thread: counts a present.
    public void OnPresenting() => Interlocked.Increment(ref _presents);

    // On the game thread: ticks once per present, and at every call before the first.
    public void Pump()
    {
        long presents = Interlocked.Read(ref _presents);
        if (presents != 0 && presents == _tickedAt)
            return;
        _tickedAt = presents;
        while (_posted.TryDequeue(out var action))
            Dispatch.Raise(_logger, nameof(Post), action);
        Dispatch.Raise(_logger, nameof(Tick), Tick);
    }
}
