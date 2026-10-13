using Reloaded.Mod.Interfaces;

using gbfr.qol.buildcard.Hooks;
using gbfr.qol.buildcard.Host;

namespace gbfr.qol.buildcard.Model;

// Holds the card last shown on status01.
public class CardSession
{
    private readonly MasterTraits _masterTraits;
    private readonly Masteries _masteries;
    private readonly GameText _text;
    private readonly ILogger _logger;

    // Raised on the game thread with the CharaInfo the card is shown on.
    public event Action<CharaInfo, BuildCard>? Shown;

    public CardSession(MasterTraits masterTraits, Masteries masteries, GameText text, ILogger logger)
    {
        _masterTraits = masterTraits;
        _masteries = masteries;
        _text = text;
        _logger = logger;
    }

    public BuildCard? Current { get; private set; }

    public void OnCharacterShown(CharaInfo charaInfo, CharaBuild build)
    {
        Current = BuildCardAssembler.Build(build, _masterTraits.ReadCells(build.CharaKey),
            _masteries.ReadPercents(build.CharaKey), _text.Find);
        Dispatch.Raise(_logger, nameof(Shown), Shown, charaInfo, Current);
    }

    public void SetCharaName(string name)
    {
        if (Current != null)
            Current = Current with { CharaName = name };
    }
}
