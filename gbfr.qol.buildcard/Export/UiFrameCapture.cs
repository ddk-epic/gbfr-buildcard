using System.Drawing;

using Reloaded.Mod.Interfaces;

using gbfr.qol.buildcard.Host;
using gbfr.qol.buildcard.Interop.D3D11;

namespace gbfr.qol.buildcard.Export;

// Captures a rect of one UI frame at a given size: redrawn into off-screen frame buffers, or read from the backbuffer.
public class UiFrameCapture
{
    // milliseconds
    private const long DrawHooksTimeout = 500;

    private readonly UiRedraw _redraw;
    private readonly RenderThread _renderThread;
    private readonly ILogger _logger;
    private readonly Action _keepDrawHooks;
    private CaptureRequest? _waiting;
    private CaptureRequest? _capturing;
    private int _waited;
    private long _drawHooksAt;

    public UiFrameCapture(UiRedraw redraw, RenderThread renderThread, ILogger logger)
    {
        _redraw = redraw;
        _renderThread = renderThread;
        _logger = logger;
        _keepDrawHooks = () => _drawHooksAt = Environment.TickCount64;
    }

    // From any thread: keeps the draw hooks on for the next 500 milliseconds.
    public void KeepDrawHooks() => _renderThread.Post(_keepDrawHooks);

    // From any thread: redraws the frame after the request's presents.
    public void Capture(CaptureRequest request) => _renderThread.Post(() =>
    {
        _waiting = request;
        _waited = 0;
    });

    // On the render thread: starts the waiting request's redraw, or ends the running one.
    public void OnPresenting(SwapChain swapChain)
    {
        _redraw.Init(swapChain);
        if (_capturing != null)
            End(swapChain);
        else if (_waiting != null && ++_waited >= _waiting.Presents)
        {
            if (_waiting.TryStart())
            {
                _capturing = _waiting;
                _redraw.Begin(swapChain, _capturing.Rect, _capturing.Width, _capturing.Height);
            }
            _waiting = null;
        }
        _redraw.SetDrawHooks(_capturing != null || Environment.TickCount64 - _drawHooksAt < DrawHooksTimeout);
    }

    // Reads the redrawn frame, or the backbuffer's rect, and hands it to the request.
    private void End(SwapChain swapChain)
    {
        var request = _capturing!;
        _capturing = null;
        CapturedFrame? frame = null;
        try
        {
            byte[]? rgb = _redraw.End();
            _logger.WriteLine($"[gbfr.qol.buildcard] Redraw {_redraw.Stats}");
            if (rgb != null)
                frame = new CapturedFrame(rgb, request.Width, request.Height);
            else
            {
                rgb = BackbufferReadback.Read(swapChain, (w, h) => Rectangle.Round(request.Rect(w, h)), out int width, out int height);
                frame = new CapturedFrame(rgb, width, height);
            }
        }
        finally
        {
            request.Done(frame);
        }
    }
}

// A capture of one UI frame, started by UiFrameCapture or cancelled by its owner, whichever comes first.
public sealed class CaptureRequest
{
    private const int Waiting = 0;
    private const int Started = 1;
    private const int Cancelled = 2;

    private int _state;

    // done runs on the render thread, with null on a failure
    public CaptureRequest(Func<int, int, RectangleF> rect, int width, int height, int presents, Action<CapturedFrame?> done)
    {
        Rect = rect;
        Width = width;
        Height = height;
        Presents = presents;
        Done = done;
    }

    public Func<int, int, RectangleF> Rect { get; }
    public int Width { get; }
    public int Height { get; }

    // presents to wait before the redrawn frame
    public int Presents { get; }

    public Action<CapturedFrame?> Done { get; }

    public bool TryStart() => Interlocked.CompareExchange(ref _state, Started, Waiting) == Waiting;

    public bool TryCancel() => Interlocked.CompareExchange(ref _state, Cancelled, Waiting) == Waiting;
}

// A captured frame as rows of 8-bit RGB.
public sealed record CapturedFrame(byte[] Rgb, int Width, int Height);
