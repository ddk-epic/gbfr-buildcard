using System.Drawing;

using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;

using gbfr.qol.buildcard.Host;
using gbfr.qol.buildcard.Interop.Game;
using gbfr.qol.buildcard.Sources;

using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace gbfr.qol.buildcard.Game;

// Hooks the function that fills a CharaInfo from a character.
public unsafe class CharaStatusHooks : IFeature
{
    private readonly IReloadedHooks _hooks;
    private readonly UiTree _tree;
    private readonly GameApi _api;
    private readonly ILogger _logger;
    private bool _loggedBuild;

    // Raised after the game fills the card's CharaInfo.
    public event Action<CharaInfo, CharaSnapshot>? CharacterShown;

    private delegate void FillCharacterStatus(nint charaInfo, nint chara, int index);
    private IHook<FillCharacterStatus>? _fillHook;

    public CharaStatusHooks(IReloadedHooks hooks, UiTree tree, GameApi api, ILogger logger)
    {
        _hooks = hooks;
        _tree = tree;
        _api = api;
        _logger = logger;
    }

    public Capability[] Requires => [_api.FillCharacterStatus];

    public void Start() =>
        _fillHook = _hooks.CreateHook<FillCharacterStatus>(FillImpl, _api.FillCharacterStatus.Address).Activate();

    private void FillImpl(nint charaInfo, nint chara, int index)
    {
        _fillHook!.OriginalFunction(charaInfo, chara, index);
        var info = new CharaInfo(charaInfo);
        if (chara != 0 && _tree.IsOnCard(info))
            Dispatch.Raise(_logger, nameof(CharacterShown), CharacterShown, info, CharaSnapshot.Decode(chara, LogBuildOnce));
    }

    private void LogBuildOnce(string message)
    {
        if (_loggedBuild)
            return;
        _loggedBuild = true;
        _logger.WriteLine($"[gbfr.qol.buildcard] Chara build: {message}", Color.Yellow);
    }
}
