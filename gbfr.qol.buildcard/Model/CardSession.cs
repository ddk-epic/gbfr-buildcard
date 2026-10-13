using Reloaded.Mod.Interfaces;

using gbfr.qol.buildcard.Host;
using gbfr.qol.buildcard.Interop.Game;
using gbfr.qol.buildcard.Sources;

namespace gbfr.qol.buildcard.Model;

// Holds the card last shown on status01.
public class CardSession
{
    private readonly TraitTables _traitTables;
    private readonly MasteryReader _masteryReader;
    private readonly TextTable _table;
    private readonly ILogger _logger;

    // Raised on the game thread with the CharaInfo the card is shown on.
    public event Action<CharaInfo, BuildCard>? Shown;

    public CardSession(TraitTables traitTables, MasteryReader masteryReader, TextTable table, ILogger logger)
    {
        _traitTables = traitTables;
        _masteryReader = masteryReader;
        _table = table;
        _logger = logger;
    }

    public BuildCard? Current { get; private set; }

    public void OnCharacterShown(CharaInfo charaInfo, CharaSnapshot build)
    {
        Current = BuildCardAssembler.Build(build, _traitTables.ReadCells(build.CharaKey),
            _masteryReader.ReadPercents(build.CharaKey), _table.Find);
        Dispatch.Raise(_logger, nameof(Shown), Shown, charaInfo, Current);
    }
}
