using gbfr.qol.buildcard.Configuration;

namespace gbfr.qol.buildcard.Hooks;

// Turns a chara build into the card's writes, by CardIds.
public class CardContents
{
    // slots per rank of the normal and the captain's board, from the cells' Ids
    private static readonly int[] Slots = CardIds.CellOn[0][0].Select(rank => rank.Length).ToArray();
    private static readonly int[] CaptainSlots = CardIds.CellOn[1][0].Select(rank => rank.Length).ToArray();
    private static readonly int Styles = CardIds.CellOn[0].Length;
    private static readonly int[] Budgets = [10, 10, 10, 20];
    private const uint WeaponTitleHash = 0x53185300;  // TXT_PAU_ITEM_WEAPON
    private const uint WeaponTitleSubId = 0xDE6482AF;  // equip01_info01
    private const uint MasteriesLabelHash = 0xB090BB12;  // TXT_PAU_TTL_LB
    private const uint CollectionLabelHash = 0x8278DE48;  // TXT_PAU_TREE_TAB_WEAPON
    private const float IconScale = 0.7f;  // times the game's icon size
    private const string OverMasteryValuePrefix = " +";

    private readonly Func<uint, uint, string> _findText;
    private readonly MasterTraits _masterTraits;
    private readonly Masteries _masteries;
    private readonly Func<MasterTraitsPalette> _palette;

    public CardContents(Func<uint, uint, string> findText, MasterTraits masterTraits, Masteries masteries,
        Func<MasterTraitsPalette> palette)
    {
        _findText = findText;
        _masterTraits = masterTraits;
        _masteries = masteries;
        _palette = palette;
    }

    // The card's writes, to apply in order
    public List<CardWrite> Compose(CharaBuild build)
    {
        var writes = new List<CardWrite>();
        writes.Add(LocalizedText(CardIds.WeaponTitle, WeaponTitleHash, WeaponTitleSubId));
        ComposeMasterTraits(writes, build, _masterTraits.ReadCells(build.CharaKey), _palette());
        ComposeMasteries(writes, _masteries.ReadPercents(build.CharaKey));
        ComposeOverMastery(writes, build);
        ComposeSummons(writes, build);
        ComposeSkills(writes);
        ComposeSigils(writes);
        return writes;
    }

    private void ComposeMasterTraits(List<CardWrite> writes, CharaBuild build, IReadOnlyList<MasterTraitCell> masterTraits,
        MasterTraitsPalette palette)
    {
        var cells = new Dictionary<uint, MasterTraitCell>();
        foreach (var cell in masterTraits)
        {
            if (cell.Style < Styles)
                cells.TryAdd(cell.EffectKey, cell);
        }
        var titles = new uint?[Styles];
        var perks = new int[Styles];
        var spent = new int[Slots.Length];
        var placed = new (MasterTraitCell Cell, bool Picked)?[Styles, CaptainSlots.Length, CaptainSlots.Max()];
        bool captain = false;
        foreach (var entry in build.Entries)
        {
            if (!cells.TryGetValue(entry.Key, out MasterTraitCell cell))
                continue;
            bool picked = entry.Bits == 1;
            if (cell.IsPerk)
            {
                if (cell.Rank == 0 && cell.TitleTextId != GameText.EmptyIdHash)
                    titles[cell.Style] = cell.TitleTextId;
                perks[cell.Style] += picked ? 1 : 0;
            }
            else if (cell.Position <= CaptainSlots[cell.Rank])
            {
                placed[cell.Style, cell.Rank, cell.Position - 1] = (cell, picked);
                spent[cell.Rank] += picked ? 1 : 0;
                captain |= cell.Position > Slots[cell.Rank];
            }
        }
        var slots = captain ? CaptainSlots : Slots;
        int layout = captain ? 1 : 0;
        int shown = (int)(Enum.IsDefined(palette) ? palette : MasterTraitsPalette.Blue);

        for (int s = 0; s < Styles; s++)
        {
            writes.Add(new TextWrite(CardIds.PerkNames[s], titles[s] is { } title ? StyleName(_findText(title, GameText.EmptyIdHash)) : ""));
            for (int k = 0; k < CardIds.PerkStars[s].Length; k++)
                writes.Add(new ActiveWrite(CardIds.PerkStars[s][k], k < perks[s]));
        }
        for (int s = 0; s < Styles; s++)
            writes.Add(titles[s] is { } title ? LocalizedText(CardIds.StyleTitles[s], title) : new TextWrite(CardIds.StyleTitles[s], ""));
        writes.Add(new ActiveWrite(CardIds.Cells, !captain));
        writes.Add(new ActiveWrite(CardIds.CaptainCells, captain));
        for (int p = 0; p < CardIds.Scenes.Length; p++)
        {
            writes.Add(new ActiveWrite(CardIds.Scenes[p], p == shown));
            writes.Add(new ActiveWrite(CardIds.Palettes[p][layout], p == shown));
        }
        for (int s = 0; s < Styles; s++)
        {
            for (int r = 0; r < slots.Length; r++)
            {
                writes.Add(new TextWrite(CardIds.RankCounts[layout][s][r], $"{spent[r]}"));
                writes.Add(new TextWrite(CardIds.RankBudgets[layout][s][r], $"/{Budgets[r]}"));
            }
            for (int r = 0; r < slots.Length; r++)
            {
                for (int c = 0; c < slots[r]; c++)
                {
                    var slot = placed[s, r, c];
                    writes.Add(CellWrite(CardIds.CellOn[layout][s][r][c], build.CharaKey, slot is { Picked: true } ? slot.Value.Cell : null));
                    writes.Add(CellWrite(CardIds.CellOff[layout][s][r][c], build.CharaKey, slot is { Picked: false } ? slot.Value.Cell : null));
                    writes.Add(new ActiveWrite(CardIds.CellPicked[shown][layout][s][r][c], slot is { Picked: true }));
                    writes.Add(new ActiveWrite(CardIds.CellBases[shown][layout][s][r][c], slot is not { Picked: true }));
                }
            }
        }
    }

