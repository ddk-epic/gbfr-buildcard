using System.Runtime.InteropServices;
using System.Text;

using NenTools.Reloaded.ScanManager.Interfaces;

namespace gbfr.qol.buildcard.Hooks;

// Sets, reads and wraps UI objects' texts and reads the loaded language's texts by id.
// Signature and parameters from Nenkai's gbfr.qol.detailedpercentages (MIT).
public unsafe class GameText
{
    private const int BufferSize = 0x400;
    public const uint EmptyIdHash = 0x887AE0B0;

    // TextComponentSetText's mov rdx, [rip + disp32] loading the text tables
    private const int TablesLoad = 0x26;

    // Text component
    private const int String = 0x40;
    private const int Lines = 0xA0;
    private const int Hash = 0x188;
    private const int SubId = 0x18C;
    private const int WrapWidth = 0x1D0;
    private const int WrapMode = 0x1D4;
    private const int WrapOn = 0x1D8;
    private const int IconSize = 0x1DF;  // percent of the font size
    private const int LineSize = 0x18;
    private const int GlyphSize = 0x50;
    private const int Reflow = 1;  // wrap mode
    private const int MaxLines = 2;
    private const uint EllipsisHash = 0x6895D7BB;  // TXT_HUD_COMMUNICATION_OVER
    private const int EllipsisGlyphs = 16;

    private readonly UiObjects _objects;
    private readonly nint _textVtable;
    private readonly nint _buffer = Marshal.AllocHGlobal(BufferSize);
    private nint* _tables;
    private byte? _iconSize;  // the cell texts' original icon size
    // glyph vector the ellipsis is built into
    private readonly nint* _ellipsis = (nint*)NativeMemory.Alloc(3, (nuint)sizeof(nint));
    // the ellipsis glyphs, 16-byte aligned
    private readonly nint _glyphs = (nint)NativeMemory.AlignedAlloc(EllipsisGlyphs * GlyphSize, 16);

    // TextComponentSetText(text, string, text id hash, unknown)
    private delegate* unmanaged<nint, GameString*, uint, int, void> _setText;
    // TextLookup(text tables, string out, text id hash, sub-id hash)
    private delegate* unmanaged<nint, TextView*, uint, uint, void> _lookup;
    private delegate* unmanaged<nint, void> _joinLines;
    private delegate* unmanaged<nint, float, nint*, byte, int> _fitLine;  // (text, width, line, with ellipsis)
    private delegate* unmanaged<nint, int, int, int, void> _splitLine;  // (text, line index, at, last glyph)
    private delegate* unmanaged<nint, nint, nint*, nint> _buildGlyphs;  // (text, string, glyphs)
    private delegate* unmanaged<nint*, nint, nint, ulong, void> _insertGlyphs;  // (line, at, glyphs, count)

    public GameText(UiObjects objects)
    {
        _objects = objects;
        _textVtable = objects.FindVtable(".?AVText@component@ui@@");
    }

    public void Init(IScanManager scanManager, string signatureGroup)
    {
        scanManager.AddScan("TextComponentSetText", signatureGroup, address =>
        {
            _setText = (delegate* unmanaged<nint, GameString*, uint, int, void>)(nint)address;
            _tables = PeImage.RipGlobal((byte*)address + TablesLoad, [0x48, 0x8B, 0x15]);
        });
        scanManager.AddScan("TextLookup", signatureGroup, address =>
            _lookup = (delegate* unmanaged<nint, TextView*, uint, uint, void>)(nint)address);
        scanManager.AddScan("TextJoinLines", signatureGroup, address =>
            _joinLines = (delegate* unmanaged<nint, void>)(nint)address);
        scanManager.AddScan("TextFitLine", signatureGroup, address =>
            _fitLine = (delegate* unmanaged<nint, float, nint*, byte, int>)(nint)address);
        scanManager.AddScan("TextSplitLine", signatureGroup, address =>
            _splitLine = (delegate* unmanaged<nint, int, int, int, void>)(nint)address);
        scanManager.AddScan("TextBuildGlyphs", signatureGroup, address =>
            _buildGlyphs = (delegate* unmanaged<nint, nint, nint*, nint>)(nint)address);
        scanManager.AddScan("TextInsertGlyphs", signatureGroup, address =>
            _insertGlyphs = (delegate* unmanaged<nint*, nint, nint, ulong, void>)(nint)address);
    }

    // hash: the custom XXHash32 of a text id
    public void Set(nint obj, string value, uint hash = EmptyIdHash)
    {
        if (_objects.FindComponent(obj, _textVtable) is var text and not 0)
            SetString(text, value, hash);
    }

    // The object's text, empty when it has none
    public string Read(nint obj)
    {
        nint text = _objects.FindComponent(obj, _textVtable);
        return text == 0 ? "" : ReadString(text);
    }

