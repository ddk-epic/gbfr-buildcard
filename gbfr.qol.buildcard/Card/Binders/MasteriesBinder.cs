using gbfr.qol.buildcard.Card;
using gbfr.qol.buildcard.Game;
using gbfr.qol.buildcard.Interop.Game;
using gbfr.qol.buildcard.Model;
using gbfr.qol.buildcard.Sources;

namespace gbfr.qol.buildcard.Card.Binders;

// Writes the masteries panel's two texts.
public class MasteriesBinder : ICardBinder
{
    private const uint MasteriesLabelHash = 0xB090BB12;  // TXT_PAU_TTL_LB
    private const uint CollectionLabelHash = 0x8278DE48;  // TXT_PAU_TREE_TAB_WEAPON

    private readonly TextService _text;
    private readonly TextTable _table;

    public MasteriesBinder(TextService text, TextTable table)
    {
        _text = text;
        _table = table;
    }

    public void Write(IReadOnlyDictionary<int, UiObject> objects, BuildCard card)
    {
        var masteries = card.Masteries;
        SetText(objects[CardLayout.Status.MasteryTexts[0]], MasteriesLabelHash, masteries?.Offense, masteries?.Defense);
        SetText(objects[CardLayout.Status.MasteryTexts[1]], CollectionLabelHash, masteries?.Collection, masteries?.Transcendence);
    }

    private void SetText(UiObject obj, uint labelId, int? first, int? second) =>
        _text.Set(obj, first == null ? "" : $"{_table.Find(labelId)} {first}% / {second}%");
}
