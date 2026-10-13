using System.Drawing;
using System.Text.Json;

using Reloaded.Mod.Interfaces;

using gbfr.qol.buildcard.Export;
using gbfr.qol.buildcard.Export.Encoders;
using gbfr.qol.buildcard.Host;
using gbfr.qol.buildcard.Model;

namespace gbfr.qol.buildcard.Export.Sinks;

// Writes the card as a 2880x1440 PNG with its BuildCard JSON in Pictures on a worker thread.
public class CardFile
{
    public const int Width = 2880;
    public const int Height = 1440;
    public const string JsonKeyword = "BuildCard";

    private readonly GameThread _gameThread;
    private readonly ILogger _logger;
    private readonly Action _written;
    private readonly string _folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures, Environment.SpecialFolderOption.DoNotVerify),
        "GBFR Character Build Cards");
    private bool _saving;

    // Raised on the worker thread after the PNG is written.
    public event Action<string, int, int>? Saved;

    public CardFile(GameThread gameThread, ILogger logger)
    {
        _gameThread = gameThread;
        _logger = logger;
        _written = () => _saving = false;
    }

    // On the game thread
    public bool Saving => _saving;

    // On the game thread: resizes the frame to 2880x1440 and writes it on a worker thread, named after the card's chara.
    public void Save(CapturedFrame frame, BuildCard? card)
    {
        _saving = true;
        Task.Run(() => Write(frame, card));
    }

    private void Write(CapturedFrame frame, BuildCard? card)
    {
        try
        {
            byte[] rgb = frame.Rgb;
            if (frame.Width != Width || frame.Height != Height)
                rgb = Resample.Resize(rgb, frame.Width, frame.Height, Width, Height);
            (string, string)[] texts = card == null ? [] : [(JsonKeyword, JsonSerializer.Serialize(card, BuildCardJson.Default.BuildCard))];

            Directory.CreateDirectory(_folder);
            string path = Path.Combine(_folder, $"{FileNamePrefix(card?.CharaName ?? "")}{DateTime.Now:yyyyMMdd_HHmmss}.png");
            try
            {
                Png.Write(path, rgb, Width, Height, texts);
            }
            catch
            {
                File.Delete(path);
                throw;
            }
            _logger.WriteLine($"[gbfr.qol.buildcard] Card saved to {path}");
            Dispatch.Raise(_logger, nameof(Saved), Saved, path, Width, Height);
        }
        catch (Exception e)
        {
            _logger.WriteLine($"[gbfr.qol.buildcard] Card export failed: {e.Message}", Color.Red);
        }
        finally
        {
            _gameThread.Post(_written);
        }
    }

    // Invalid file name characters dropped and _ appended; empty for an empty name
    private static string FileNamePrefix(string charaName)
    {
        string cleaned = string.Concat(charaName.Split(Path.GetInvalidFileNameChars())).Trim();
        return cleaned.Length == 0 ? "" : cleaned + "_";
    }
}
