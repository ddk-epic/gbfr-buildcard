using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;

using gbfr.qol.buildcard.Host;
using gbfr.qol.buildcard.Interop.Game;
using gbfr.qol.buildcard.Interop.Native;
using gbfr.qol.buildcard.Sources;

namespace gbfr.qol.buildcard.Game;

// Sets, reads and wraps UI objects' texts.
// Signature and parameters from Nenkai's gbfr.qol.detailedpercentages (MIT).
public unsafe class TextService : IFeature
{
    private const int BufferSize = 0x400;

    private const int Reflow = 1;  // wrap mode
    private const uint EllipsisHash = 0x6895D7BB;  // TXT_HUD_COMMUNICATION_OVER
    private const int EllipsisGlyphs = 16;

    private readonly UiTree _tree;
    private readonly TextTable _table;
    private readonly GameApi _api;
    private readonly nint _buffer = Marshal.AllocHGlobal(BufferSize);
    private byte? _iconSize;  // the cell texts' original icon size
    // glyph vector the ellipsis is built into
    private readonly GameVector<Glyph>* _ellipsis = (GameVector<Glyph>*)NativeMemory.Alloc((nuint)sizeof(GameVector<Glyph>));
    // the ellipsis glyphs, 16-byte aligned
    private readonly Glyph* _glyphs = (Glyph*)NativeMemory.AlignedAlloc(EllipsisGlyphs * (nuint)sizeof(Glyph), 16);

    // TextComponentSetText(text, string, text id hash, unknown)
    private delegate* unmanaged<nint, GameString*, uint, int, void> _setText;
    private delegate* unmanaged<nint, void> _joinLines;
    private delegate* unmanaged<nint, float, GameVector<Glyph>*, byte, int> _fitLine;  // (text, width, line, with ellipsis)
    private delegate* unmanaged<nint, int, int, int, void> _splitLine;  // (text, line index, at, last glyph)
    private delegate* unmanaged<nint, nint, GameVector<Glyph>*, nint> _buildGlyphs;  // (text, string, glyphs)
    private delegate* unmanaged<GameVector<Glyph>*, Glyph*, Glyph*, ulong, void> _insertGlyphs;  // (line, at, glyphs, count)

    public TextService(UiTree tree, TextTable table, GameApi api)
    {
        _tree = tree;
        _table = table;
        _api = api;
    }

    public Capability[] Requires => [_api.TextComponentSetText, _api.TextVtable];

    public Capability[] Optional =>
        [_api.TextJoinLines, _api.TextFitLine, _api.TextSplitLine, _api.TextBuildGlyphs, _api.TextInsertGlyphs];

    public void Start()
    {
        _setText = (delegate* unmanaged<nint, GameString*, uint, int, void>)_api.TextComponentSetText.Address;
        _joinLines = (delegate* unmanaged<nint, void>)_api.TextJoinLines.Address;
        _fitLine = (delegate* unmanaged<nint, float, GameVector<Glyph>*, byte, int>)_api.TextFitLine.Address;
        _splitLine = (delegate* unmanaged<nint, int, int, int, void>)_api.TextSplitLine.Address;
        _buildGlyphs = (delegate* unmanaged<nint, nint, GameVector<Glyph>*, nint>)_api.TextBuildGlyphs.Address;
        _insertGlyphs = (delegate* unmanaged<GameVector<Glyph>*, Glyph*, Glyph*, ulong, void>)_api.TextInsertGlyphs.Address;
    }

    // hash: the custom XXHash32 of a text id
    public void Set(UiObject obj, string value, uint hash = TextTable.EmptyIdHash)
    {
        if (FindText(obj) is { } text)
            SetString(text, value, hash);
    }

    // The object's text, empty when it has none
    public string Read(UiObject obj) => FindText(obj) is { } text ? ReadString(text) : "";

    public string Read(TextComponent text) => ReadString(text);

    // Wraps what fill sets to width and caps it at maxLines lines
    public void Wrap(UiObject obj, int width, Action<TextComponent> fill, float? iconScale = null, int maxLines = 2)
    {
        if (FindText(obj) is not { } text)
            return;
        Limit(text, width);
        if (iconScale is { } scale)
            ScaleIcons(text, scale);
        fill(text);
        Cap(text, maxLines);
    }

    // Wraps the object's current text
    public void Rewrap(UiObject obj, int width, int maxLines = 2, string suffix = "") =>
        Wrap(obj, width, text =>
        {
            string value = ReadString(text) + suffix;
            uint hash = Data(text).Hash;
            SetString(text, "", hash);
            SetString(text, value, hash);
        }, maxLines: maxLines);

