using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;

using gbfr.qol.buildcard.Game;
using gbfr.qol.buildcard.Host;
using gbfr.qol.buildcard.Interop.Game;
using gbfr.qol.buildcard.Interop.Native;
using gbfr.qol.buildcard.Model;
using gbfr.qol.buildcard.Sources;

using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace gbfr.qol.buildcard.Card;

// Adds the card's weapon to the art ui::icon::LoadWeaponParty loads while the Character Details menu is open.
public unsafe class WeaponArt : IFeature
{
    private const int WantedMax = 6;

    private readonly IReloadedHooks _hooks;
    private readonly GameApi _api;
    private readonly ILogger _logger;
    private readonly MenuState _menu;
    private Settings** _settings;
    private uint _key = TextTable.EmptyIdHash;
    private bool _alternate;

    private delegate byte IsOpen(nint loader);
    private delegate void Collect(nint loader);
    private IHook<IsOpen>? _isOpenHook;
    private IHook<Collect>? _collectHook;

    public WeaponArt(IReloadedHooks hooks, GameApi api, ILogger logger, MenuState menu)
    {
        _hooks = hooks;
        _api = api;
        _logger = logger;
        _menu = menu;
    }

    public Capability[] Requires => [_api.LoadWeaponPartyIsOpen, _api.LoadWeaponPartyCollect];

    // Reads the settings object's address from the mov rcx, [rip + disp32] at LoadWeaponParty vfunc 5 + 0x26.
    public void Start()
    {
        nint collect = _api.LoadWeaponPartyCollect.Address;
        _settings = (Settings**)PeImage.RipGlobal((byte*)collect + 0x26, [0x48, 0x8B, 0x0D]);
        if (_settings == null)
        {
            _logger.WriteLine("[gbfr.qol.buildcard] Settings object not found", Color.Red);
            return;
        }
        _isOpenHook = _hooks.CreateHook<IsOpen>(IsOpenImpl, _api.LoadWeaponPartyIsOpen.Address).Activate();
        _collectHook = _hooks.CreateHook<Collect>(CollectImpl, collect).Activate();
    }

    // Sets the weapon whose art is loaded, following the mirage and alternate art settings.
    public void OnShown(CharaInfo charaInfo, BuildCard card)
    {
        if (_settings == null || *_settings == null)
            return;
        ref var settings = ref **_settings;
        _key = card.Weapon.MirageKey != TextTable.EmptyIdHash && settings.ShowMirage != 0 ? card.Weapon.MirageKey : card.Weapon.Key;
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
    private bool CardShown() => _key != TextTable.EmptyIdHash && _menu.IsOpen;

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
