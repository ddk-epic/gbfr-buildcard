using System.Diagnostics;
using System.Drawing;

using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;

using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace gbfr.qol.buildcard.Hooks;

// Adds the card's weapon to the art ui::icon::LoadWeaponParty loads while the Character Details page is open.
public unsafe class WeaponArtHooks
{
    // loader entries: [weapon key, alternate art]
    private const int WantedCount = 0x40;
    private const int Wanted = 0x48;
    private const int WantedMax = 6;

    private const int WeaponKey = 0x54;
    private const int MirageKey = 0x58;
    private const int WeaponFlags = 0x5C;
    private const byte AlternateArt = 0x20;
    private const uint NoKey = 0x887AE0B0;

    private const int ShowMirage = 0x1113;
    private const int ShowAlternateArt = 0x1114;

    // menu manager: open menus
    private const int OpenMenus = 0x40;
    private const int OpenMenuSize = 0x20;
    private const int OpenMenu = 0x18;

    // menu manager: menu requests
    private const int Requests = 0xD8;
    private const int RequestSize = 0x48;
    private const int OpenRequest = 1;

    // menu
    private const int MenuOpenState = 0x110;
    private const int MenuName = 0x148;

    // name hash
    private const uint PauseStatus = 0xF355AE9C;

    private readonly IReloadedHooks _hooks;
    private readonly ILogger _logger;
    private nint* _menus;
    private nint* _settings;
    private uint _key = NoKey;
    private bool _alternate;

    private delegate byte IsOpen(nint loader);
    private delegate void Collect(nint loader);
    private IHook<IsOpen>? _isOpenHook;
    private IHook<Collect>? _collectHook;

    public WeaponArtHooks(IReloadedHooks hooks, ILogger logger)
    {
        _hooks = hooks;
        _logger = logger;
    }

    // Reads the settings object's address from the mov rcx, [rip + disp32] at LoadWeaponParty vfunc 5 + 0x26.
    public void Init()
    {
        nint exeBase = Process.GetCurrentProcess().MainModule!.BaseAddress;
        nint party = PeImage.FindVtable(exeBase, ".?AVLoadWeaponParty@icon@ui@@");
        nint status = PeImage.FindVtable(exeBase, ".?AVLoadSkillBoardCategoryStatus@icon@ui@@");
        if (party == 0 || status == 0)
        {
            _logger.WriteLine("[gbfr.qol.buildcard] Weapon art loaders not found", Color.Red);
            return;
        }
        byte* collect = *(byte**)(party + 5 * 8);
        if (collect[0x26] != 0x48 || collect[0x27] != 0x8B || collect[0x28] != 0x0D)
        {
            _logger.WriteLine("[gbfr.qol.buildcard] Settings object not found", Color.Red);
            return;
        }
        _settings = (nint*)(collect + 0x2D + *(int*)(collect + 0x29));
        _menus = PeImage.RipGlobal(*(byte**)(status + 4 * 8), [0x48, 0x8B, 0x0D]);
        if (_menus == null)
        {
            _logger.WriteLine("[gbfr.qol.buildcard] Menu manager not found", Color.Red);
            return;
        }
        _isOpenHook = _hooks.CreateHook<IsOpen>(IsOpenImpl, *(long*)(party + 4 * 8)).Activate();
        _collectHook = _hooks.CreateHook<Collect>(CollectImpl, (long)collect).Activate();
    }

    // Sets the weapon whose art is loaded, following the mirage and alternate art settings.
    public void Show(nint chara)
    {
        if (_settings == null || *_settings == 0)
            return;
        nint settings = *_settings;
        uint mirage = *(uint*)(chara + MirageKey);
        _key = mirage != NoKey && *(byte*)(settings + ShowMirage) != 0 ? mirage : *(uint*)(chara + WeaponKey);
        _alternate = (*(byte*)(chara + WeaponFlags) & AlternateArt) != 0 && *(byte*)(settings + ShowAlternateArt) != 0;
    }

    // Open while the party menu is open, or while the Character Details page is open and a card weapon is set.
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
        for (int i = 0; i < count; i++)
        {
            nint entry = loader + Wanted + i * 8;
            if (*(uint*)entry == _key && *(byte*)(entry + 4) == (_alternate ? 1 : 0))
                return;
        }
        if (count >= WantedMax)
            return;
        nint added = loader + Wanted + (nint)count * 8;
        *(uint*)added = _key;
        *(byte*)(added + 4) = _alternate ? (byte)1 : (byte)0;
        *(long*)(loader + WantedCount) = count + 1;
    }

    // Whether the Character Details page is open with a card filled.
    public bool CardShown() => _key != NoKey && StatusMenuOpen();

    // Whether PauseStatus is open or requested to open.
    private bool StatusMenuOpen()
    {
        if (_menus == null || *_menus == 0)
            return false;
        nint manager = *_menus;
        nint end = *(nint*)(manager + OpenMenus + 8);
        for (nint entry = *(nint*)(manager + OpenMenus); entry != end; entry += OpenMenuSize)
        {
            nint menu = *(nint*)(entry + OpenMenu);
            if (menu != 0 && *(uint*)(menu + MenuName) == PauseStatus)
                return *(int*)(menu + MenuOpenState) == 1;
        }
        end = *(nint*)(manager + Requests + 8);
        for (nint request = *(nint*)(manager + Requests); request != end; request += RequestSize)
        {
            if (*(int*)request == OpenRequest && *(uint*)(request + 4) == PauseStatus)
                return true;
        }
        return false;
    }
}
