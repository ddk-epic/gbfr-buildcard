using NenTools.Reloaded.ScanManager.Interfaces;
using Reloaded.Hooks.Definitions;

using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace gbfr.qol.buildcard.Hooks;

// Hooks the view manager's open and close of a view.
public unsafe class Views
{
    // view
    private const int Open = 0x38;

    private readonly IReloadedHooks _hooks;
    private nint _manager;

    // Raised after a view opens, with its name hash.
    public event Action<uint>? Opened;

    // Raised after a view closes, with its name hash.
    public event Action<uint>? Closed;

    private delegate void OpenViewFn(nint manager, uint view, int layer, byte flag, byte reset);
    private delegate void CloseViewFn(nint manager, uint view, byte remove, int flag);
    private IHook<OpenViewFn>? _openViewHook;
    private IHook<CloseViewFn>? _closeViewHook;
    private delegate* unmanaged<nint, nint*, uint, void> _findView;

    public Views(IReloadedHooks hooks)
    {
        _hooks = hooks;
    }

    public void Init(IScanManager scanManager, string signatureGroup)
    {
        scanManager.AddScan("FindView", signatureGroup, address =>
            _findView = (delegate* unmanaged<nint, nint*, uint, void>)(nint)address);
        scanManager.AddScan("OpenView", signatureGroup, address =>
            _openViewHook = _hooks.CreateHook<OpenViewFn>(OpenViewImpl, address).Activate());
        scanManager.AddScan("CloseView", signatureGroup, address =>
            _closeViewHook = _hooks.CreateHook<CloseViewFn>(CloseViewImpl, address).Activate());
    }

    // Whether the view manager last seen has the view open
    public bool IsOpen(uint view) => IsOpen(_manager, view);

    private bool IsOpen(nint manager, uint view)
    {
        if (manager == 0 || _findView == null)
            return false;
        nint found;
        _findView(manager, &found, view);
        return found != 0 && *(byte*)(found + Open) != 0;
    }

    private void OpenViewImpl(nint manager, uint view, int layer, byte flag, byte reset)
    {
        _manager = manager;
        bool wasOpen = IsOpen(manager, view);
        _openViewHook!.OriginalFunction(manager, view, layer, flag, reset);
        if (!wasOpen && IsOpen(manager, view))
            Opened?.Invoke(view);
    }

    private void CloseViewImpl(nint manager, uint view, byte remove, int flag)
    {
        _manager = manager;
        bool wasOpen = IsOpen(manager, view);
        _closeViewHook!.OriginalFunction(manager, view, remove, flag);
        if (wasOpen && !IsOpen(manager, view))
            Closed?.Invoke(view);
    }
}
