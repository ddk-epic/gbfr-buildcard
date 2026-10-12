using System.Drawing;

using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;

namespace gbfr.qol.buildcard.Hooks;

// Shows the card's panel and lines in chr_status_bg01 on the status page and stock's on the menu's other pages.
public unsafe class StatusBackground
{
    private const string ControllerStatusBg = ".?AVControllerStatusBg@component@ui@@";
    // ControllerStatusBg's vtable indexes of the controllers' shared setup and destructor
    private const int SetUpIndex = 8;
    private const int DestroyIndex = 7;
    private const string SetUpStart = "41 57 41 56 56 57 53 48 83 EC 30";
    private const string DestroyStart = "56 57 48 83 EC 28 ?? ?? ?? ?? ?? E8";

    private readonly Vtables _vtables;
    private readonly ILogger _logger;
    private readonly UiObjects _objects;
    private readonly CharacterDetailsMenu _menu;
    private nint _controllerVtable;
    private nint _controller;
    private nint[] _stock = [];
    private nint[] _card = [];
    private bool _cardShown;

    private delegate long SetUpFn(nint controller);
    private delegate nint DestroyFn(nint controller, int flags);
    private IHook<SetUpFn>? _setUpHook;
    private IHook<DestroyFn>? _destroyHook;

    public StatusBackground(Vtables vtables, ILogger logger, UiObjects objects, CharacterDetailsMenu menu)
    {
        _vtables = vtables;
        _logger = logger;
        _objects = objects;
        _menu = menu;
        menu.Changed += Update;
    }

    public void Init()
    {
        _controllerVtable = _vtables.Find(ControllerStatusBg);
        // both or neither
        if (_vtables.Function(ControllerStatusBg, SetUpIndex, SetUpStart) == 0
            || _vtables.Function(ControllerStatusBg, DestroyIndex, DestroyStart) == 0)
            return;
        _setUpHook = _vtables.HookFunction<SetUpFn>(ControllerStatusBg, SetUpIndex, SetUpStart, SetUpImpl);
        _destroyHook = _vtables.HookFunction<DestroyFn>(ControllerStatusBg, DestroyIndex, DestroyStart, DestroyImpl);
    }

    private long SetUpImpl(nint controller)
    {
        long result = _setUpHook!.OriginalFunction(controller);
        if (*(nint*)controller == _controllerVtable)
        {
            _controller = controller;
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

    private nint DestroyImpl(nint controller, int flags)
    {
        if (controller == _controller)
        {
            _controller = 0;
            _stock = _card = [];
        }
        return _destroyHook!.OriginalFunction(controller, flags);
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
        if (_controller == 0 || !FindVariants())
            return;
        foreach (nint obj in _stock)
            _objects.SetActive(obj, !_cardShown);
        foreach (nint obj in _card)
            _objects.SetActive(obj, _cardShown);
    }

    // Finds chr_status_bg01's stock and card objects once per controller
    private bool FindVariants()
    {
        if (_card.Length != 0)
            return true;
        nint background = _objects.ObjectOf(_controller);
        _stock = [.. _objects.FindByName(background, (uint)CardIds.StockPanel, (uint)CardIds.StockBackdrop, (uint)CardIds.StockLines)];
        _card = [.. _objects.FindByName(background, (uint)CardIds.CardPanel, (uint)CardIds.CardLines)];
        if (_stock.Length == 3 && _card.Length == 2)
            return true;
        _logger.WriteLine($"[gbfr.qol.buildcard] chr_status_bg01 has {_stock.Length} stock and {_card.Length} card objects, not 3 and 2", Color.Red);
        _controller = 0;
        _stock = _card = [];
        return false;
    }
}
