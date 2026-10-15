using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;

using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

using gbfr.qol.buildcard.Core;
using gbfr.qol.buildcard.Interop.Game;

namespace gbfr.qol.buildcard.Game;

// Raises Destroyed before a UI controller is destroyed.
public class ControllerLifetimes : IFeature
{
    private readonly IReloadedHooks _hooks;
    private readonly GameApi _api;
    private readonly ILogger _logger;
    private IHook<DestroyFn>? _destroyHook;

    // Raised on the game thread.
    public event Action<UiComponent>? Destroyed;

    private delegate void DestroyFn(nint controller);

    public ControllerLifetimes(IReloadedHooks hooks, GameApi api, ILogger logger)
    {
        _hooks = hooks;
        _api = api;
        _logger = logger;
    }

    public Capability[] Requires => [];

    public Capability[] Optional => [_api.ControllerDestroy];

    // Whether controllers raise Destroyed
    public bool IsHooked => _destroyHook != null;

    public void Start()
    {
        if (_api.ControllerDestroy.Found)
            _destroyHook = _hooks.CreateHook<DestroyFn>(DestroyImpl, _api.ControllerDestroy.Address).Activate();
    }

    private void DestroyImpl(nint controller)
    {
        Dispatch.Raise(_logger, nameof(Destroyed), Destroyed, new UiComponent(controller));
        _destroyHook!.OriginalFunction(controller);
    }
}
