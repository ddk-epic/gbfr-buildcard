using System.Collections.Concurrent;

using Reloaded.Mod.Interfaces;

using gbfr.qol.buildcard.Interop.D3D11;

namespace gbfr.qol.buildcard.Core;

// Runs posted work and Presenting on the render thread before each present.
public class RenderThread
{
    private readonly ILogger _logger;
    private readonly SwapChainHooks _swapChain;
    private readonly ConcurrentQueue<Action> _posted = new();

    public event Action<SwapChain>? Presenting;

    public RenderThread(ILogger logger, SwapChainHooks swapChain)
    {
        _logger = logger;
        _swapChain = swapChain;
    }

    // From any thread: runs action before the next Presenting once SwapChainHooks has started.
    public void Post(Action action)
    {
        if (_swapChain.IsHooked)
            _posted.Enqueue(action);
    }

    // On the render thread: runs the posted actions in order, then raises Presenting.
    public void OnPresenting(SwapChain swapChain)
    {
        while (_posted.TryDequeue(out var action))
            Dispatch.Raise(_logger, nameof(Post), action);
        Dispatch.Raise(_logger, nameof(Presenting), Presenting, swapChain);
    }
}
