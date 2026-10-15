using System.Drawing;

using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;

using gbfr.qol.buildcard.Core;
using gbfr.qol.buildcard.Interop.Game;
using gbfr.qol.buildcard.Sources;

using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace gbfr.qol.buildcard.Game;

// Hooks the function that fills a CharaInfo from a character.
public unsafe class CharaStatusHooks : IFeature
{
    private readonly IReloadedHooks _hooks;
    private readonly UiTree _tree;
    private readonly TextService _text;
    private readonly GameApi _api;
    private readonly ILogger _logger;
    private bool _loggedBuild;

    public event Action? Filling;

    // Raised after the game fills the card's CharaInfo.
    public event Action<CharaInfo, CharaSnapshot>? CharacterShown;

    public event Action? Filled;

    // chara's weapon inventory id
    private const int WeaponId = 0x50;

    // in the chara's weapon
    private const int WeaponKey = 0x04;

    // an id no inventory weapon has
    private const uint PlaceholderWeaponId = uint.MaxValue;

    // in the chara's weapon
    private const int Transcendence = 0x40;

    // WeaponUpgradeStatus results
    private const int NotOwnedStatus = 2;
    private const int TranscendenceStatus = 6;

    private delegate void FillCharacterStatus(nint charaInfo, nint chara, int index);
    private IHook<FillCharacterStatus>? _fillHook;

    private delegate int WeaponUpgradeStatus(nint manager, uint weaponKey);
    private IHook<WeaponUpgradeStatus>? _statusHook;

    // another player's weapon while the game fills it
    [ThreadStatic]
    private static byte* _remoteWeapon;

    public CharaStatusHooks(IReloadedHooks hooks, UiTree tree, TextService text, GameApi api, ILogger logger)
    {
        _hooks = hooks;
        _tree = tree;
        _text = text;
        _api = api;
        _logger = logger;
    }

    public Capability[] Requires => [_api.FillCharacterStatus];

    public Capability[] Optional => [_api.WeaponUpgradeStatus];

    public void Start()
    {
        _fillHook = _hooks.CreateHook<FillCharacterStatus>(FillImpl, _api.FillCharacterStatus.Address).Activate();
        if (_api.WeaponUpgradeStatus.Found)
            _statusHook = _hooks.CreateHook<WeaponUpgradeStatus>(WeaponUpgradeStatusImpl, _api.WeaponUpgradeStatus.Address).Activate();
    }

    private void FillImpl(nint charaInfo, nint chara, int index)
    {
        var info = new CharaInfo(charaInfo);
        if (!_tree.IsOnCard(info))
        {
            _fillHook!.OriginalFunction(charaInfo, chara, index);
            return;
        }
        Dispatch.Raise(_logger, nameof(Filling), Filling);
        try
        {
            FillWithWeaponId(charaInfo, chara, index);
            if (chara != 0)
                RaiseCharacterShown(info, chara);
        }
        finally
        {
            Dispatch.Raise(_logger, nameof(Filled), Filled);
        }
    }

    // Fills with a placeholder id on another player's weapon
    private void FillWithWeaponId(nint charaInfo, nint chara, int index)
    {
        uint* weaponId = (uint*)(chara + WeaponId);
        if (chara == 0 || !CharaSnapshot.IsRemote(chara) || *weaponId != 0)
        {
            _fillHook!.OriginalFunction(charaInfo, chara, index);
            return;
        }
        *weaponId = PlaceholderWeaponId;
        _remoteWeapon = (byte*)weaponId;
        try
        {
            _fillHook!.OriginalFunction(charaInfo, chara, index);
        }
        finally
        {
            _remoteWeapon = null;
            *weaponId = 0;
        }
    }

    // Gives another player's weapon an upgrade status from its own transcendence
    private int WeaponUpgradeStatusImpl(nint manager, uint weaponKey)
    {
        byte* weapon = _remoteWeapon;
        if (weapon == null || weaponKey != *(uint*)(weapon + WeaponKey))
            return _statusHook!.OriginalFunction(manager, weaponKey);
        return *(int*)(weapon + Transcendence) > 0 ? TranscendenceStatus : NotOwnedStatus;
    }

    private void RaiseCharacterShown(CharaInfo info, nint chara)
    {
        try
        {
            Dispatch.Raise(_logger, nameof(CharacterShown), CharacterShown, info, CharaSnapshot.Decode(chara, LogBuildOnce)
                with { CharaName = info.Name is { } name ? _text.Read(name) : "" });
        }
        catch (Exception e)
        {
            _logger.WriteLine($"[gbfr.qol.buildcard] {e}", Color.Red);
        }
    }

    private void LogBuildOnce(string message)
    {
        if (_loggedBuild)
            return;
        _loggedBuild = true;
        _logger.WriteLine($"[gbfr.qol.buildcard] Chara build: {message}", Color.Yellow);
    }
}
