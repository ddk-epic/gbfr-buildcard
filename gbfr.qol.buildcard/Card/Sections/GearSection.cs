using gbfr.qol.buildcard.Card;
using gbfr.qol.buildcard.Game;
using gbfr.qol.buildcard.Interop.Game;
using gbfr.qol.buildcard.Model;
using gbfr.qol.buildcard.Sources;

namespace gbfr.qol.buildcard.Card.Sections;

// Writes the weapon heading and wraps the skill names and sigil trait names the game filled.
public class GearSection : ICardSection
{
    private const uint WeaponTitleHash = 0x53185300;  // TXT_PAU_ITEM_WEAPON
    private const uint WeaponTitleSubId = 0xDE6482AF;  // equip01_info01

    private readonly TextService _text;
    private readonly TextTable _table;

    public GearSection(TextService text, TextTable table)
    {
        _text = text;
        _table = table;
    }

    public void Write(IReadOnlyDictionary<int, UiObject> objects, BuildCard card) =>
        _text.Set(objects[CardLayout.Gear.WeaponTitle], _table.Find(WeaponTitleHash, WeaponTitleSubId), WeaponTitleHash);

    public void Layout(IReadOnlyDictionary<int, UiObject> objects, BuildCard card)
    {
        foreach (short id in CardLayout.Skills.Names)
            _text.Rewrap(objects[id], CardLayout.Skills.NameWidth, 2);
        foreach (short[] row in CardLayout.Gear.SigilTraitNames)
            foreach (short id in row)
                _text.Rewrap(objects[id], CardLayout.Gear.SigilTraitNameWidth, 1);
    }
}
