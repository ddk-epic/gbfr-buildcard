using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

using NenTools.Reloaded.ScanManager.Interfaces;
using Reloaded.Hooks.Definitions;

using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace gbfr.qol.buildcard.Hooks;

// Hooks the game's UI Text setters, and sets text through them. With logging enabled, writes every change to a Text
// component's text to a log file, with the game code that set it.
// Signatures and parameters from Nenkai's gbfr.qol.detailedpercentages (MIT).
public unsafe class TextHooks
{
    private const int MaxCallers = 8;
    private const int ScanSlots = 0x800;
    private const int BufferSize = 0x400;
    public const uint NoHash = 0x887AE0B0;  // hash of an empty text id

    private readonly IReloadedHooks _hooks;
    private readonly StreamWriter _writer;
    private readonly object _writeLock = new();
    private readonly Dictionary<nint, string> _lastText = new();
    private readonly nint _exeBase;
    private readonly nint _codeStart;
    private readonly nint _codeEnd;
    private readonly nint _buffer = Marshal.AllocHGlobal(BufferSize);

    public bool Logging { get; set; }

    private delegate void TextComponentSetText(nint text, GameString* str, uint hash, int unk);
    private IHook<TextComponentSetText>? _setTextHook;

    private delegate void SetComponentFromInt(nint text, long number);
    private IHook<SetComponentFromInt>? _setFromIntHook;

    [DllImport("kernel32.dll")]
    private static extern void GetCurrentThreadStackLimits(out nint lowLimit, out nint highLimit);

    public TextHooks(IReloadedHooks hooks, string logPath)
    {
        _hooks = hooks;
        _writer = new StreamWriter(logPath, append: false) { AutoFlush = true };

        ProcessModule exe = Process.GetCurrentProcess().MainModule!;
        _exeBase = exe.BaseAddress;
        (_codeStart, _codeEnd) = FindSection(_exeBase, ".text") ?? (_exeBase, _exeBase + exe.ModuleMemorySize);
    }

    public void Init(IScanManager scanManager, string signatureGroup)
    {
        scanManager.AddScan(nameof(TextComponentSetText), signatureGroup, address =>
            _setTextHook = _hooks.CreateHook<TextComponentSetText>(SetTextImpl, address).Activate());
        scanManager.AddScan(nameof(SetComponentFromInt), signatureGroup, address =>
            _setFromIntHook = _hooks.CreateHook<SetComponentFromInt>(SetFromIntImpl, address).Activate());
    }

    private void SetTextImpl(nint text, GameString* str, uint hash, int unk)
    {
        _setTextHook!.OriginalFunction(text, str, hash, unk);
        if (Logging && str is not null && str->Ptr != 0)
            Write("text", text, Encoding.UTF8.GetString((byte*)str->Ptr, (int)str->Length), $" hash={hash:X8} unk={unk}");
    }

    // hash: the custom XXHash32 of a text id, whose tags (text_*_tag.msg) draw icons on the value's <d> placeholders
    public void Set(nint text, string value, uint hash = NoHash)
    {
        int length = Encoding.UTF8.GetBytes(value, new Span<byte>((void*)_buffer, BufferSize));
        var str = new GameString { Ptr = _buffer, Length = (uint)length };
        _setTextHook?.OriginalFunction(text, &str, hash, -1);
    }

    private void SetFromIntImpl(nint text, long number)
    {
        _setFromIntHook!.OriginalFunction(text, number);
        if (Logging)
            Write("int ", text, number.ToString(), "");
    }

    private void Write(string kind, nint text, string value, string extra)
    {
        lock (_writeLock)
        {
            try
            {
                if (_lastText.TryGetValue(text, out string? last) && last == value)
                    return;
                _lastText[text] = value;
                _writer.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {kind} {text:X} \"{value.Replace("\n", "\\n")}\"{extra} from{Callers()}");
            }
            catch (Exception e)
            {
                _writer.WriteLine($"{DateTime.Now:HH:mm:ss.fff} log failed: {e}");
            }
        }
    }

    // Scans the raw stack for values in the exe's code: return addresses and stale values left in earlier frames.
    private string Callers()
    {
        GetCurrentThreadStackLimits(out _, out nint high);
        nint here = 0;
        nint* slot = &here;
        nint* end = (nint*)Math.Min((long)high, (long)(slot + ScanSlots));

        var callers = new StringBuilder();
        for (int found = 0; slot < end && found < MaxCallers; slot++)
        {
            if (*slot >= _codeStart && *slot < _codeEnd)
            {
                callers.Append($" exe+{*slot - _exeBase:X}");
                found++;
            }
        }
        return callers.ToString();
    }

    private static (nint, nint)? FindSection(nint image, string name)
    {
        byte* nt = (byte*)image + *(int*)(image + 0x3C);
        int sections = *(ushort*)(nt + 6);
        byte* header = nt + 24 + *(ushort*)(nt + 20);
        for (int i = 0; i < sections; i++, header += 40)
        {
            if (Encoding.ASCII.GetString(header, 8).TrimEnd('\0') == name)
                return (image + *(int*)(header + 12), image + *(int*)(header + 12) + *(int*)(header + 8));
        }
        return null;
    }

    public struct GameString
    {
        public nint Ptr;
        public uint Length;
    }
}
