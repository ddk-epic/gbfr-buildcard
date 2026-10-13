using System.Drawing;

using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;

namespace gbfr.qol.buildcard.Hooks;

// Shows the card's panel and lines in chr_status_bg01 on the status page and stock's on the menu's other pages.
public unsafe class StatusBackground
{
    private const string ControllerStatusBg = ".?AVControllerStatusBg@component@ui@@";
    // ControllerStatusBg's vtable index of the controllers' shared setup
    private const int SetUpIndex = 8;
    private const string SetUpStart = "41 57 41 56 56 57 53 48 83 EC 30";

    private readonly Vtables _vtables;
    private readonly UiControllers _controllers;
    private readonly ILogger _logger;
    private readonly UiObjects _objects;
    private readonly CharacterDetailsMenu _menu;
    private nint _controllerVtable;
    private UiComponent? _controller;
    private UiObject[] _stock = [];
    private UiObject[] _card = [];
    private bool _cardShown;

    private delegate long SetUpFn(nint controller);
    private IHook<SetUpFn>? _setUpHook;

    public StatusBackground(Vtables vtables, UiControllers controllers, ILogger logger, UiObjects objects,
        CharacterDetailsMenu menu)
    {
        _vtables = vtables;
        _controllers = controllers;
        _logger = logger;
        _objects = objects;
        _menu = menu;
        menu.Changed += Update;
        controllers.Destroyed += OnDestroyed;
    }

    public void Init()
    {
        _controllerVtable = _vtables.Find(ControllerStatusBg);
        if (_controllers.IsHooked)
            _setUpHook = _vtables.HookFunction<SetUpFn>(ControllerStatusBg, SetUpIndex, SetUpStart, SetUpImpl);
    }

    private long SetUpImpl(nint controller)
    {
        long result = _setUpHook!.OriginalFunction(controller);
        var component = new UiComponent(controller);
        if (component.Vtable == _controllerVtable)
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
            _objects.SetActive(obj, !_cardShown);
        foreach (var obj in _card)
            _objects.SetActive(obj, _cardShown);
    }

    // Finds chr_status_bg01's stock and card objects once per controller
    private bool FindVariants()
    {
        if (_card.Length != 0)
            return true;
        if (_controller?.Object is not { } background)
            return false;
        _stock = [.. _objects.FindByName(background, CardLayout.Background.StockPanel, CardLayout.Background.StockBackdrop, CardLayout.Background.StockLines)];
        _card = [.. _objects.FindByName(background, CardLayout.Background.CardPanel, CardLayout.Background.CardLines)];
        if (_stock.Length == 3 && _card.Length == 2)
            return true;
        _logger.WriteLine($"[gbfr.qol.buildcard] chr_status_bg01 has {_stock.Length} stock and {_card.Length} card objects, not 3 and 2", Color.Red);
        _controller = null;
        _stock = _card = [];
        return false;
    }
}
