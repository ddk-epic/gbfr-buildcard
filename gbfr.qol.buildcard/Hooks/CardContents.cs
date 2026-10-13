using gbfr.qol.buildcard.Configuration;
using gbfr.qol.buildcard.Model;

namespace gbfr.qol.buildcard.Hooks;

// Turns a BuildCard into the card's writes, by CardIds.
public class CardContents
{
    private const uint WeaponTitleHash = 0x53185300;  // TXT_PAU_ITEM_WEAPON
    private const uint WeaponTitleSubId = 0xDE6482AF;  // equip01_info01
    private const uint MasteriesLabelHash = 0xB090BB12;  // TXT_PAU_TTL_LB
    private const uint CollectionLabelHash = 0x8278DE48;  // TXT_PAU_TREE_TAB_WEAPON
    private const float IconScale = 0.7f;  // times the game's icon size
    private const string OverMasteryValuePrefix = " +";

    private readonly Func<uint, uint, string> _findText;
    private readonly Func<MasterTraitsPalette> _palette;

    public CardContents(Func<uint, uint, string> findText, Func<MasterTraitsPalette> palette)
    {
        _findText = findText;
        _palette = palette;
    }

    // The card's writes, to apply in order
    public List<CardWrite> Compose(BuildCard card)
    {
        var writes = new List<CardWrite>();
        writes.Add(LocalizedText(CardIds.WeaponTitle, WeaponTitleHash, WeaponTitleSubId));
        ComposeMasterTraits(writes, card.CharaKey, card.Traits, _palette());
        ComposeMasteries(writes, card.Masteries);
        ComposeOverMastery(writes, card.OverMastery);
        ComposeSummons(writes, card.Summons);
        ComposeSkills(writes);
        ComposeSigils(writes);
        return writes;
    }

    private static void ComposeMasterTraits(List<CardWrite> writes, uint charaKey, TraitBoard traits,
        MasterTraitsPalette palette)
    {
        int layout = traits.Captain ? 1 : 0;
        int shown = (int)(Enum.IsDefined(palette) ? palette : MasterTraitsPalette.Blue);
        var styles = traits.Styles;

        for (int s = 0; s < styles.Length; s++)
        {
            writes.Add(new TextWrite(CardIds.PerkNames[s], styles[s].Name));
            for (int k = 0; k < CardIds.PerkStars[s].Length; k++)
                writes.Add(new ActiveWrite(CardIds.PerkStars[s][k], k < styles[s].Perks));
        }
        for (int s = 0; s < styles.Length; s++)
            writes.Add(styles[s].TitleTextId is { } title
                ? new TextWrite(CardIds.StyleTitles[s], styles[s].Title, title)
                : new TextWrite(CardIds.StyleTitles[s], ""));
        writes.Add(new ActiveWrite(CardIds.Cells, !traits.Captain));
        writes.Add(new ActiveWrite(CardIds.CaptainCells, traits.Captain));
        for (int p = 0; p < CardIds.Scenes.Length; p++)
        {
            writes.Add(new ActiveWrite(CardIds.Scenes[p], p == shown));
            writes.Add(new ActiveWrite(CardIds.Palettes[p][layout], p == shown));
        }
        for (int s = 0; s < styles.Length; s++)
        {
            var ranks = styles[s].Cells;
            for (int r = 0; r < ranks.Length; r++)
            {
                writes.Add(new TextWrite(CardIds.RankCounts[layout][s][r], $"{traits.Spent[r]}"));
                writes.Add(new TextWrite(CardIds.RankBudgets[layout][s][r], $"/{traits.Budgets[r]}"));
            }
            for (int r = 0; r < ranks.Length; r++)
            {
                for (int c = 0; c < ranks[r].Length; c++)
                {
                    var cell = ranks[r][c];
                    writes.Add(CellWrite(CardIds.CellOn[layout][s][r][c], charaKey, cell is { Picked: true } ? cell : null));
                    writes.Add(CellWrite(CardIds.CellOff[layout][s][r][c], charaKey, cell is { Picked: false } ? cell : null));
                    writes.Add(new ActiveWrite(CardIds.CellPicked[shown][layout][s][r][c], cell is { Picked: true }));
                    writes.Add(new ActiveWrite(CardIds.CellBases[shown][layout][s][r][c], cell is not { Picked: true }));
                }
            }
        }
    }

    private TextWrite LocalizedText(int id, uint textId, uint subId = GameText.EmptyIdHash) =>
        new(id, _findText(textId, subId), textId);

    private static CardWrite CellWrite(int id, uint charaKey, TraitCell? cell) =>
        cell is { } c ? new MasterTraitDescriptionWrite(id, charaKey, c.Slot, CardIds.CellTextWidth, IconScale) : new TextWrite(id, "");

    private void ComposeMasteries(List<CardWrite> writes, MasteryPercents? masteries)
    {
        writes.Add(MasteryWrite(CardIds.MasteryTexts[0], MasteriesLabelHash, masteries?.Offense, masteries?.Defense));
        writes.Add(MasteryWrite(CardIds.MasteryTexts[1], CollectionLabelHash, masteries?.Collection, masteries?.Transcendence));
    }

    private TextWrite MasteryWrite(int id, uint labelId, int? first, int? second) =>
        new(id, first == null ? "" : $"{_findText(labelId, GameText.EmptyIdHash)} {first}% / {second}%");

    private static void ComposeOverMastery(List<CardWrite> writes, OverMasteryLine?[] lines)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            int id = CardIds.OverMasteryRows[i];
            if (lines[i] is { } line)
                writes.Add(new OverMasteryWrite(id, i, line));
            writes.Add(new ActiveWrite(id, lines[i] != null));
            // the row's value after its name
            if (lines[i] != null)
                writes.Add(new SuffixWrite(CardIds.OverMasteryNames[i], CardIds.OverMasteryNameWidth, OverMasteryValuePrefix,
                    [CardIds.OverMasteryValues[i][1]], [CardIds.OverMasteryValues[i][2]], CardIds.OverMasteryValues[i]));
        }
    }

    private static void ComposeSummons(List<CardWrite> writes, uint?[] summons)
    {
        for (int i = 0; i < summons.Length; i++)
            writes.Add(new SummonWrite(CardIds.SummonSlots[i], summons[i] ?? 0));
    }

    private static void ComposeSkills(List<CardWrite> writes)
    {
        foreach (short id in CardIds.SkillNames)
            writes.Add(new RewrapWrite(id, CardIds.SkillNameWidth, 2));
    }

    private static void ComposeSigils(List<CardWrite> writes)
    {
        foreach (short[] row in CardIds.SigilTraitNames)
            foreach (short id in row)
                writes.Add(new RewrapWrite(id, CardIds.SigilTraitNameWidth, 1));
    }
}

// A write to a card object, found by its CardIds Id.
public abstract record CardWrite(int Id);
public sealed record TextWrite(int Id, string Value, uint Hash = GameText.EmptyIdHash) : CardWrite(Id);
public sealed record MasterTraitDescriptionWrite(int Id, uint CharaKey, int Slot, int Width, float IconScale) : CardWrite(Id);
public sealed record ActiveWrite(int Id, bool Active) : CardWrite(Id);
public sealed record SummonWrite(int Id, uint SummonId) : CardWrite(Id);
public sealed record OverMasteryWrite(int Id, int Line, OverMasteryLine Value) : CardWrite(Id);
public sealed record RewrapWrite(int Id, int Width, int MaxLines) : CardWrite(Id);
public sealed record SuffixWrite(int Id, int Width, string Prefix, short[] Values, short[] ActiveValues, short[] Hidden) : CardWrite(Id);
