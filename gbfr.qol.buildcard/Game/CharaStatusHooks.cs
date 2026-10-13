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

    private delegate void FillCharacterStatus(nint charaInfo, nint chara, int index);
    private IHook<FillCharacterStatus>? _fillHook;

    public CharaStatusHooks(IReloadedHooks hooks, UiTree tree, TextService text, GameApi api, ILogger logger)
    {
        _hooks = hooks;
        _tree = tree;
        _text = text;
        _api = api;
        _logger = logger;
    }

    public Capability[] Requires => [_api.FillCharacterStatus];

    public void Start() =>
        _fillHook = _hooks.CreateHook<FillCharacterStatus>(FillImpl, _api.FillCharacterStatus.Address).Activate();

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
            _fillHook!.OriginalFunction(charaInfo, chara, index);
            if (chara != 0)
                RaiseCharacterShown(info, chara);
        }
        finally
        {
            Dispatch.Raise(_logger, nameof(Filled), Filled);
        }
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
