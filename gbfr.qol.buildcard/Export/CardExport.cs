using System.Drawing;

using Reloaded.Mod.Interfaces;

using gbfr.qol.buildcard.Hooks;

namespace gbfr.qol.buildcard.Export;

// Saves the card as a 2880x1440 PNG on Save Card.
public class CardExport
{
    private const int CanvasWidth = 3840;
    private const int CanvasHeight = 2160;

    private const int TargetWidth = 2880;
    private const int TargetHeight = 1440;

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
    private readonly Func<bool> _cardShown;
    private readonly Func<bool> _addToSteam;
    private readonly ILogger _logger;
    private readonly string _folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
        "GBFR Build Cards");
    private volatile int _state;
    private volatile int _saving;
    private long _presents;
    private long _hiddenAt;
    private long _hiddenSince;
    private long _cardSeenAt;

    public CardExport(SaveCardButton button, StatusGuide guide, CardRedraw redraw, Func<bool> cardShown,
        Func<bool> addToSteam, ILogger logger)
    {
        _button = button;
        _guide = guide;
        _redraw = redraw;
        _cardShown = cardShown;
        _addToSteam = addToSteam;
        _logger = logger;
    }

    // On the game thread: hides the page button prompts on a press, shows them after the capture or its timeout.
    public void OnTick()
    {
        bool pressed = _button.Pressed();
        bool cardShown = _cardShown();
        if (cardShown)
            Interlocked.Exchange(ref _cardSeenAt, Environment.TickCount64);

        if (_state == Hidden && Environment.TickCount64 - _hiddenSince > CaptureTimeout
            && Interlocked.CompareExchange(ref _state, Idle, Hidden) == Hidden)
        {
            _guide.Show(true);
            _logger.WriteLine("[gbfr.qol.buildcard] Card export failed: no frame was captured", Color.Red);
        }
        else if (_state == Captured)
        {
            _guide.Show(true);
            _state = Idle;
        }
        else if (_state == Idle && pressed && _saving == 0 && cardShown)
        {
            _guide.Show(false);
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
            _redraw.Begin(swapChain, CardRectF, TargetWidth, TargetHeight);
        _redraw.SetDrawHooks(_state == Capturing || Environment.TickCount64 - Interlocked.Read(ref _cardSeenAt) < DrawHooksTimeout);
    }

    // Reads the redrawn card, or the backbuffer's, and saves it on a worker thread.
    private void Capture(nint swapChain)
    {
        try
        {
            int width = TargetWidth, height = TargetHeight;
            byte[]? rgb = _redraw.End();
            _logger.WriteLine($"[gbfr.qol.buildcard] Redraw {_redraw.Stats}");
            rgb ??= BackbufferReadback.Read(swapChain, CardRect, out width, out height);
            _saving = 1;
            Task.Run(() => Save(rgb, width, height));
        }
        finally
        {
            _state = Captured;
        }
    }

    private void Save(byte[] rgb, int width, int height)
    {
        try
        {
            if (width != TargetWidth || height != TargetHeight)
                rgb = Resample.Resize(rgb, width, height, TargetWidth, TargetHeight);

            Directory.CreateDirectory(_folder);
            string path = Path.Combine(_folder, $"buildcard_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            Png.Write(path, rgb, TargetWidth, TargetHeight);
            bool steam = _addToSteam() && SteamScreenshots.Add(path, TargetWidth, TargetHeight);
            _logger.WriteLine($"[gbfr.qol.buildcard] Card saved to {path}{(steam ? " and added to Steam" : "")}");
        }
        catch (Exception e)
        {
            _logger.WriteLine($"[gbfr.qol.buildcard] Card export failed: {e.Message}", Color.Red);
        }
        finally
        {
            _saving = 0;
        }
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