    private TextComponent? FindText(UiObject obj) =>
        _tree.FindComponent(obj, _api.TextVtable) is { } component ? new TextComponent(component.Address) : null;

    private static ref TextData Data(TextComponent text) => ref *(TextData*)text.Address;

    private void SetString(TextComponent text, string value, uint hash)
    {
        if (_setText == null)
            return;
        // cut at the buffer's size
        Utf8.FromUtf16(value, new Span<byte>((void*)_buffer, BufferSize), out _, out int length);
        var str = new GameString { Ptr = _buffer, Length = (uint)length };
        _setText(text.Address, &str, hash, -1);
    }

    private static string ReadString(TextComponent text)
    {
        ref var str = ref Data(text).String;
        return str.Size <= 0 || str.Size > BufferSize ? "" : Encoding.UTF8.GetString(str.Data, (int)str.Size);
    }

    // Wraps the text's next strings to width
    private static void Limit(TextComponent text, int width)
    {
        ref var data = ref Data(text);
        data.WrapWidth = width;
        data.WrapMode = Reflow;
        data.WrapOn = 1;
    }

    // Scales the icons of the text's next strings
    private void ScaleIcons(TextComponent text, float scale)
    {
        ref var data = ref Data(text);
        _iconSize ??= data.IconSize;
        data.IconSize = (byte)MathF.Round(_iconSize.Value * scale);
    }

    // Caps the text at maxLines lines, the last ending in the game's ellipsis
    private void Cap(TextComponent text, int maxLines)
    {
        ref var data = ref Data(text);
        if (_joinLines == null || _fitLine == null || _splitLine == null || _buildGlyphs == null || _insertGlyphs == null
            || !data.Lines.IsValid)
            return;
        Split(text);
        if (!data.Lines.IsValid || data.Lines.Count <= maxLines)
            return;
        var ellipsis = _table.Lookup(EllipsisHash, data.SubId);
        if (ellipsis.Ptr == 0 || ellipsis.Length > EllipsisGlyphs)
            return;

        var first = data.Lines.Begin;
        data.Lines.Begin = first + (maxLines - 1);
        _joinLines(text.Address);
        data.Lines.Begin = first;

        var last = first + (maxLines - 1);
        if (!last->IsValid)
            return;
        int cut = _fitLine(text.Address, data.WrapWidth, last, 1);
        if (cut <= 0 || cut >= last->Count)
            return;
        last->End = last->Begin + cut;

        _ellipsis->Begin = _ellipsis->End = _glyphs;
        _ellipsis->Capacity = _glyphs + EllipsisGlyphs;
        _buildGlyphs(text.Address, ellipsis.Ptr, _ellipsis);
        _insertGlyphs(last, last->End, _ellipsis->Begin, (ulong)_ellipsis->Count);
    }

    // Splits each line wider than the wrap width
    private void Split(TextComponent text)
    {
        ref var data = ref Data(text);
        for (int i = 0; i < data.Lines.Count; i++)
        {
            var line = data.Lines.Begin + i;
            if (!line->IsValid)
                return;
            int cut = _fitLine(text.Address, data.WrapWidth, line, 0);
            int glyphs = line->Count;
            if (cut > 0 && cut < glyphs)
                _splitLine(text.Address, i, cut, glyphs - 1);
        }
    }

    // ui::component::Text
    [StructLayout(LayoutKind.Explicit)]
    private struct TextData
    {
        [FieldOffset(0x40)] public StdString String;
        [FieldOffset(0xA0)] public GameVector<GameVector<Glyph>> Lines;
        [FieldOffset(0x188)] public uint Hash;
        [FieldOffset(0x18C)] public uint SubId;
        [FieldOffset(0x1D0)] public int WrapWidth;
        [FieldOffset(0x1D4)] public int WrapMode;
        [FieldOffset(0x1D8)] public byte WrapOn;
        [FieldOffset(0x1DF)] public byte IconSize;  // percent of the font size
    }

    // An MSVC string, inline up to 15 bytes.
    [StructLayout(LayoutKind.Explicit, Size = 0x20)]
    private struct StdString
    {
        [FieldOffset(0x0)] public byte* Pointer;
        [FieldOffset(0x10)] public long Size;
        [FieldOffset(0x18)] public long Capacity;

        public byte* Data => Capacity > 15 ? Pointer : (byte*)Unsafe.AsPointer(ref this);
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x50)]
    private struct Glyph;

    private struct GameString
    {
        public nint Ptr;
        public uint Length;
    }
}
