using System.Drawing;

using Reloaded.Mod.Interfaces;

using gbfr.qol.buildcard.Hooks;

namespace gbfr.qol.buildcard.Export;

// Saves the card as a 2880x1440 PNG on the footer's Save Card, with the page button prompts hidden, from the card's
// rect on the screen.
public class CardExport
{
    // the UI canvas, fitted into the backbuffer
    private const int CanvasWidth = 3840;
    private const int CanvasHeight = 2160;

    // the export's size
    private const int TargetWidth = 2880;
    private const int TargetHeight = 1440;

    // frames presented between hiding the page button prompts and the capture
    private const int HiddenFrames = 2;

    // milliseconds the page button prompts stay hidden when no capture comes
    private const long CaptureTimeout = 1000;

    private const int Idle = 0;
    private const int Hidden = 1;
    private const int Captured = 2;

    private readonly SaveCardButton _button;
    private readonly StatusGuide _guide;
    private readonly Func<bool> _cardShown;
    private readonly ILogger _logger;
    private readonly string _folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
        "GBFR Build Cards");
    private volatile int _state;
    private volatile int _saving;
    private long _presents;
    private long _hiddenAt;
    private long _hiddenSince;

    public CardExport(SaveCardButton button, StatusGuide guide, Func<bool> cardShown, ILogger logger)
    {
        _button = button;
        _guide = guide;
        _cardShown = cardShown;
        _logger = logger;
    }

    // On the game thread: hides the page button prompts on a press, shows them after the capture or its timeout.
    public void OnTick()
    {
        bool pressed = _button.Pressed();
        if (_state == Hidden && Environment.TickCount64 - _hiddenSince > CaptureTimeout)
        {
            _guide.Show(true);
            _state = Idle;
            _logger.WriteLine("[gbfr.qol.buildcard] Card export failed: no frame was captured", Color.Red);
        }
        else if (_state == Captured)
        {
            _guide.Show(true);
            _state = Idle;
        }
        else if (_state == Idle && pressed && _saving == 0 && _cardShown())
        {
            _guide.Show(false);
            _hiddenSince = Environment.TickCount64;
            _hiddenAt = Interlocked.Read(ref _presents);
            _state = Hidden;
        }
    }

    // On the render thread: captures the card from the backbuffer once the frames without the page button prompts are
    // presented.
    public void OnPresenting(nint swapChain)
    {
        long presents = Interlocked.Increment(ref _presents);
        if (_state != Hidden || presents - _hiddenAt <= HiddenFrames)
            return;
        try
        {
            byte[] rgb = BackbufferReadback.Read(swapChain, CardRect, out int width, out int height);
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
            _logger.WriteLine($"[gbfr.qol.buildcard] Card saved to {path}");
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

    // The card's rect in a width x height backbuffer.
    private static RectangleF CardRectF(int width, int height)
    {
        float scale = Math.Min((float)width / CanvasWidth, (float)height / CanvasHeight);
        float left = (width - CanvasWidth * scale) / 2 + (CanvasWidth - CardIds.CardWidth) / 2f * scale;
        float top = (height - CanvasHeight * scale) / 2 + (CanvasHeight - CardIds.CardHeight) / 2f * scale - CardIds.CardY * scale;
        return new RectangleF(left, top, CardIds.CardWidth * scale, CardIds.CardHeight * scale);
    }

    // The card's rect in a width x height backbuffer, in whole pixels.
    private static Rectangle CardRect(int width, int height) => Rectangle.Round(CardRectF(width, height));
}
