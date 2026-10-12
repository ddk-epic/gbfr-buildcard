using System.Drawing;

using NenTools.Reloaded.ScanManager.Interfaces;
using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;

using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace gbfr.qol.buildcard.Hooks;

// Tracks the Character Details menu and its page from its flow and views.
public unsafe class CharacterDetailsMenu
{
    // view
    private const int Open = 0x38;

    // flow and view name hashes
    private const uint PauseStatus = 0xF355AE9C;
    private const uint PauseStatusBg = 0x4BE58B5D;

    private enum State { Closed, Opening, OnOtherPage, OnStatusPage, Closing }

    private readonly IReloadedHooks _hooks;
    private readonly ILogger _logger;
    private State _state;

    // Raised on the game thread after the menu's state changes.
    public event Action? Changed;

    private delegate void StartFlowFn(nint manager, uint flow, nint callback, int param, nint arg, byte flag);
    private delegate void EndFlowFn(nint manager, uint flow, int result);
    private delegate void OpenViewFn(nint manager, uint view, int layer, byte flag, byte reset);
    private delegate void CloseViewFn(nint manager, uint view, byte remove, int flag);
    private IHook<StartFlowFn>? _startFlowHook;
    private IHook<EndFlowFn>? _endFlowHook;
    private IHook<OpenViewFn>? _openViewHook;
    private IHook<CloseViewFn>? _closeViewHook;
    private delegate* unmanaged<nint, nint*, uint, void> _findView;

    public CharacterDetailsMenu(IReloadedHooks hooks, ILogger logger)
    {
        _hooks = hooks;
        _logger = logger;
    }

    public void Init(IScanManager scanManager, string signatureGroup)
    {
        scanManager.AddScan("StartFlow", signatureGroup, address =>
            _startFlowHook = _hooks.CreateHook<StartFlowFn>(StartFlowImpl, address).Activate());
        scanManager.AddScan("EndFlow", signatureGroup, address =>
            _endFlowHook = _hooks.CreateHook<EndFlowFn>(EndFlowImpl, address).Activate());
        scanManager.AddScan("FindView", signatureGroup, address =>
            _findView = (delegate* unmanaged<nint, nint*, uint, void>)(nint)address);
        scanManager.AddScan("OpenView", signatureGroup, address =>
            _openViewHook = _hooks.CreateHook<OpenViewFn>(OpenViewImpl, address).Activate());
        scanManager.AddScan("CloseView", signatureGroup, address =>
            _closeViewHook = _hooks.CreateHook<CloseViewFn>(CloseViewImpl, address).Activate());
    }

    // From the menu's start until its end
    public bool IsOpen => _state != State.Closed;

    public bool IsOnStatusPage => _state == State.OnStatusPage;

    public bool IsOnOtherPage => _state == State.OnOtherPage;

    private void StartFlowImpl(nint manager, uint flow, nint callback, int param, nint arg, byte flag)
    {
        _startFlowHook!.OriginalFunction(manager, flow, callback, param, arg, flag);
        if (flow == PauseStatus && _state == State.Closed)
            Set(State.Opening);
    }

    private void EndFlowImpl(nint manager, uint flow, int result)
    {
        _endFlowHook!.OriginalFunction(manager, flow, result);
        if (flow == PauseStatus)
            Set(State.Closed);
    }

    private void OpenViewImpl(nint manager, uint view, int layer, byte flag, byte reset)
    {
        bool tracked = view is PauseStatus or PauseStatusBg;
        bool wasOpen = tracked && IsViewOpen(manager, view);
        _openViewHook!.OriginalFunction(manager, view, layer, flag, reset);
        if (tracked && !wasOpen && IsViewOpen(manager, view))
            Update(manager);
    }

    private void CloseViewImpl(nint manager, uint view, byte remove, int flag)
    {
        bool tracked = view is PauseStatus or PauseStatusBg;
        bool wasOpen = tracked && IsViewOpen(manager, view);
        _closeViewHook!.OriginalFunction(manager, view, remove, flag);
        if (tracked && wasOpen && !IsViewOpen(manager, view))
            Update(manager);
    }

    // The page while the background view is open, closing once it closes
    private void Update(nint manager)
    {
        if (_state == State.Closed)
            return;
        if (IsViewOpen(manager, PauseStatusBg))
            Set(IsViewOpen(manager, PauseStatus) ? State.OnStatusPage : State.OnOtherPage);
        else if (_state != State.Opening)
            Set(State.Closing);
    }

    private bool IsViewOpen(nint manager, uint view)
    {
        if (manager == 0 || _findView == null)
            return false;
        nint found;
        _findView(manager, &found, view);
        return found != 0 && *(byte*)(found + Open) != 0;
    }

    private void Set(State state)
    {
        if (state == _state)
            return;
        _state = state;
        try
        {
            Changed?.Invoke();
        }
        catch (Exception e)
        {
            _logger.WriteLine($"[gbfr.qol.buildcard] Handling the Character Details menu's {state} failed: {e}", Color.Red);
        }
    }
}
