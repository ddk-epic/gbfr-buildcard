using System.Runtime.InteropServices;

using Reloaded.Hooks.Definitions;

using gbfr.qol.buildcard.Export;
using gbfr.qol.buildcard.Export.Outputs;
using gbfr.qol.buildcard.Game;
using gbfr.qol.buildcard.Core;
using gbfr.qol.buildcard.Interop.Game;

using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace gbfr.qol.buildcard.Export.Overlays;

// Shortens the saved notice and hides every shown info while the card is captured.
public unsafe class NoticeTiming : IOverlay, IFeature
{
    private const int InfoKind = 2;

    // milliseconds
    private const int NoticeDuration = 1500;

    // milliseconds
    private const long ShowTimeout = 5000;

    private readonly IReloadedHooks _hooks;
    private readonly GameApi _api;
    private readonly ControllerLifetimes _lifetimes;
    private readonly UiTree _tree;
    private readonly Notice _notice;
    private readonly Dictionary<UiComponent, float> _shortened = [];
    private readonly HashSet<UiComponent> _controllers = [];
    private readonly HashSet<UiComponent> _hidden = [];
    private bool _hiding;

    private delegate nint ShowInfoFn(nint controller, int* info);
    private readonly List<IHook<ShowInfoFn>> _showInfoHooks = [];

    public NoticeTiming(IReloadedHooks hooks, GameApi api, ControllerLifetimes lifetimes, UiTree tree, Notice notice)
    {
        _hooks = hooks;
        _api = api;
        _lifetimes = lifetimes;
        _tree = tree;
        _notice = notice;
        lifetimes.Destroyed += OnDestroyed;
    }

    public Capability[] Requires => [];

    public IFeature[] DependsOn => [_lifetimes];

    public Capability[] Optional => [.. ShowInfos, _api.ControllerDestroy];

    private Capability[] ShowInfos => [_api.ControllerInformationSystemShowInfo, _api.ControllerInformationToastShowInfo];

    public void Start()
    {
        if (!_lifetimes.IsHooked)
            return;
        foreach (var showInfo in ShowInfos)
        {
            if (!showInfo.Found)
                continue;
            IHook<ShowInfoFn>? hook = null;
            hook = _hooks.CreateHook<ShowInfoFn>((c, info) => ShowInfoImpl(hook!, c, info), showInfo.Address).Activate();
            _showInfoHooks.Add(hook);
        }
    }

    // On the game thread: hides the shown notices or shows the running ones again.
    public void Show(bool shown)
    {
        _hiding = !shown;
        if (shown)
        {
            foreach (var controller in _hidden)
            {
                if (Data(controller).Showing != 0 && controller.Object is { } obj)
                    _tree.SetActive(obj, true);
            }
            _hidden.Clear();
            return;
        }
        foreach (var controller in _controllers)
            Hide(controller);
    }

    private void Hide(UiComponent controller)
    {
        if (controller.Object is not { } obj || Data(controller).Showing == 0)
            return;
        _tree.SetActive(obj, false);
        _hidden.Add(controller);
    }

    private void OnDestroyed(UiComponent controller)
    {
        _shortened.Remove(controller);
        _controllers.Remove(controller);
        _hidden.Remove(controller);
    }

    // Shortens the controller's duration for the notice until its next info.
    private nint ShowInfoImpl(IHook<ShowInfoFn> hook, nint address, int* info)
    {
        var controller = new UiComponent(address);
        ref var data = ref Data(controller);
        if (_shortened.Remove(controller, out float duration))
            data.Duration = duration;
        _controllers.Add(controller);
        bool notice = info[0] == InfoKind && info[1] == Notice.PhotoSaved && _notice.PushedAt != 0
            && Environment.TickCount64 - _notice.PushedAt < ShowTimeout;
        if (notice)
        {
            _shortened[controller] = data.Duration;
            data.Duration = NoticeDuration / 1000f;
        }
        nint result = hook.OriginalFunction(address, info);
        if (_hiding)
            Hide(controller);
        return result;
    }

    private static ref InfoController Data(UiComponent controller) => ref *(InfoController*)controller.Address;

    // ControllerInformationSystem and ControllerInformationToast
    [StructLayout(LayoutKind.Explicit)]
    private struct InfoController
    {
        [FieldOffset(0x1A8)] public byte Showing;
        [FieldOffset(0x1D0)] public float Duration;  // seconds
    }
}
