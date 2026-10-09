using System.Reflection;

namespace gbfr.qol.buildcard.Hooks;

// Turns a chara's build into the card's writes, by CardIds.
public class CardContents
{
    private const int WrapLength = 19;
    private const int SkillNameWrapLength = 15;

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

    private readonly Dictionary<uint, Cell> _cells = LoadCells();
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

    public List<CardWrite> Compose(CharaBuild build)
    {
        var writes = new List<CardWrite>();
        ComposeMasterTraits(writes, build);
        ComposeMasteries(writes, build);
        ComposeOverMastery(writes, build);
        ComposeSummons(writes, build);
        return writes;
    }

    private void ComposeMasterTraits(List<CardWrite> writes, CharaBuild build)
    {
        var titles = new string[Styles];
        var perks = new int[Styles];
        var spent = new int[Slots.Length];
        var labels = new (Cell Cell, bool Picked)?[Styles, CaptainSlots.Length, CaptainSlots.Max()];
        bool captain = false;
        foreach (var entry in build.Entries)
        {
            if (!_cells.TryGetValue(entry.Key, out Cell cell))
                continue;
            bool picked = entry.Bits == 1;
            if (cell.Position == 0)
            {
                if (cell.Rank == 0)
                    titles[cell.Style] = cell.Label;
                perks[cell.Style] += picked ? 1 : 0;
            }
            else if (cell.Position <= CaptainSlots[cell.Rank])
            {
                labels[cell.Style, cell.Rank, cell.Position - 1] = (cell, picked);
                spent[cell.Rank] += picked ? 1 : 0;
                captain |= cell.Position > Slots[cell.Rank];
            }
        }
        var slots = captain ? CaptainSlots : Slots;
        int board = captain ? 1 : 0;

        for (int s = 0; s < Styles; s++)
        {
            writes.Add(new TextWrite(CardIds.PerkNames[s], StyleNames[s]));
            for (int k = 0; k < CardIds.PerkStars[s].Length; k++)
                writes.Add(new ActiveWrite(CardIds.PerkStars[s][k], k < perks[s]));
        }
        for (int s = 0; s < Styles; s++)
            writes.Add(new TextWrite(CardIds.StyleTitles[s], titles[s] is { } title ? $"{StyleNames[s]}: {title}" : ""));
        writes.Add(new ActiveWrite(CardIds.Cells, !captain));
        writes.Add(new ActiveWrite(CardIds.CaptainCells, captain));
        for (int s = 0; s < Styles; s++)
        {
            for (int r = 0; r < slots.Length; r++)
                writes.Add(new TextWrite(CardIds.RankCounts[board][s][r], $"{spent[r]}/{Budgets[r]}"));
            for (int r = 0; r < slots.Length; r++)
            {
                for (int c = 0; c < slots[r]; c++)
                {
                    var slot = labels[s, r, c];
                    writes.Add(CellWrite(CardIds.CellOn[board][s][r][c], slot is { Picked: true } ? slot.Value.Cell : null));
                    writes.Add(CellWrite(CardIds.CellOff[board][s][r][c], slot is { Picked: false } ? slot.Value.Cell : null));
                    writes.Add(new ActiveWrite(CardIds.CellPicked[board][s][r][c], slot is { Picked: true }));
                }
            }
        }
    }

    private static TextWrite CellWrite(int id, Cell? cell) =>
        cell is { } c ? new TextWrite(id, Wrap(c.Label, WrapLength), c.TextHash) : new TextWrite(id, "");

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

    public static string WrapSkillName(string name) => Wrap(name, SkillNameWrapLength);

    // Breaks a one-line label of length or more characters at the space nearest its middle; <d> counts as two.
    private static string Wrap(string label, int length)
    {
        if (label.Contains('\n') || label.Replace("<d>", "xx").Length < length)
            return label;
        int best = -1;
        for (int i = label.IndexOf(' '); i != -1; i = label.IndexOf(' ', i + 1))
        {
            if (best == -1 || Math.Abs(i - label.Length / 2) < Math.Abs(best - label.Length / 2))
                best = i;
        }
        return best == -1 ? label : $"{label[..best]}\n{label[(best + 1)..]}";
    }

    // master_traits.tsv: skillboard_effect key, style, rank, position, label, text tag hash
    private static Dictionary<uint, Cell> LoadCells()
    {
        var cells = new Dictionary<uint, Cell>();
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("master_traits.tsv")!;
        using var reader = new StreamReader(stream);
        while (reader.ReadLine() is { } line)
        {
            string[] fields = line.Split('\t');
            uint hash = fields[5].Length > 0 ? Convert.ToUInt32(fields[5], 16) : GameText.NoHash;
            cells[Convert.ToUInt32(fields[0], 16)] = new Cell(int.Parse(fields[1]), int.Parse(fields[2]), int.Parse(fields[3]), fields[4], hash);
        }
        return cells;
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

    private readonly record struct Cell(int Style, int Rank, int Position, string Label, uint TextHash);
}

// A write to a card object, found by its CardIds Id.
public abstract record CardWrite(int Id);
public sealed record TextWrite(int Id, string Value, uint Hash = GameText.NoHash) : CardWrite(Id);
public sealed record ActiveWrite(int Id, bool Active) : CardWrite(Id);
public sealed record SummonWrite(int Id, uint SummonId) : CardWrite(Id);
public sealed record OverMasteryWrite(int Id, int Line, OverMasteryLine Value) : CardWrite(Id);
