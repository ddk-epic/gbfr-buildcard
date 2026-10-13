using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;

using gbfr.qol.buildcard.Core;
using gbfr.qol.buildcard.Interop.Game;

using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace gbfr.qol.buildcard.Game;

// Reads Save Card's button and raises ShortcutUpdated at each shortcut update.
public class Input : IFeature
{
    private readonly IReloadedHooks _hooks;
    private readonly GameApi _api;
    private readonly ILogger _logger;
    private bool _pressed;

    // Raised on the game thread for each shortcut update.
    public event Action? ShortcutUpdated;

    private delegate void UpdateShortcutInput(nint shortcut);
    private delegate ulong StatusL3Action(nint callable);
    private IHook<UpdateShortcutInput>? _updateHook;
    private IHook<StatusL3Action>? _pressedHook;

    public Input(IReloadedHooks hooks, GameApi api, ILogger logger)
    {
        _hooks = hooks;
        _api = api;
        _logger = logger;
    }

    public Capability[] Requires => [_api.UpdateShortcutInput, _api.StatusL3Action];

    public void Start()
    {
        _updateHook = _hooks.CreateHook<UpdateShortcutInput>(UpdateImpl, _api.UpdateShortcutInput.Address).Activate();
        _pressedHook = _hooks.CreateHook<StatusL3Action>(PressedImpl, _api.StatusL3Action.Address).Activate();
    }

    // Whether the button went down since the last call
    public bool Pressed()
    {
        bool pressed = _pressed;
        _pressed = false;
        return pressed;
    }

    private void UpdateImpl(nint shortcut)
    {
        Dispatch.Raise(_logger, nameof(ShortcutUpdated), ShortcutUpdated);
        _updateHook!.OriginalFunction(shortcut);
    }

    private ulong PressedImpl(nint callable)
    {
        _pressed = true;
        return 0;
    }
}
