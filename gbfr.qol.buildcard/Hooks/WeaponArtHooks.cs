using System.Diagnostics;
using System.Drawing;

using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;

using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace gbfr.qol.buildcard.Hooks;

// Loads the art of the weapon on the card through ui::icon::LoadWeaponParty, the loader of the party menu's weapon art.
// Each frame the game asks each icon loader whether its menu is open (vfunc 4), lets an open one collect the weapons it
// wants (vfunc 5), then loads the wanted art it lacks and releases the art no longer wanted. The hooks report
// LoadWeaponParty open while the Character Details page is, which LoadSkillBoardCategoryStatus's vfunc 4 reports, and
// add the card's weapon to what it collects.
public unsafe class WeaponArtHooks
{
    // loader: wanted count, then [weapon key, alternate art] entries of 8 bytes
    private const int WantedCount = 0x40;
    private const int Wanted = 0x48;
    private const int WantedMax = 6;

    // chara: the weapon, [unknown, weapon key, mirage weapon key, flags]; flag 0x20 selects the weapon's alternate art
    private const int WeaponKey = 0x54;
    private const int MirageKey = 0x58;
    private const int WeaponFlags = 0x5C;
    private const byte AlternateArt = 0x20;
    private const uint NoKey = 0x887AE0B0;

    // the game settings object: mirage weapons shown, alternate weapon art shown
    private const int ShowMirage = 0x1113;
    private const int ShowAlternateArt = 0x1114;

    private readonly IReloadedHooks _hooks;
    private readonly ILogger _logger;
    private delegate* unmanaged<nint, byte> _statusPageOpen;
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

    // LoadWeaponParty's vfunc 5 reads the settings object through a mov rcx, [rip + disp32] at +0x26.
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
        _statusPageOpen = (delegate* unmanaged<nint, byte>)*(nint*)(status + 4 * 8);
        _isOpenHook = _hooks.CreateHook<IsOpen>(IsOpenImpl, *(long*)(party + 4 * 8)).Activate();
        _collectHook = _hooks.CreateHook<Collect>(CollectImpl, (long)collect).Activate();
    }

    // Sets the weapon the card shows: the mirage weapon when one is set and mirages are shown, with the alternate art
    // when the weapon has it selected and alternate art is shown.
    public void Show(nint chara)
    {
        if (_settings == null || *_settings == 0)
            return;
        nint settings = *_settings;
        uint mirage = *(uint*)(chara + MirageKey);
        _key = mirage != NoKey && *(byte*)(settings + ShowMirage) != 0 ? mirage : *(uint*)(chara + WeaponKey);
        _alternate = (*(byte*)(chara + WeaponFlags) & AlternateArt) != 0 && *(byte*)(settings + ShowAlternateArt) != 0;
    }

    // Open while the party menu is, or while the Character Details page is and a card weapon is set.
    private byte IsOpenImpl(nint loader)
    {
        return _isOpenHook!.OriginalFunction(loader) != 0 || CardShown() ? (byte)1 : (byte)0;
    }

    // The party's weapons while the party menu is open, then the card's weapon while the Character Details page is
    // open, unless it is already wanted.
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

    private bool CardShown() => _key != NoKey && _statusPageOpen(0) != 0;
}
