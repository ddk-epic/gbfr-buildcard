using System.Drawing;

using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;

using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

using gbfr.qol.buildcard.Game;
using gbfr.qol.buildcard.Host;
using gbfr.qol.buildcard.Interop.Game;

namespace gbfr.qol.buildcard.Card;

// Shows the card's panel and lines in chr_status_bg01 on the status page and stock's on the menu's other pages.
public unsafe class StatusBackground : IFeature
{
    private readonly IReloadedHooks _hooks;
    private readonly GameApi _api;
    private readonly ControllerLifetimes _lifetimes;
    private readonly ILogger _logger;
    private readonly UiTree _tree;
    private readonly MenuState _menu;
    private UiComponent? _controller;
    private UiObject[] _stock = [];
    private UiObject[] _card = [];
    private bool _cardShown;

    private delegate long SetUpFn(nint controller);
    private IHook<SetUpFn>? _setUpHook;

    public StatusBackground(IReloadedHooks hooks, GameApi api, ControllerLifetimes lifetimes, ILogger logger, UiTree tree,
        MenuState menu)
    {
        _hooks = hooks;
        _api = api;
        _lifetimes = lifetimes;
        _logger = logger;
        _tree = tree;
        _menu = menu;
        menu.Changed += Update;
        lifetimes.Destroyed += OnDestroyed;
    }

    public Capability[] Requires => [_api.ControllerStatusBgVtable, _api.ControllerStatusBgSetUp, .. _lifetimes.Requires];

    public void Start()
    {
        if (_lifetimes.IsHooked)
            _setUpHook = _hooks.CreateHook<SetUpFn>(SetUpImpl, _api.ControllerStatusBgSetUp.Address).Activate();
    }

    private long SetUpImpl(nint controller)
    {
        long result = _setUpHook!.OriginalFunction(controller);
        var component = new UiComponent(controller);
        if (component.Vtable == _api.ControllerStatusBgVtable.Address)
        {
            _controller = component;
            _stock = _card = [];
            try
            {
                Update();
            }
            catch (Exception e)
            {
                _logger.WriteLine($"[gbfr.qol.buildcard] Showing chr_status_bg01's variant failed: {e}", Color.Red);
            }
        }
        return result;
    }

    private void OnDestroyed(UiComponent controller)
    {
        if (controller != _controller)
            return;
        _controller = null;
        _stock = _card = [];
    }

    // Keeps the variant shown while the menu opens, closes or is closed
    private void Update()
    {
        if (_menu.IsOnStatusPage || _menu.IsOnOtherPage)
            _cardShown = _menu.IsOnStatusPage;
        Apply();
    }

    private void Apply()
    {
        if (!FindVariants())
            return;
        foreach (var obj in _stock)
            _tree.SetActive(obj, !_cardShown);
        foreach (var obj in _card)
            _tree.SetActive(obj, _cardShown);
    }

    // Finds chr_status_bg01's stock and card objects once per controller
    private bool FindVariants()
    {
        if (_card.Length != 0)
            return true;
        if (_controller?.Object is not { } background)
            return false;
        _stock = [.. _tree.FindByName(background, CardLayout.Background.StockPanel, CardLayout.Background.StockBackdrop, CardLayout.Background.StockLines)];
        _card = [.. _tree.FindByName(background, CardLayout.Background.CardPanel, CardLayout.Background.CardLines)];
        if (_stock.Length == 3 && _card.Length == 2)
            return true;
        _logger.WriteLine($"[gbfr.qol.buildcard] chr_status_bg01 has {_stock.Length} stock and {_card.Length} card objects, not 3 and 2", Color.Red);
        _controller = null;
        _stock = _card = [];
        return false;
    }
}