    private TextWrite LocalizedText(int id, uint textId, uint subId = GameText.EmptyIdHash) =>
        new(id, _findText(textId, subId), textId);

    private static string StyleName(string title)
    {
        int colon = title.IndexOfAny([':', '：']);
        return colon < 0 ? title : title[..colon].TrimEnd();
    }

    private static CardWrite CellWrite(int id, uint charaKey, MasterTraitCell? cell) =>
        cell is { } c ? new MasterTraitDescriptionWrite(id, charaKey, c.Slot, CardIds.CellTextWidth, IconScale) : new TextWrite(id, "");

    private void ComposeMasteries(List<CardWrite> writes, int[]? masteries)
    {
        writes.Add(MasteryWrite(CardIds.MasteryTexts[0], MasteriesLabelHash, masteries, Masteries.Offense, Masteries.Defense));
        writes.Add(MasteryWrite(CardIds.MasteryTexts[1], CollectionLabelHash, masteries, Masteries.Collection, Masteries.Transcendence));
    }

    private TextWrite MasteryWrite(int id, uint labelId, int[]? masteries, int first, int second) =>
        new(id, masteries == null ? "" : $"{_findText(labelId, GameText.EmptyIdHash)} {masteries[first]}% / {masteries[second]}%");

    private static void ComposeOverMastery(List<CardWrite> writes, CharaBuild build)
    {
        for (int i = 0; i < CharaBuild.OverMasteryLines; i++)
        {
            int id = CardIds.OverMasteryRows[i];
            if (build.OverMastery[i] is { } line)
                writes.Add(new OverMasteryWrite(id, i, line));
            writes.Add(new ActiveWrite(id, build.OverMastery[i] != null));
            // the row's value after its name
            if (build.OverMastery[i] != null)
                writes.Add(new SuffixWrite(CardIds.OverMasteryNames[i], CardIds.OverMasteryNameWidth, OverMasteryValuePrefix,
                    [CardIds.OverMasteryValues[i][1]], [CardIds.OverMasteryValues[i][2]], CardIds.OverMasteryValues[i]));
        }
    }

    private static void ComposeSummons(List<CardWrite> writes, CharaBuild build)
    {
        for (int i = 0; i < CharaBuild.SummonCount; i++)
            writes.Add(new SummonWrite(CardIds.SummonSlots[i], build.Summons[i] ?? 0));
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
