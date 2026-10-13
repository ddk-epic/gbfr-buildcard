using System.Runtime.InteropServices;

using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;

using gbfr.qol.buildcard.Host;
using gbfr.qol.buildcard.Interop.Game;

using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace gbfr.qol.buildcard.Game;

// Tracks the Character Details menu and its page from its flow and views.
public unsafe class MenuState : IFeature
{
    // flow and view name hashes
    private const uint PauseStatus = 0xF355AE9C;
    private const uint PauseStatusBg = 0x4BE58B5D;

    private enum State { Closed, Opening, OnOtherPage, OnStatusPage, Closing }

    private readonly IReloadedHooks _hooks;
    private readonly GameApi _api;
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
    private delegate* unmanaged<nint, View**, uint, void> _findView;

    public MenuState(IReloadedHooks hooks, GameApi api, ILogger logger)
    {
        _hooks = hooks;
        _api = api;
        _logger = logger;
    }

    public Capability[] Requires => [_api.StartFlow, _api.EndFlow, _api.FindView, _api.OpenView, _api.CloseView];

    public void Start()
    {
        _findView = (delegate* unmanaged<nint, View**, uint, void>)_api.FindView.Address;
        _startFlowHook = _hooks.CreateHook<StartFlowFn>(StartFlowImpl, _api.StartFlow.Address).Activate();
        _endFlowHook = _hooks.CreateHook<EndFlowFn>(EndFlowImpl, _api.EndFlow.Address).Activate();
        _openViewHook = _hooks.CreateHook<OpenViewFn>(OpenViewImpl, _api.OpenView.Address).Activate();
        _closeViewHook = _hooks.CreateHook<CloseViewFn>(CloseViewImpl, _api.CloseView.Address).Activate();
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
        _openViewHook!.OriginalFunction(manager, view, layer, flag, reset);
        if (view is PauseStatus or PauseStatusBg)
            Update(manager);
    }

    private void CloseViewImpl(nint manager, uint view, byte remove, int flag)
    {
        bool wasOpen = view == PauseStatusBg && IsViewOpen(manager, view);
        _closeViewHook!.OriginalFunction(manager, view, remove, flag);
        if (wasOpen && !IsViewOpen(manager, view))
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
        View* found;
        _findView(manager, &found, view);
        return found != null && found->Open != 0;
    }

    private void Set(State state)
    {
        if (state == _state)
            return;
        _state = state;
        Dispatch.Raise(_logger, nameof(Changed), Changed);
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct View
    {
        [FieldOffset(0x38)] public byte Open;
    }
}
