using System.Reflection;

namespace gbfr.qol.buildcard.Hooks;

// Turns a chara's build into the card's writes, by CardIds.
public class CardContents
{
    // masteries.tsv sections
    private const int Offense = 0, OffenseExtension = 1, Defense = 2, DefenseExtension = 3, Collection = 4, Transcendence = 5;
    private const int Sections = 6;
    private const int ExtensionPercent = 2;

    // slots per rank of the normal and the captain's board, from the cells' Ids
    private static readonly int[] Slots = CardIds.CellOn[0][0].Select(rank => rank.Length).ToArray();
    private static readonly int[] CaptainSlots = CardIds.CellOn[1][0].Select(rank => rank.Length).ToArray();
    private static readonly int Styles = CardIds.CellOn[0].Length;
    private static readonly int[] Budgets = [10, 10, 10, 20];
    private static readonly string[] StyleNames = ["Insight", "Essence", "Crux"];

    private readonly Dictionary<(uint Chara, uint Key), string> _masteries = LoadMasteries();
    private readonly Dictionary<uint, int[]> _masteryTotals = new();

    public CardContents()
    {
        foreach (var ((chara, _), ladder) in _masteries)
        {
            if (!_masteryTotals.TryGetValue(chara, out int[]? totals))
                _masteryTotals[chara] = totals = new int[Sections];
            foreach (char section in ladder)
                if (section != '-')
                    totals[section - '0']++;
        }
    }

    public List<CardWrite> Compose(CharaBuild build, IReadOnlyList<MasterTraitCell> masterTraits)
    {
        var writes = new List<CardWrite>();
        ComposeMasterTraits(writes, build, masterTraits);
        ComposeMasteries(writes, build);
        ComposeOverMastery(writes, build);
        ComposeSummons(writes, build);
        return writes;
    }

    private static void ComposeMasterTraits(List<CardWrite> writes, CharaBuild build, IReadOnlyList<MasterTraitCell> masterTraits)
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

        for (int s = 0; s < Styles; s++)
        {
            writes.Add(new TextWrite(CardIds.PerkNames[s], StyleNames[s]));
            for (int k = 0; k < CardIds.PerkStars[s].Length; k++)
                writes.Add(new ActiveWrite(CardIds.PerkStars[s][k], k < perks[s]));
        }
        for (int s = 0; s < Styles; s++)
            writes.Add(titles[s] is { } title ? new LocalizedTextWrite(CardIds.StyleTitles[s], title) : new TextWrite(CardIds.StyleTitles[s], ""));
        writes.Add(new ActiveWrite(CardIds.Cells, !captain));
        writes.Add(new ActiveWrite(CardIds.CaptainCells, captain));
        for (int s = 0; s < Styles; s++)
        {
            for (int r = 0; r < slots.Length; r++)
                writes.Add(new TextWrite(CardIds.RankCounts[layout][s][r], $"{spent[r]}/{Budgets[r]}"));
            for (int r = 0; r < slots.Length; r++)
            {
                for (int c = 0; c < slots[r]; c++)
                {
                    var slot = placed[s, r, c];
                    writes.Add(CellWrite(CardIds.CellOn[layout][s][r][c], build.CharaKey, slot is { Picked: true } ? slot.Value.Cell : null));
                    writes.Add(CellWrite(CardIds.CellOff[layout][s][r][c], build.CharaKey, slot is { Picked: false } ? slot.Value.Cell : null));
                    writes.Add(new ActiveWrite(CardIds.CellPicked[layout][s][r][c], slot is { Picked: true }));
                }
            }
        }
    }

    private static CardWrite CellWrite(int id, uint charaKey, MasterTraitCell? cell) =>
        cell is { } c ? new MasterTraitDescriptionWrite(id, charaKey, c.Slot) : new TextWrite(id, "");

    private void ComposeMasteries(List<CardWrite> writes, CharaBuild build)
    {
        if (!_masteryTotals.TryGetValue(build.CharaKey, out int[]? totals))
        {
            writes.Add(new TextWrite(CardIds.MasteryTexts[0], ""));
            writes.Add(new TextWrite(CardIds.MasteryTexts[1], ""));
            return;
        }
        var taken = new int[Sections];
        foreach (var entry in build.Entries)
        {
            if (!_masteries.TryGetValue((build.CharaKey, entry.Key), out string? ladder))
                continue;
            for (int i = 0; i < ladder.Length; i++)
                if (ladder[i] != '-' && (entry.Bits & (1 << i)) != 0)
                    taken[ladder[i] - '0']++;
        }

        int Percent(int section) => totals[section] == 0 ? 0 : taken[section] * 100 / totals[section];
        int offense = Percent(Offense) + ExtensionPercent * taken[OffenseExtension];
        int defense = Percent(Defense) + ExtensionPercent * taken[DefenseExtension];
        writes.Add(new TextWrite(CardIds.MasteryTexts[0], $"Masteries: {offense}% / {defense}%"));
        writes.Add(new TextWrite(CardIds.MasteryTexts[1], $"Collection: {Percent(Collection)}% / {Percent(Transcendence)}%"));
    }

    private static void ComposeOverMastery(List<CardWrite> writes, CharaBuild build)
    {
        for (int i = 0; i < CharaBuild.OverMasteryLines; i++)
        {
            int id = CardIds.OverMasteryRows[i];
            if (build.OverMastery[i] is { } line)
                writes.Add(new OverMasteryWrite(id, i, line));
            writes.Add(new ActiveWrite(id, build.OverMastery[i] != null));
        }
    }

    private static void ComposeSummons(List<CardWrite> writes, CharaBuild build)
    {
        for (int i = 0; i < CharaBuild.SummonCount; i++)
            writes.Add(new SummonWrite(CardIds.SummonSlots[i], build.Summons[i] ?? 0));
    }

    // masteries.tsv: chara key, limit_bonus key, each LimitBonusParamIndex's section or -
    private static Dictionary<(uint, uint), string> LoadMasteries()
    {
        var masteries = new Dictionary<(uint, uint), string>();
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("masteries.tsv")!;
        using var reader = new StreamReader(stream);
        while (reader.ReadLine() is { } line)
        {
            string[] fields = line.Split('\t');
            masteries[(Convert.ToUInt32(fields[0], 16), Convert.ToUInt32(fields[1], 16))] = fields[2];
        }
        return masteries;
    }
}

// A write to a card object, found by its CardIds Id.
public abstract record CardWrite(int Id);
public sealed record TextWrite(int Id, string Value) : CardWrite(Id);
public sealed record LocalizedTextWrite(int Id, uint TextId) : CardWrite(Id);
public sealed record MasterTraitDescriptionWrite(int Id, uint CharaKey, int Slot) : CardWrite(Id);
public sealed record ActiveWrite(int Id, bool Active) : CardWrite(Id);
public sealed record SummonWrite(int Id, uint SummonId) : CardWrite(Id);
public sealed record OverMasteryWrite(int Id, int Line, OverMasteryLine Value) : CardWrite(Id);
