using System.Diagnostics;
using System.Drawing;

using NenTools.Reloaded.ScanManager.Interfaces;
using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;

using gbfr.qol.buildcard.Hooks;

using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace gbfr.qol.buildcard.Export;

// Shows the photo mode's saved notice.
public unsafe class SavedNotice
{
    // offsets in the PushInfo signature
    private const int UiManagerLoad = 0x0;
    private const int EmptyCallbackLoad = 0x6E;
    private const int PushCall = 0x85;

    // UI manager: the info queue
    private const int InfoQueue = 0x310;

    private const int InfoKind = 2;
    private const int PhotoSaved = 0x27;

    // the controllers that show infos
    private static readonly string[] Controllers =
        [".?AVControllerInformationSystem@component@ui@@", ".?AVControllerInformationToast@component@ui@@"];

    // the controllers' vtable index of ShowInfo
    private const int ShowInfoIndex = 35;

    // controller: seconds an info stays
    private const int Duration = 0x1D0;

    // milliseconds
    private const int NoticeDuration = 1500;

    // milliseconds
    private const long ShowTimeout = 5000;

    private readonly IReloadedHooks _hooks;
    private readonly ILogger _logger;
    private nint* _uiManager;
    private nint _emptyCallback;
    private delegate* unmanaged<nint, int, nint, byte, void> _pushInfo;
    private long _pushedAt;
    private readonly Dictionary<nint, float> _shortened = [];

    private delegate nint ShowInfoFn(nint controller, int* info);
    private readonly List<IHook<ShowInfoFn>> _showInfoHooks = [];

    public SavedNotice(IReloadedHooks hooks, ILogger logger)
    {
        _hooks = hooks;
        _logger = logger;
    }

    // PushInfo(queue, type, callback, add to log)
    public void Init(IScanManager scanManager, string signatureGroup)
    {
        scanManager.AddScan("PushInfo", signatureGroup, address =>
        {
            byte* code = (byte*)address;
            byte* lea = code + EmptyCallbackLoad;
            byte* call = code + PushCall;
            _uiManager = PeImage.RipGlobal(code + UiManagerLoad, [0x48, 0x8B, 0x05]);
            _emptyCallback = (nint)(lea + 7 + *(int*)(lea + 3));
            _pushInfo = (delegate* unmanaged<nint, int, nint, byte, void>)(call + 5 + *(int*)(call + 1));
        });

        nint image = Process.GetCurrentProcess().MainModule!.BaseAddress;
        foreach (string controller in Controllers)
        {
            nint vtable = PeImage.FindVtable(image, controller);
            if (vtable == 0)
            {
                _logger.WriteLine($"[gbfr.qol.buildcard] {controller} vtable not found", Color.Red);
                continue;
            }
            IHook<ShowInfoFn>? hook = null;
            hook = _hooks.CreateHook<ShowInfoFn>((c, info) => ShowInfoImpl(hook!, c, info),
                *(long*)(vtable + ShowInfoIndex * 8)).Activate();
            _showInfoHooks.Add(hook);
        }
    }

    // Queues the notice on the game thread.
    public void Show()
    {
        if (_uiManager == null || *_uiManager == 0 || _pushInfo == null)
            return;
        nint queue = *(nint*)(*_uiManager + InfoQueue);
        if (queue == 0)
            return;
        nint* callback = stackalloc nint[8];
        new Span<nint>(callback, 8).Clear();
        callback[0] = _emptyCallback;
        _pushedAt = Environment.TickCount64;
        _pushInfo(queue, PhotoSaved, (nint)callback, 1);
    }

    // Shortens the controller's duration for the notice until its next info.
    private nint ShowInfoImpl(IHook<ShowInfoFn> hook, nint controller, int* info)
    {
        if (_shortened.Remove(controller, out float duration))
            *(float*)(controller + Duration) = duration;
        if (info[0] == InfoKind && info[1] == PhotoSaved && _pushedAt != 0
            && Environment.TickCount64 - _pushedAt < ShowTimeout)
        {
            _shortened[controller] = *(float*)(controller + Duration);
            *(float*)(controller + Duration) = NoticeDuration / 1000f;
        }
        return hook.OriginalFunction(controller, info);
    }
}
