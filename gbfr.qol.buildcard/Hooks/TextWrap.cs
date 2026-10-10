using System.Runtime.InteropServices;

using NenTools.Reloaded.ScanManager.Interfaces;

namespace gbfr.qol.buildcard.Hooks;

// Wraps texts to a width with the Text component's own wrap and ends the second line with the game's ellipsis.
public unsafe class TextWrap
{
    // Text component
    private const int Lines = 0xA0;
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

    private readonly GameText _text;
    private byte? _iconSize;  // the cell texts' original icon size
    // glyph vector the ellipsis is built into
    private readonly nint* _ellipsis = (nint*)NativeMemory.Alloc(3, (nuint)sizeof(nint));
    // the ellipsis glyphs, 16-byte aligned
    private readonly nint _glyphs = (nint)NativeMemory.AlignedAlloc(EllipsisGlyphs * GlyphSize, 16);

    private delegate* unmanaged<nint, void> _joinLines;
    private delegate* unmanaged<nint, float, nint*, byte, int> _fitLine;  // (text, width, line, with ellipsis)
    private delegate* unmanaged<nint, int, int, int, void> _splitLine;  // (text, line index, at, last glyph)
    private delegate* unmanaged<nint, nint, nint*, nint> _buildGlyphs;  // (text, string, glyphs)
    private delegate* unmanaged<nint*, nint, nint, ulong, void> _insertGlyphs;  // (line, at, glyphs, count)

    public TextWrap(GameText text)
    {
        _text = text;
    }

    public void Init(IScanManager scanManager, string signatureGroup)
    {
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

    // Wraps the text's next strings to width
    public void Limit(nint text, int width)
    {
        *(int*)(text + WrapWidth) = width;
        *(int*)(text + WrapMode) = Reflow;
        *(byte*)(text + WrapOn) = 1;
    }

    // Scales the icons of the text's next strings from the first scaled text's original icon size
    public void ScaleIcons(nint text, float scale)
    {
        _iconSize ??= *(byte*)(text + IconSize);
        *(byte*)(text + IconSize) = (byte)MathF.Round(_iconSize.Value * scale);
    }

    // Caps the text at two lines, the second ending in the game's ellipsis
    public void Cap(nint text)
    {
        if (_joinLines == null || _fitLine == null || _splitLine == null || _buildGlyphs == null || _insertGlyphs == null)
            return;
        Split(text);
        nint* lines = (nint*)(text + Lines);
        if (!GameMemory.IsVector(text + Lines, LineSize) || (lines[1] - lines[0]) / LineSize <= MaxLines)
            return;
        var ellipsis = _text.Lookup(EllipsisHash, *(uint*)(text + SubId));
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
}
