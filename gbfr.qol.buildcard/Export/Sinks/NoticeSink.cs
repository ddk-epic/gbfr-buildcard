using System.Runtime.InteropServices;

using gbfr.qol.buildcard.Host;
using gbfr.qol.buildcard.Interop.Game;
using gbfr.qol.buildcard.Interop.Native;

namespace gbfr.qol.buildcard.Export.Sinks;

// Queues the photo mode's saved notice after a card is saved.
public unsafe class NoticeSink : IFeature
{
    public const int PhotoSaved = 0x27;

    // offsets in the PushInfo signature
    private const int UiManagerLoad = 0x0;
    private const int EmptyCallbackLoad = 0x6E;
    private const int PushCall = 0x85;

    private readonly GameApi _api;
    private readonly GameThread _gameThread;
    private UiManager** _uiManager;
    private nint _emptyCallback;
    private delegate* unmanaged<nint, int, nint, byte, void> _pushInfo;

    public NoticeSink(GameApi api, GameThread gameThread)
    {
        _api = api;
        _gameThread = gameThread;
    }

    // On the game thread: when the notice was last queued, 0 before
    public long PushedAt { get; private set; }

    public Capability[] Requires => [_api.PushInfo];

    // PushInfo(queue, type, callback, add to log)
    public void Start()
    {
        byte* code = (byte*)_api.PushInfo.Address;
        byte* lea = code + EmptyCallbackLoad;
        byte* call = code + PushCall;
        _uiManager = (UiManager**)PeImage.RipGlobal(code + UiManagerLoad, [0x48, 0x8B, 0x05]);
        _emptyCallback = (nint)(lea + 7 + *(int*)(lea + 3));
        _pushInfo = (delegate* unmanaged<nint, int, nint, byte, void>)(call + 5 + *(int*)(call + 1));
    }

    // On the worker thread: queues the notice on the game thread.
    public void OnSaved(string path, int width, int height) => _gameThread.Post(Queue);

    private void Queue()
    {
        if (_uiManager == null || *_uiManager == null || _pushInfo == null)
            return;
        nint queue = (*_uiManager)->InfoQueue;
        if (queue == 0)
            return;
        nint* callback = stackalloc nint[8];
        new Span<nint>(callback, 8).Clear();
        callback[0] = _emptyCallback;
        PushedAt = Environment.TickCount64;
        _pushInfo(queue, PhotoSaved, (nint)callback, 1);
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct UiManager
    {
        [FieldOffset(0x310)] public nint InfoQueue;
    }
}
