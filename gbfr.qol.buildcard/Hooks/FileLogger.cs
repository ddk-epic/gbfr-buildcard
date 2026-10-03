using System.Runtime.InteropServices;

using NenTools.Reloaded.ScanManager.Interfaces;
using Reloaded.Hooks.Definitions;

using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace gbfr.qol.buildcard.Hooks;

// Writes every file the game opens or checks to a log file.
// Hooks and signatures ported from Nenkai's gbfr.utility.modtools (MIT).
public unsafe class FileLogger
{
    private readonly IReloadedHooks _hooks;
    private readonly StreamWriter _writer;
    private readonly object _writeLock = new();

    public bool Enabled { get; set; }

    private delegate void OpenFile(FileLoadResult* result, uint a2, StringWrap* fileName);
    private IHook<OpenFile>? _openFileHook;

    private delegate int FileExists(StringWrap* fileName);
    private IHook<FileExists>? _fileExistsHook;

    private delegate void OpenFile2(nint a1, StringWrap* fileName, nint @params, nint a4, nint a5);
    private IHook<OpenFile2>? _openFile2Hook;

    public FileLogger(IReloadedHooks hooks, string logPath)
    {
        _hooks = hooks;
        _writer = new StreamWriter(logPath, append: false) { AutoFlush = true };
    }

    public void Init(IScanManager scanManager, string signatureGroup)
    {
        scanManager.AddScan(nameof(OpenFile), signatureGroup, address =>
            _openFileHook = _hooks.CreateHook<OpenFile>(OpenFileImpl, address).Activate());
        scanManager.AddScan(nameof(FileExists), signatureGroup, address =>
            _fileExistsHook = _hooks.CreateHook<FileExists>(FileExistsImpl, address).Activate());
        scanManager.AddScan(nameof(OpenFile2), signatureGroup, address =>
            _openFile2Hook = _hooks.CreateHook<OpenFile2>(OpenFile2Impl, address).Activate());
    }

    private void OpenFileImpl(FileLoadResult* result, uint a2, StringWrap* fileName)
    {
        _openFileHook!.OriginalFunction(result, a2, fileName);
        if (Enabled)
            Write(result->ChunkFileStorage is null ? "open (not found)" : "open", fileName);
    }

    private int FileExistsImpl(StringWrap* fileName)
    {
        int found = _fileExistsHook!.OriginalFunction(fileName);
        if (Enabled)
            Write(found == 0 ? "exists (not found)" : "exists", fileName);
        return found;
    }

    private void OpenFile2Impl(nint a1, StringWrap* fileName, nint @params, nint a4, nint a5)
    {
        _openFile2Hook!.OriginalFunction(a1, fileName, @params, a4, a5);
        if (Enabled)
            Write("open2", fileName);
    }

    private void Write(string kind, StringWrap* fileName)
    {
        if (fileName is null || fileName->pStr is null)
            return;

        string path = Marshal.PtrToStringAnsi((nint)fileName->pStr)!;
        lock (_writeLock)
            _writer.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {kind}: {path}");
    }
}

public unsafe struct StringWrap
{
    public byte* pStr;
    public nint StringSize;
}

public unsafe struct FileLoadResult
{
    public void* ChunkFileStorage;
    public void* FileBuffer;
    public ulong FileSize;
}
