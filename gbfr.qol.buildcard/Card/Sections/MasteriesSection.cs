using gbfr.qol.buildcard.Card;
using gbfr.qol.buildcard.Game;
using gbfr.qol.buildcard.Interop.Game;
using gbfr.qol.buildcard.Model;
using gbfr.qol.buildcard.Sources;

namespace gbfr.qol.buildcard.Card.Sections;

// Writes the masteries panel's two texts.
public class MasteriesSection : ICardSection
{
    private const uint MasteriesLabelHash = 0xB090BB12;  // TXT_PAU_TTL_LB
    private const uint CollectionLabelHash = 0x8278DE48;  // TXT_PAU_TREE_TAB_WEAPON
    private const uint HyphenHash = 0x1B37394A;  // TXT_PAU_HYPHEN_SMALL

    private readonly TextService _text;
    private readonly TextTable _table;

    public MasteriesSection(TextService text, TextTable table)
    {
        _text = text;
        _table = table;
    }

    public void Write(IReadOnlyDictionary<int, UiObject> objects, BuildCard card)
    {
        var masteries = card.Masteries;
        SetText(objects[CardLayout.Status.MasteryTexts[0]], MasteriesLabelHash, card.Remote, masteries?.Offense,
            masteries?.Defense);
        SetText(objects[CardLayout.Status.MasteryTexts[1]], CollectionLabelHash, card.Remote, masteries?.Collection,
            masteries?.Transcendence);
    }

    // hyphens for another player's character
    private void SetText(UiObject obj, uint labelId, bool remote, int? first, int? second)
    {
        string label = _table.Find(labelId);
        if (remote)
        {
            string hyphen = _table.Find(HyphenHash);
            _text.Set(obj, $"{label} {hyphen} / {hyphen}");
        }
        else
            _text.Set(obj, first == null ? "" : $"{label} {first}% / {second}%");
    }
}
