using System.Runtime.InteropServices;

using gbfr.qol.buildcard.Host;
using gbfr.qol.buildcard.Interop.Game;
using gbfr.qol.buildcard.Interop.Native;

namespace gbfr.qol.buildcard.Sources;

// Reads the limit_bonus_param rows of Over Mastery lines.
public unsafe class OverMasteryParams : IFeature
{
    // SetOverMasteryLine's mov rdx, [rip + disp32] loading the param tables
    private const int TablesLoad = 0x2C;

    private readonly GameApi _api;
    private ParamTables** _tables;
    private Dictionary<uint, nint>? _rows;

    public OverMasteryParams(GameApi api)
    {
        _api = api;
    }

    public Capability[] Requires => [_api.SetOverMasteryLine];

    public void Start() =>
        _tables = (ParamTables**)PeImage.RipGlobal((byte*)_api.SetOverMasteryLine.Address + TablesLoad, [0x48, 0x8B, 0x15]);

    // The param of a limit_bonus_param key; null when unavailable
    public OverMasteryParam? Find(uint key)
    {
        if (_rows == null && _tables != null && *_tables != null)
            _rows = (*_tables)->LimitBonusParams.ReadByUintKey();
        if (_rows == null || !_rows.TryGetValue(key, out nint row) || row == 0)
            return null;
        return new OverMasteryParam(((ParamRow*)row)->Scale, ((ParamRow*)row)->Percent != 0);
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct ParamTables
    {
        [FieldOffset(0x88)] public GameMap LimitBonusParams;  // limit_bonus_param key to row
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct ParamRow
    {
        [FieldOffset(0x4C)] public int Scale;
        [FieldOffset(0x50)] public byte Percent;
    }
}

// The shown value is the line's value times Scale, truncated.
public readonly record struct OverMasteryParam(int Scale, bool Percent);
