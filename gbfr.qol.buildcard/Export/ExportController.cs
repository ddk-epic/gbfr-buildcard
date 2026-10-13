using System.Drawing;

using Reloaded.Mod.Interfaces;

using gbfr.qol.buildcard.Export.Sinks;
using gbfr.qol.buildcard.Game;
using gbfr.qol.buildcard.Host;
using gbfr.qol.buildcard.Interop.Game;
using gbfr.qol.buildcard.Model;

namespace gbfr.qol.buildcard.Export;

// Exports the shown card on Save Card: hides the overlays, captures the card's rect and hands it to CardFile.
public class ExportController : IFeature
{
    private const int HiddenFrames = 2;

    // milliseconds
    private const long CaptureTimeout = 1000;

    private readonly Input _input;
    private readonly MenuState _menu;
    private readonly CardSession _session;
    private readonly UiFrameCapture _capture;
    private readonly CardFile _file;
    private readonly GameThread _gameThread;
    private readonly SwapChainHooks _swapChain;
    private readonly ILogger _logger;
    private readonly IOverlay[] _overlays;
    private CaptureRequest? _request;
    private long _requestedAt;

    public ExportController(Input input, MenuState menu, CardSession session, UiFrameCapture capture,
        CardFile file, GameThread gameThread, SwapChainHooks swapChain, ILogger logger, params IOverlay[] overlays)
    {
        _input = input;
        _menu = menu;
        _session = session;
        _capture = capture;
        _file = file;
        _gameThread = gameThread;
        _swapChain = swapChain;
        _logger = logger;
        _overlays = overlays;
    }

    public Capability[] Requires => [];

    public IFeature[] DependsOn => [_input, _menu, _swapChain];

    public void Start() => _gameThread.Tick += OnTick;

    // On the game thread: starts a capture on a press while the card is open, cancels it on its timeout.
    public void OnTick()
    {
        bool pressed = _input.Pressed();
        bool cardOpen = _menu.IsOnStatusPage;
        if (_request != null)
        {
            if (Environment.TickCount64 - _requestedAt > CaptureTimeout && _request.TryCancel())
            {
                _request = null;
                ShowOverlays(true);
                _logger.WriteLine("[gbfr.qol.buildcard] Card export failed: no frame was captured", Color.Red);
            }
        }
        else if (pressed && cardOpen && !_file.Saving)
            Start(_session.Current);
    }

    private void Start(BuildCard? card)
    {
        ShowOverlays(false);
        _request = new CaptureRequest(CardRect, CardFile.Width, CardFile.Height, HiddenFrames,
            frame => _gameThread.Post(() => OnCaptured(frame, card)));
        _requestedAt = Environment.TickCount64;
        _capture.Capture(_request);
    }

    // On the game thread: shows the overlays and saves the frame.
    private void OnCaptured(CapturedFrame? frame, BuildCard? card)
    {
        _request = null;
        ShowOverlays(true);
        if (frame != null)
            _file.Save(frame, card);
    }

    private void ShowOverlays(bool shown)
    {
        foreach (var overlay in _overlays)
            overlay.Show(shown);
    }

    private static RectangleF CardRect(int width, int height)
    {
        float scale = Math.Min((float)width / CardLayout.Canvas.Width, (float)height / CardLayout.Canvas.Height);
        float left = (width - CardLayout.Canvas.Width * scale) / 2 + (CardLayout.Canvas.Width - CardLayout.Card.Width) / 2f * scale;
        float top = (height - CardLayout.Canvas.Height * scale) / 2 + (CardLayout.Canvas.Height - CardLayout.Card.Height) / 2f * scale - CardLayout.Card.Y * scale;
        return new RectangleF(left, top, CardLayout.Card.Width * scale, CardLayout.Card.Height * scale);
    }
}

// A UI element hidden while the card is captured.
public interface IOverlay
{
    // On the game thread
    void Show(bool shown);
}
