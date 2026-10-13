using System.Drawing;

using NenTools.Reloaded.ScanManager.Interfaces;
using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;

using gbfr.qol.buildcard.Host;

using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace gbfr.qol.buildcard.Hooks;

// Hooks the function that fills a CharaInfo from a character.
public unsafe class CharaStatusHooks
{
    private readonly IReloadedHooks _hooks;
    private readonly UiObjects _objects;
    private readonly ILogger _logger;
    private bool _loggedBuild;

    // Raised after the game fills the card's CharaInfo.
    public event Action<CharaInfo, CharaBuild>? CharacterShown;

    private delegate void FillCharacterStatus(nint charaInfo, nint chara, int index);
    private IHook<FillCharacterStatus>? _fillHook;

    public CharaStatusHooks(IReloadedHooks hooks, UiObjects objects, ILogger logger)
    {
        _hooks = hooks;
        _objects = objects;
        _logger = logger;
    }

    public void Init(IScanManager scanManager, string signatureGroup)
    {
        scanManager.AddScan(nameof(FillCharacterStatus), signatureGroup, address =>
            _fillHook = _hooks.CreateHook<FillCharacterStatus>(FillImpl, address).Activate());
    }

    private void FillImpl(nint charaInfo, nint chara, int index)
    {
        _fillHook!.OriginalFunction(charaInfo, chara, index);
        var info = new CharaInfo(charaInfo);
        if (chara != 0 && _objects.IsOnCard(info))
            Dispatch.Raise(_logger, nameof(CharacterShown), CharacterShown, info, CharaBuild.Decode(chara, LogBuildOnce));
    }

    private void LogBuildOnce(string message)
    {
        if (_loggedBuild)
            return;
        _loggedBuild = true;
        _logger.WriteLine($"[gbfr.qol.buildcard] Chara build: {message}", Color.Yellow);
    }
}
