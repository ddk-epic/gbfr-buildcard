using System.Drawing;

using Reloaded.Mod.Interfaces;

namespace gbfr.qol.buildcard.Export;

// Writes the card as a 2880x1440 PNG in Pictures on a worker thread.
public class CardFile
{
    public const int Width = 2880;
    public const int Height = 1440;

    private readonly ILogger _logger;
    private readonly string _folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
        "GBFR Character Build Cards");
    private volatile int _saving;

    // Raised on the worker thread after the PNG is written.
    public event Action<string, int, int>? Saved;

    public CardFile(ILogger logger)
    {
        _logger = logger;
    }

    public bool Saving => _saving != 0;

    // Resizes the image to 2880x1440 and writes it on a worker thread, named after the chara.
    public void Save(byte[] rgb, int width, int height, string charaName)
    {
        _saving = 1;
        Task.Run(() => Write(rgb, width, height, charaName));
    }

    private void Write(byte[] rgb, int width, int height, string charaName)
    {
        try
        {
            if (width != Width || height != Height)
                rgb = Resample.Resize(rgb, width, height, Width, Height);

            Directory.CreateDirectory(_folder);
            string path = Path.Combine(_folder, $"{FileNamePrefix(charaName)}{DateTime.Now:yyyyMMdd_HHmmss}.png");
            Png.Write(path, rgb, Width, Height);
            _logger.WriteLine($"[gbfr.qol.buildcard] Card saved to {path}");
            Saved?.Invoke(path, Width, Height);
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

    // Invalid file name characters dropped and _ appended; empty for an empty name
    private static string FileNamePrefix(string charaName)
    {
        string cleaned = string.Concat(charaName.Split(Path.GetInvalidFileNameChars())).Trim();
        return cleaned.Length == 0 ? "" : cleaned + "_";
    }
}
