using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;

using gbfr.qol.buildcard.Model;

namespace gbfr.qol.buildcard.Hooks;

// Adds the card's weapon to the art ui::icon::LoadWeaponParty loads while the Character Details menu is open.
public unsafe class WeaponArtHooks
{
    private const int WantedMax = 6;

    private const string LoadWeaponParty = ".?AVLoadWeaponParty@icon@ui@@";
    private const int IsOpenIndex = 4;
    private const int CollectIndex = 5;
    private const string IsOpenStart = "48 8B 05 ?? ?? ?? ?? 48 8B 48 40 48 8B 50 48 48 39 D1";

    private const uint NoKey = 0x887AE0B0;

    private readonly Vtables _vtables;
    private readonly ILogger _logger;
    private readonly CharacterDetailsMenu _menu;
    private Settings** _settings;
    private uint _key = NoKey;
    private bool _alternate;

    private delegate byte IsOpen(nint loader);
    private delegate void Collect(nint loader);
    private IHook<IsOpen>? _isOpenHook;
    private IHook<Collect>? _collectHook;

    public WeaponArtHooks(Vtables vtables, ILogger logger, CharacterDetailsMenu menu)
    {
        _vtables = vtables;
        _logger = logger;
        _menu = menu;
    }

    // Reads the settings object's address from the mov rcx, [rip + disp32] at LoadWeaponParty vfunc 5 + 0x26.
    public void Init()
    {
        byte* collect = (byte*)_vtables.Function(LoadWeaponParty, CollectIndex, null);
        if (collect == null)
            return;
        _settings = (Settings**)PeImage.RipGlobal(collect + 0x26, [0x48, 0x8B, 0x0D]);
        if (_settings == null)
        {
            _logger.WriteLine("[gbfr.qol.buildcard] Settings object not found", Color.Red);
            return;
        }
        _isOpenHook = _vtables.HookFunction<IsOpen>(LoadWeaponParty, IsOpenIndex, IsOpenStart, IsOpenImpl);
        if (_isOpenHook != null)
            _collectHook = _vtables.HookFunction<Collect>(LoadWeaponParty, CollectIndex, null, CollectImpl);
    }

    // Sets the weapon whose art is loaded, following the mirage and alternate art settings.
    public void OnShown(CharaInfo charaInfo, BuildCard card)
    {
        if (_settings == null || *_settings == null)
            return;
        ref var settings = ref **_settings;
        _key = card.Weapon.MirageKey != NoKey && settings.ShowMirage != 0 ? card.Weapon.MirageKey : card.Weapon.Key;
        _alternate = card.Weapon.AlternateArt && settings.ShowAlternateArt != 0;
    }

    // Open while the party menu is open, or while the Character Details menu is open and a card weapon is set.
    private byte IsOpenImpl(nint loader)
    {
        return _isOpenHook!.OriginalFunction(loader) != 0 || CardShown() ? (byte)1 : (byte)0;
    }

    // Collects the party's weapons while the party menu is open, then adds the card's weapon.
    private void CollectImpl(nint loader)
    {
        if (_isOpenHook!.OriginalFunction(loader) != 0)
            _collectHook!.OriginalFunction(loader);
        if (!CardShown())
            return;

        ref var list = ref *(Loader*)loader;
        long count = list.Count;
        if (count is < 0 or >= WantedMax)
            return;
        byte alternate = _alternate ? (byte)1 : (byte)0;
        for (int i = 0; i < count; i++)
        {
            if (list.Wanted[i].Key == _key && list.Wanted[i].Alternate == alternate)
                return;
        }
        ref var added = ref list.Wanted[(int)count];
        added.Key = _key;
        added.Alternate = alternate;
        list.Count = count + 1;
    }

    // Whether the Character Details menu is open with a card filled.
    private bool CardShown() => _key != NoKey && _menu.IsOpen;

    // ui::icon::LoadWeaponParty
    [StructLayout(LayoutKind.Explicit)]
    private struct Loader
    {
        [FieldOffset(0x40)] public long Count;
        [FieldOffset(0x48)] public WantedArray Wanted;
    }

    [StructLayout(LayoutKind.Explicit, Size = 8)]
    private struct WantedEntry
    {
        [FieldOffset(0x0)] public uint Key;
        [FieldOffset(0x4)] public byte Alternate;
    }

    [InlineArray(WantedMax)]
    private struct WantedArray
    {
        private WantedEntry _element;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct Settings
    {
        [FieldOffset(0x1113)] public byte ShowMirage;
        [FieldOffset(0x1114)] public byte ShowAlternateArt;
    }
}
