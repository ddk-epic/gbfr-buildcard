using Reloaded.Hooks.Definitions;

using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

using gbfr.qol.buildcard.Export;
using gbfr.qol.buildcard.Game;
using gbfr.qol.buildcard.Host;
using gbfr.qol.buildcard.Interop.Game;

namespace gbfr.qol.buildcard.Export.Overlays;

// Hides and shows the page arrows' button prompts (status_guide01), found through their controller's setup.
public class StatusGuide : IOverlay, IFeature
{
    // name hashes of loc_button_system01_l and loc_button_system01_r
    private const uint ButtonLeft = 0x6030E86D;
    private const uint ButtonRight = 0xFB687EBB;

    private readonly IReloadedHooks _hooks;
    private readonly GameApi _api;
    private readonly ControllerLifetimes _lifetimes;
    private readonly UiTree _tree;
    private readonly MenuState _menu;
    private UiComponent? _controller;
    private List<UiObject> _buttons = [];

    private delegate void SetUpPageArrowsFn(nint controller);
    private IHook<SetUpPageArrowsFn>? _setUpPageArrowsHook;

    public StatusGuide(IReloadedHooks hooks, GameApi api, ControllerLifetimes lifetimes, UiTree tree,
        MenuState menu)
    {
        _hooks = hooks;
        _api = api;
        _lifetimes = lifetimes;
        _tree = tree;
        _menu = menu;
        lifetimes.Destroyed += OnDestroyed;
    }

    public Capability[] Requires => [_api.ControllerStatusGuideSetUpPageArrows, _api.ControllerStatusBgDestroy];

    public IFeature[] DependsOn => [_lifetimes];

    public void Start() =>
        _setUpPageArrowsHook = _hooks.CreateHook<SetUpPageArrowsFn>(SetUpPageArrowsImpl,
            _api.ControllerStatusGuideSetUpPageArrows.Address).Activate();

    // Shows the prompts only on the status page
    public void Show(bool shown)
    {
        if (shown && !_menu.IsOnStatusPage)
            return;
        foreach (var button in _buttons)
            _tree.SetActive(button, shown);
    }

    private void SetUpPageArrowsImpl(nint controller)
    {
        _setUpPageArrowsHook!.OriginalFunction(controller);
        _controller = new UiComponent(controller);
        _buttons = _controller.Value.Object is { } obj ? _tree.FindByName(obj, ButtonLeft, ButtonRight) : [];
    }

    private void OnDestroyed(UiComponent controller)
    {
        if (controller != _controller)
            return;
        _controller = null;
        _buttons = [];
    }
}
