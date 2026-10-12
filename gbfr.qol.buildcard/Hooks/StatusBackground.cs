using System.Drawing;

using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;

using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace gbfr.qol.buildcard.Hooks;

// Shows the card's panel and lines in chr_status_bg01 while the status page's view is open and stock's otherwise.
public unsafe class StatusBackground
{
    // ControllerStatusBg's vtable indexes of the controllers' shared setup and destructor
    private const int SetUpIndex = 8;
    private const int DestroyIndex = 7;
    private const string SetUpStart = "41 57 41 56 56 57 53 48 83 EC 30";
    private const string DestroyStart = "56 57 48 83 EC 28 ?? ?? ?? ?? ?? E8";

    // view name hashes
    private const uint PauseStatus = 0xF355AE9C;
    private const uint PauseStatusBg = 0x4BE58B5D;

    private readonly IReloadedHooks _hooks;
    private readonly ILogger _logger;
    private readonly UiObjects _objects;
    private readonly Views _views;
    private nint _controllerVtable;
    private nint _controller;
    private nint[] _stock = [];
    private nint[] _card = [];
    private bool _cardShown;

    private delegate long SetUpFn(nint controller);
    private delegate nint DestroyFn(nint controller, int flags);
    private IHook<SetUpFn>? _setUpHook;
    private IHook<DestroyFn>? _destroyHook;

    public StatusBackground(IReloadedHooks hooks, ILogger logger, UiObjects objects, Views views)
    {
        _hooks = hooks;
        _logger = logger;
        _objects = objects;
        _views = views;
        views.Opened += OnViewChanged;
        views.Closed += OnViewChanged;
    }

    public void Init()
    {
        _controllerVtable = _objects.FindVtable(".?AVControllerStatusBg@component@ui@@");
        if (_controllerVtable == 0)
            return;
        nint setUp = *(nint*)(_controllerVtable + SetUpIndex * 8);
        nint destroy = *(nint*)(_controllerVtable + DestroyIndex * 8);
        if (!PeImage.Matches(setUp, SetUpStart) || !PeImage.Matches(destroy, DestroyStart))
        {
            _logger.WriteLine("[gbfr.qol.buildcard] ControllerStatusBg setup or destructor not found", Color.Red);
            return;
        }
        _setUpHook = _hooks.CreateHook<SetUpFn>(SetUpImpl, setUp).Activate();
        _destroyHook = _hooks.CreateHook<DestroyFn>(DestroyImpl, destroy).Activate();
    }

    private void OnViewChanged(uint view)
    {
        if (view == PauseStatus || view == PauseStatusBg)
            Update();
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

    // Shows the card's while both views are open and stock's while only the background is
    private void Update()
    {
        if (_views.IsOpen(PauseStatusBg))
            _cardShown = _views.IsOpen(PauseStatus);
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
