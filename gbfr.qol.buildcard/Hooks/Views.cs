using NenTools.Reloaded.ScanManager.Interfaces;
using Reloaded.Hooks.Definitions;

using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace gbfr.qol.buildcard.Hooks;

// Hooks the view manager's open and close of a view.
public class Views
{
    private readonly IReloadedHooks _hooks;

    // Raised after a view opens, with its name hash.
    public event Action<uint>? Opened;

    // Raised before a view closes, with its name hash.
    public event Action<uint>? Closed;

    private delegate void OpenViewFn(nint manager, uint view, int layer, byte flag, byte reset);
    private delegate void CloseViewFn(nint manager, uint view, byte remove, int flag);
    private IHook<OpenViewFn>? _openViewHook;
    private IHook<CloseViewFn>? _closeViewHook;

    public Views(IReloadedHooks hooks)
    {
        _hooks = hooks;
    }

    public void Init(IScanManager scanManager, string signatureGroup)
    {
        scanManager.AddScan("OpenView", signatureGroup, address =>
            _openViewHook = _hooks.CreateHook<OpenViewFn>(OpenViewImpl, address).Activate());
        scanManager.AddScan("CloseView", signatureGroup, address =>
            _closeViewHook = _hooks.CreateHook<CloseViewFn>(CloseViewImpl, address).Activate());
    }

    private void OpenViewImpl(nint manager, uint view, int layer, byte flag, byte reset)
    {
        _openViewHook!.OriginalFunction(manager, view, layer, flag, reset);
        Opened?.Invoke(view);
    }

    private void CloseViewImpl(nint manager, uint view, byte remove, int flag)
    {
        Closed?.Invoke(view);
        _closeViewHook!.OriginalFunction(manager, view, remove, flag);
    }
}
