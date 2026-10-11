using System.Drawing;

using Reloaded.Mod.Interfaces;

using gbfr.qol.buildcard.Hooks;

namespace gbfr.qol.buildcard.Export;

// Captures the card on Save Card and hands it to CardFile.
public class CardExport
{
    private const int CanvasWidth = 3840;
    private const int CanvasHeight = 2160;

    private const int HiddenFrames = 2;

    // milliseconds
    private const long CaptureTimeout = 1000;

    // milliseconds
    private const long DrawHooksTimeout = 500;

    // game thread: Idle to Hidden, Hidden to Idle on the timeout, Captured to Idle; render thread: the rest
    private const int Idle = 0;
    private const int Hidden = 1;
    private const int Capturing = 2;
    private const int Captured = 3;

    private readonly SaveCardButton _button;
    private readonly StatusGuide _guide;
    private readonly CardRedraw _redraw;
    private readonly SavedNotice _notice;
    private readonly CardFile _file;
    private readonly Func<bool> _cardShown;
    private readonly Func<string> _charaName;
    private readonly ILogger _logger;
    private volatile int _state;
    private long _presents;
    private long _hiddenAt;
    private long _hiddenSince;
    private long _cardSeenAt;
    private string _pressedCharaName = "";

    public CardExport(SaveCardButton button, StatusGuide guide, CardRedraw redraw, SavedNotice notice, CardFile file,
        Func<bool> cardShown, Func<string> charaName, ILogger logger)
    {
        _button = button;
        _guide = guide;
        _redraw = redraw;
        _notice = notice;
        _file = file;
        _cardShown = cardShown;
        _charaName = charaName;
        _logger = logger;
    }

    // On the game thread: hides the prompts and notices on a press, shows them after the capture or its timeout.
    public void OnTick()
    {
        bool pressed = _button.Pressed();
        bool cardShown = _cardShown();
        if (cardShown)
            Interlocked.Exchange(ref _cardSeenAt, Environment.TickCount64);

        if (_state == Hidden && Environment.TickCount64 - _hiddenSince > CaptureTimeout
            && Interlocked.CompareExchange(ref _state, Idle, Hidden) == Hidden)
        {
            ShowOverlays(true);
            _logger.WriteLine("[gbfr.qol.buildcard] Card export failed: no frame was captured", Color.Red);
        }
        else if (_state == Captured)
        {
            ShowOverlays(true);
            _state = Idle;
        }
        else if (_state == Idle && pressed && !_file.Saving && cardShown)
        {
            _pressedCharaName = _charaName();
            ShowOverlays(false);
            _hiddenSince = Environment.TickCount64;
            _hiddenAt = Interlocked.Read(ref _presents);
            _state = Hidden;
        }
    }

    // On the render thread: redraws and captures the card once the frames without the prompts are presented.
    public void OnPresenting(nint swapChain)
    {
        long presents = Interlocked.Increment(ref _presents);
        _redraw.Init(swapChain);
        if (_state == Capturing)
            Capture(swapChain);
        else if (_state == Hidden && presents - _hiddenAt >= HiddenFrames
            && Interlocked.CompareExchange(ref _state, Capturing, Hidden) == Hidden)
            _redraw.Begin(swapChain, CardRectF, CardFile.Width, CardFile.Height);
        _redraw.SetDrawHooks(_state == Capturing || Environment.TickCount64 - Interlocked.Read(ref _cardSeenAt) < DrawHooksTimeout);
    }

    // Reads the redrawn card, or the backbuffer's, and saves it.
    private void Capture(nint swapChain)
    {
        try
        {
            int width = CardFile.Width, height = CardFile.Height;
            byte[]? rgb = _redraw.End();
            _logger.WriteLine($"[gbfr.qol.buildcard] Redraw {_redraw.Stats}");
            rgb ??= BackbufferReadback.Read(swapChain, CardRect, out width, out height);
            _file.Save(rgb, width, height, _pressedCharaName);
        }
        finally
        {
            _state = Captured;
        }
    }

    private void ShowOverlays(bool shown)
    {
        _guide.Show(shown);
        _notice.Show(shown);
    }

    private static RectangleF CardRectF(int width, int height)
    {
        float scale = Math.Min((float)width / CanvasWidth, (float)height / CanvasHeight);
        float left = (width - CanvasWidth * scale) / 2 + (CanvasWidth - CardIds.CardWidth) / 2f * scale;
        float top = (height - CanvasHeight * scale) / 2 + (CanvasHeight - CardIds.CardHeight) / 2f * scale - CardIds.CardY * scale;
        return new RectangleF(left, top, CardIds.CardWidth * scale, CardIds.CardHeight * scale);
    }

    private static Rectangle CardRect(int width, int height) => Rectangle.Round(CardRectF(width, height));
}
