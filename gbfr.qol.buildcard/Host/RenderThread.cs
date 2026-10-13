using System.Collections.Concurrent;

using Reloaded.Mod.Interfaces;

using gbfr.qol.buildcard.Interop.D3D11;

namespace gbfr.qol.buildcard.Host;

// Runs posted work and Presenting on the render thread before each present.
public class RenderThread
{
    private readonly ILogger _logger;
    private readonly ConcurrentQueue<Action> _posted = new();

    public event Action<SwapChain>? Presenting;

    public RenderThread(ILogger logger)
    {
        _logger = logger;
    }

    // From any thread: runs action on the render thread before the next Presenting.
    public void Post(Action action) => _posted.Enqueue(action);

    // On the render thread: runs the posted actions in order, then raises Presenting.
    public void OnPresenting(SwapChain swapChain)
    {
        while (_posted.TryDequeue(out var action))
            Dispatch.Raise(_logger, nameof(Post), action);
        Dispatch.Raise(_logger, nameof(Presenting), Presenting, swapChain);
    }
}
