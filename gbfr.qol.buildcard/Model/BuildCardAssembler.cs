using gbfr.qol.buildcard.Sources;

namespace gbfr.qol.buildcard.Model;

// Assembles a BuildCard from a chara build and the game's tables.
public static class BuildCardAssembler
{
    private const int Styles = CardLayout.MasterTraits.StyleCount;
    private static readonly int[] Slots = CardLayout.MasterTraits.Slots;
    private static readonly int[] CaptainSlots = CardLayout.MasterTraits.CaptainSlots;
    public static readonly int[] Budgets = [10, 10, 10, 20];

    // findText(text id, sub-id): the text in the loaded language
    public static BuildCard Build(CharaSnapshot build, IReadOnlyList<MasterTraitCell> cells, int[]? percents,
        Func<uint, uint, string> findText, Func<uint, OverMasteryParam?> findParam, string language) =>
        new(build.CharaKey,
            new WeaponPart(build.WeaponKey, build.MirageKey, build.AlternateArt),
            BuildTraits(build, cells, findText),
            percents == null ? null : new MasteryPercents(percents[MasteryReader.Offense], percents[MasteryReader.Defense],
                percents[MasteryReader.Collection], percents[MasteryReader.Transcendence]),
            build.OverMastery.Select(line => BuildOverMastery(line, findParam)).ToArray(),
            build.Summons) { CharaName = build.CharaName, Language = language };

    // null for an empty line or one without a param
    private static OverMasteryRow? BuildOverMastery(OverMasteryLine? line, Func<uint, OverMasteryParam?> findParam) =>
        line is { } value && findParam(value.Key) is { } param
            ? new OverMasteryRow(value, (int)(param.Scale * value.Value), param.Percent)
            : null;

    private static TraitBoard BuildTraits(CharaSnapshot build, IReadOnlyList<MasterTraitCell> cells,
        Func<uint, uint, string> findText)
    {
        var byKey = new Dictionary<uint, MasterTraitCell>();
        foreach (var cell in cells)
        {
            if (cell.Style < Styles)
                byKey.TryAdd(cell.EffectKey, cell);
        }
        var titles = new uint?[Styles];
        var perks = new int[Styles];
        var spent = new int[Slots.Length];
        var placed = new TraitCell?[Styles, CaptainSlots.Length, CaptainSlots.Max()];
        bool captain = false;
        foreach (var entry in build.Entries)
        {
            if (!byKey.TryGetValue(entry.Key, out MasterTraitCell cell))
                continue;
            bool picked = entry.Bits == 1;
            if (cell.IsPerk)
            {
                if (cell.Rank == 0 && cell.TitleTextId != TextTable.EmptyIdHash)
                    titles[cell.Style] = cell.TitleTextId;
                perks[cell.Style] += picked ? 1 : 0;
            }
            else if (cell.Position <= CaptainSlots[cell.Rank])
            {
                placed[cell.Style, cell.Rank, cell.Position - 1] = new TraitCell(cell.Slot, picked);
                spent[cell.Rank] += picked ? 1 : 0;
                captain |= cell.Position > Slots[cell.Rank];
            }
        }

        var slots = captain ? CaptainSlots : Slots;
        var styles = new StyleBoard[Styles];
        for (int s = 0; s < Styles; s++)
        {
            string title = titles[s] is { } id ? findText(id, TextTable.EmptyIdHash) : "";
            var ranks = new TraitCell?[slots.Length][];
            for (int r = 0; r < slots.Length; r++)
            {
                ranks[r] = new TraitCell?[slots[r]];
                for (int c = 0; c < slots[r]; c++)
                    ranks[r][c] = placed[s, r, c];
            }
            styles[s] = new StyleBoard(titles[s], title, StyleName(title), perks[s], ranks);
        }
        return new TraitBoard(captain, spent, Budgets, styles);
    }

    // The title up to its colon
    private static string StyleName(string title)
    {
        int colon = title.IndexOfAny([':', '：']);
        return colon < 0 ? title : title[..colon].TrimEnd();
    }
}
