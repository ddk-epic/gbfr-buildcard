using System.Diagnostics;
using System.Drawing;

using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;

using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace gbfr.qol.buildcard.Hooks;

// Adds the card's weapon to the art ui::icon::LoadWeaponParty loads while the Character Details menu is open.
public unsafe class WeaponArtHooks
{
    // loader entries: [weapon key, alternate art]
    private const int WantedCount = 0x40;
    private const int Wanted = 0x48;
    private const int WantedMax = 6;

    // LoadWeaponParty's IsOpen first bytes
    private const string IsOpenStart = "48 8B 05 ?? ?? ?? ?? 48 8B 48 40 48 8B 50 48 48 39 D1";

    private const int WeaponKey = 0x54;
    private const int MirageKey = 0x58;
    private const int WeaponFlags = 0x5C;
    private const byte AlternateArt = 0x20;
    private const uint NoKey = 0x887AE0B0;

    private const int ShowMirage = 0x1113;
    private const int ShowAlternateArt = 0x1114;

    private readonly IReloadedHooks _hooks;
    private readonly ILogger _logger;
    private readonly CharacterDetailsMenu _menu;
    private nint* _settings;
    private uint _key = NoKey;
    private bool _alternate;

    private delegate byte IsOpen(nint loader);
    private delegate void Collect(nint loader);
    private IHook<IsOpen>? _isOpenHook;
    private IHook<Collect>? _collectHook;

    public WeaponArtHooks(IReloadedHooks hooks, ILogger logger, CharacterDetailsMenu menu)
    {
        _hooks = hooks;
        _logger = logger;
        _menu = menu;
    }

    // Reads the settings object's address from the mov rcx, [rip + disp32] at LoadWeaponParty vfunc 5 + 0x26.
    public void Init()
    {
        nint party = PeImage.FindVtable(Process.GetCurrentProcess().MainModule!.BaseAddress, ".?AVLoadWeaponParty@icon@ui@@");
        if (party == 0)
        {
            _logger.WriteLine("[gbfr.qol.buildcard] Weapon art loader not found", Color.Red);
            return;
        }
        byte* collect = *(byte**)(party + 5 * 8);
        _settings = PeImage.RipGlobal(collect + 0x26, [0x48, 0x8B, 0x0D]);
        if (_settings == null)
        {
            _logger.WriteLine("[gbfr.qol.buildcard] Settings object not found", Color.Red);
            return;
        }
        nint isOpen = *(nint*)(party + 4 * 8);
        if (!PeImage.Matches(isOpen, IsOpenStart))
        {
            _logger.WriteLine("[gbfr.qol.buildcard] Weapon art loader check not found", Color.Red);
            return;
        }
        _isOpenHook = _hooks.CreateHook<IsOpen>(IsOpenImpl, isOpen).Activate();
        _collectHook = _hooks.CreateHook<Collect>(CollectImpl, (long)collect).Activate();
    }

    // Sets the weapon whose art is loaded, following the mirage and alternate art settings.
    public void OnCharacterShown(nint charaInfo, nint chara)
    {
        if (_settings == null || *_settings == 0)
            return;
        nint settings = *_settings;
        uint mirage = *(uint*)(chara + MirageKey);
        _key = mirage != NoKey && *(byte*)(settings + ShowMirage) != 0 ? mirage : *(uint*)(chara + WeaponKey);
        _alternate = (*(byte*)(chara + WeaponFlags) & AlternateArt) != 0 && *(byte*)(settings + ShowAlternateArt) != 0;
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

        long count = *(long*)(loader + WantedCount);
        if (count is < 0 or >= WantedMax)
            return;
        for (int i = 0; i < count; i++)
        {
            nint entry = loader + Wanted + i * 8;
            if (*(uint*)entry == _key && *(byte*)(entry + 4) == (_alternate ? 1 : 0))
                return;
        }
        nint added = loader + Wanted + (nint)count * 8;
        *(uint*)added = _key;
        *(byte*)(added + 4) = _alternate ? (byte)1 : (byte)0;
        *(long*)(loader + WantedCount) = count + 1;
    }

    // Whether the Character Details menu is open with a card filled.
    private bool CardShown() => _key != NoKey && _menu.IsOpen;
}