    // Wraps what fill sets to width and caps it at two lines
    public void Wrap(nint obj, int width, Action<nint> fill, float? iconScale = null)
    {
        if (_objects.FindComponent(obj, _textVtable) is not (var text and not 0))
            return;
        Limit(text, width);
        if (iconScale is { } scale)
            ScaleIcons(text, scale);
        fill(text);
        Cap(text);
    }

    // Wraps the object's current text
    public void Rewrap(nint obj, int width) =>
        Wrap(obj, width, text => SetString(text, ReadString(text), *(uint*)(text + Hash)));

    public string Find(uint hash, uint subId = EmptyIdHash)
    {
        var view = Lookup(hash, subId);
        return view.Ptr == 0 ? "" : Encoding.UTF8.GetString((byte*)view.Ptr, (int)view.Length);
    }

    private void SetString(nint text, string value, uint hash)
    {
        if (_setText == null)
            return;
        int length = Encoding.UTF8.GetBytes(value, new Span<byte>((void*)_buffer, BufferSize));
        var str = new GameString { Ptr = _buffer, Length = (uint)length };
        _setText(text, &str, hash, -1);
    }

    private static string ReadString(nint text)
    {
        nint str = text + String;
        long size = *(long*)(str + 0x10);
        if (size <= 0 || size > BufferSize)
            return "";
        nint data = *(long*)(str + 0x18) > 15 ? *(nint*)str : str;
        return Encoding.UTF8.GetString((byte*)data, (int)size);
    }

    // The text of a text id in the loaded language, null-terminated; Ptr 0 when unavailable
    private TextView Lookup(uint hash, uint subId = EmptyIdHash)
    {
        var view = new TextView();
        if (_lookup == null || _tables == null || *_tables == 0)
            return view;
        _lookup(*_tables, &view, hash, subId);
        if (view.Ptr == 0 || view.Length < 0 || view.Length > BufferSize || ((byte*)view.Ptr)[view.Length] != 0)
            return new TextView();
        return view;
    }

    // Wraps the text's next strings to width
    private static void Limit(nint text, int width)
    {
        *(int*)(text + WrapWidth) = width;
        *(int*)(text + WrapMode) = Reflow;
        *(byte*)(text + WrapOn) = 1;
    }

    // Scales the icons of the text's next strings from the first scaled text's original icon size
    private void ScaleIcons(nint text, float scale)
    {
        _iconSize ??= *(byte*)(text + IconSize);
        *(byte*)(text + IconSize) = (byte)MathF.Round(_iconSize.Value * scale);
    }

    // Caps the text at two lines, the second ending in the game's ellipsis
    private void Cap(nint text)
    {
        if (_joinLines == null || _fitLine == null || _splitLine == null || _buildGlyphs == null || _insertGlyphs == null)
            return;
        Split(text);
        nint* lines = (nint*)(text + Lines);
        if (!GameMemory.IsVector(text + Lines, LineSize) || (lines[1] - lines[0]) / LineSize <= MaxLines)
            return;
        var ellipsis = Lookup(EllipsisHash, *(uint*)(text + SubId));
        if (ellipsis.Ptr == 0 || ellipsis.Length > EllipsisGlyphs)
            return;

        nint first = lines[0];
        lines[0] = first + (MaxLines - 1) * LineSize;
        _joinLines(text);
        lines[0] = first;

        nint* last = (nint*)(first + (MaxLines - 1) * LineSize);
        if (!GameMemory.IsVector((nint)last, GlyphSize))
            return;
        int cut = _fitLine(text, *(int*)(text + WrapWidth), last, 1);
        if (cut <= 0 || cut >= (last[1] - last[0]) / GlyphSize)
            return;
        last[1] = last[0] + cut * GlyphSize;

        _ellipsis[0] = _ellipsis[1] = _glyphs;
        _ellipsis[2] = _glyphs + EllipsisGlyphs * GlyphSize;
        _buildGlyphs(text, ellipsis.Ptr, _ellipsis);
        _insertGlyphs(last, last[1], _ellipsis[0], (ulong)((_ellipsis[1] - _ellipsis[0]) / GlyphSize));
    }

    // Splits each line wider than the wrap width where it stops fitting
    private void Split(nint text)
    {
        nint* lines = (nint*)(text + Lines);
        for (int i = 0; i < (lines[1] - lines[0]) / LineSize; i++)
        {
            nint* line = (nint*)(lines[0] + i * LineSize);
            int cut = _fitLine(text, *(int*)(text + WrapWidth), line, 0);
            if (cut != 0)
                _splitLine(text, i, cut, (int)((line[1] - line[0]) / GlyphSize) - 1);
        }
    }

    private struct GameString
    {
        public nint Ptr;
        public uint Length;
    }

    private struct TextView
    {
        public nint Ptr;
        public long Length;
    }
}
