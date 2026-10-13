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
    private readonly List<IHook<DestroyFn>> _destroyHooks = [];
    private readonly List<Capability> _hooked = [];

    // Raised on the game thread.
    public event Action<UiComponent>? Destroyed;

    private delegate nint DestroyFn(nint controller, int flags);

    public ControllerLifetimes(IReloadedHooks hooks, GameApi api, ILogger logger)
    {
        _hooks = hooks;
        _api = api;
        _logger = logger;
    }

    public Capability[] Requires => [];

    // ControllerStatusBg's destructor is also ControllerStatusGuide's and ControllerInformationSystem's
    public Capability[] Optional =>
        [_api.ControllerStatusDestroy, _api.ControllerStatusBgDestroy, _api.ControllerInformationToastDestroy];

    // Whether the destructor's controllers raise Destroyed
    public bool Hooks(Capability destructor) => _hooked.Contains(destructor);

    public void Start()
    {
        foreach (var destructor in Optional)
        {
            if (!destructor.Found)
                continue;
            IHook<DestroyFn>? hook = null;
            hook = _hooks.CreateHook<DestroyFn>((controller, flags) => DestroyImpl(hook!, controller, flags),
                destructor.Address).Activate();
            _destroyHooks.Add(hook);
            _hooked.Add(destructor);
        }
    }

    private nint DestroyImpl(IHook<DestroyFn> hook, nint controller, int flags)
    {
        Dispatch.Raise(_logger, nameof(Destroyed), Destroyed, new UiComponent(controller));
        return hook.OriginalFunction(controller, flags);
    }
}
