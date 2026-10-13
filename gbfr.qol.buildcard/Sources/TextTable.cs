using System.Text;

using gbfr.qol.buildcard.Host;
using gbfr.qol.buildcard.Interop.Game;
using gbfr.qol.buildcard.Interop.Native;

namespace gbfr.qol.buildcard.Sources;

// Reads the loaded language's texts by text id.
// Signature and parameters from Nenkai's gbfr.qol.detailedpercentages (MIT).
public unsafe class TextTable : IFeature
{
    private const int MaxLength = 0x400;
    public const uint EmptyIdHash = 0x887AE0B0;

    // TextComponentSetText's mov rdx, [rip + disp32] loading the text tables
    private const int TablesLoad = 0x26;

    // SetOverMasteryLine's mov rcx, [rip + disp32] loading the language settings
    private const int SettingsLoad = 0x1CF;
    private const int LanguageIndex = 0x50;

    // by the game's language index
    private static readonly string[] Languages = ["Jpn", "Eng", "Fra", "Ita", "Deu", "Esp", "Por", "Chs", "Cht", "Kor"];

    private readonly GameApi _api;
    private nint* _tables;
    private nint* _settings;

    // TextLookup(text tables, string out, text id hash, sub-id hash)
    private delegate* unmanaged<nint, TextView*, uint, uint, void> _lookup;

    public TextTable(GameApi api)
    {
        _api = api;
    }

    public Capability[] Requires => [_api.TextComponentSetText, _api.TextLookup];

    public Capability[] Optional => [_api.SetOverMasteryLine];

    public void Start()
    {
        _tables = PeImage.RipGlobal((byte*)_api.TextComponentSetText.Address + TablesLoad, [0x48, 0x8B, 0x15]);
        _lookup = (delegate* unmanaged<nint, TextView*, uint, uint, void>)_api.TextLookup.Address;
        if (_api.SetOverMasteryLine.Found)
            _settings = PeImage.RipGlobal((byte*)_api.SetOverMasteryLine.Address + SettingsLoad, [0x48, 0x8B, 0x0D]);
    }

    // The loaded language as LanguageSetter names it; empty when unavailable
    public string Language => _settings == null || *_settings == 0
        ? ""
        : Languages[Math.Clamp((int)*(sbyte*)(*_settings + LanguageIndex), 0, Languages.Length - 1)];

    // hash: the custom XXHash32 of a text id; empty when unavailable
    public string Find(uint hash, uint subId = EmptyIdHash)
    {
        var view = Lookup(hash, subId);
        return view.Ptr == 0 ? "" : Encoding.UTF8.GetString((byte*)view.Ptr, (int)view.Length);
    }

    // The text of a text id in the loaded language, null-terminated; Ptr 0 when unavailable
    public TextView Lookup(uint hash, uint subId = EmptyIdHash)
    {
        var view = new TextView();
        if (_lookup == null || _tables == null || *_tables == 0)
            return view;
        _lookup(*_tables, &view, hash, subId);
        if (view.Ptr == 0 || view.Length < 0 || view.Length > MaxLength || ((byte*)view.Ptr)[view.Length] != 0)
            return new TextView();
        return view;
    }

    public struct TextView
    {
        public nint Ptr;
        public long Length;
    }
}
