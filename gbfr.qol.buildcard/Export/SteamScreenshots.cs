using System.Runtime.InteropServices;
using System.Text;

namespace gbfr.qol.buildcard.Export;

// Adds image files to the player's Steam screenshot library through the game's steam_api64.dll.
public static unsafe class SteamScreenshots
{
    // Whether Steam took the file; false when the game runs without Steam.
    public static bool Add(string path, int width, int height)
    {
        if (!NativeLibrary.TryLoad("steam_api64.dll", out nint steamApi)
            || !NativeLibrary.TryGetExport(steamApi, "SteamAPI_SteamScreenshots_v003", out nint getInterface)
            || !NativeLibrary.TryGetExport(steamApi, "SteamAPI_ISteamScreenshots_AddScreenshotToLibrary", out nint add))
            return false;
        nint screenshots = ((delegate* unmanaged<nint>)getInterface)();
        if (screenshots == 0)
            return false;

        byte[] file = Encoding.UTF8.GetBytes(path + "\0");
        fixed (byte* name = file)
            return ((delegate* unmanaged<nint, byte*, byte*, int, int, uint>)add)(screenshots, name, null, width, height) != 0;
    }
}
