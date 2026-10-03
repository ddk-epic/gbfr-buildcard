using System.Drawing;
using System.Reflection;

using Reloaded.Mod.Interfaces;

namespace gbfr.qol.buildcard.Hooks;

// Writes the master traits board onto the card's Text objects after the game fills the page. The Text objects are
// found through their refs in CharaInfo.Powers.
public unsafe class CardWriter
{
    private const short SummaryTextId = 462;      // bc_text01
    private const short MasterTraitTextsId = 557;  // bc_mt_heading, see tools/scripts/add_master_traits.py
    private const int Powers = 0x3D0;
    private const int MaxRefs = 1024;
    private const int WrapLength = 19;

    // chara: 0x38-byte entries from 0x170, masteries followed by the master trait cells; both counts vary
    private const int Entries = 0x170;
    private const int EntriesEnd = 0x58B8;
    private const int EntrySize = 0x38;

    private static readonly int[] Slots = [4, 8, 8, 10];
    private static readonly int[] Budgets = [10, 10, 10, 20];
    private static readonly string[] StyleNames = ["Insight", "Essence", "Crux"];
    private static readonly string[] RankNames = ["1", "2", "3", "EX"];
    private static readonly int TextsPerStyle = 2 + 2 * Slots.Length + 2 * Slots.Sum();
    private static readonly int MasterTraitTextCount = 1 + StyleNames.Length * TextsPerStyle;

    private readonly TextHooks _text;
    private readonly ILogger _logger;
    private readonly Dictionary<uint, Cell> _cells = LoadCells();

    public CardWriter(TextHooks text, ILogger logger)
    {
        _text = text;
        _logger = logger;
    }

    // Logs exceptions instead of letting them reach the game's frames.
    public void OnFilled(nint charaInfo, nint chara)
    {
        try
        {
            Write(charaInfo, chara);
        }
        catch (Exception e)
        {
            _logger.WriteLine($"[gbfr.qol.buildcard] Writing the card failed: {e}", Color.Red);
        }
    }

    private void Write(nint charaInfo, nint chara)
    {
        var refs = FindRefs(charaInfo, SummaryTextId, (short)(MasterTraitTextsId + MasterTraitTextCount - 1));
        if (refs.Count != 1 + MasterTraitTextCount)
            return;

        var titles = new string[StyleNames.Length];
        var perks = new int[StyleNames.Length];
        var spent = new int[Slots.Length];
        var labels = new (Cell Cell, bool Picked)?[StyleNames.Length, Slots.Length, Slots.Max()];
        for (int offset = Entries; offset < EntriesEnd; offset += EntrySize)
        {
            if (!_cells.TryGetValue(*(uint*)(chara + offset), out Cell cell))
                continue;
            bool picked = *(int*)(chara + offset + 4) == 1;
            if (cell.Position == 0)
            {
                if (cell.Rank == 0)
                    titles[cell.Style] = cell.Label;
                perks[cell.Style] += picked ? 1 : 0;
            }
            else if (cell.Position <= Slots[cell.Rank])
            {
                labels[cell.Style, cell.Rank, cell.Position - 1] = (cell, picked);
                spent[cell.Rank] += picked ? 1 : 0;
            }
        }

        Set(refs, SummaryTextId, string.Join("  ·  ", StyleNames.Select((name, s) => $"{name} {new string('★', perks[s])}".TrimEnd())));
        int id = MasterTraitTextsId;
        Set(refs, id++, "MASTER TRAITS");
        for (int s = 0; s < StyleNames.Length; s++)
        {
            Set(refs, id++, titles[s] ?? "");
            Set(refs, id++, StyleNames[s].ToUpperInvariant());
            for (int r = 0; r < Slots.Length; r++)
            {
                Set(refs, id++, $"STYLE RANK {RankNames[r]}");
                Set(refs, id++, $"{spent[r]}/{Budgets[r]}");
            }
            for (int r = 0; r < Slots.Length; r++)
            {
                for (int c = 0; c < Slots[r]; c++)
                {
                    var slot = labels[s, r, c];
                    SetCell(refs, id++, slot is { Picked: true } ? slot.Value.Cell : null);
                    SetCell(refs, id++, slot is { Picked: false } ? slot.Value.Cell : null);
                }
            }
        }
    }

    private void SetCell(Dictionary<short, nint> refs, int id, Cell? cell)
    {
        if (refs.TryGetValue((short)id, out nint text))
            _text.Set(text, cell is { } c ? Wrap(c.Label) : "", cell?.TextHash ?? TextHooks.NoHash);
    }

    // Breaks a label of 19 characters or more into two lines at the space nearest its middle, counting a button
    // icon (<d>) as two.
    private static string Wrap(string label)
    {
        if (label.Replace("<d>", "xx").Length < WrapLength)
            return label;
        int best = -1;
        for (int i = label.IndexOf(' '); i != -1; i = label.IndexOf(' ', i + 1))
        {
            if (best == -1 || Math.Abs(i - label.Length / 2) < Math.Abs(best - label.Length / 2))
                best = i;
        }
        return best == -1 ? label : $"{label[..best]}\n{label[(best + 1)..]}";
    }

    private void Set(Dictionary<short, nint> refs, int id, string value)
    {
        if (refs.TryGetValue((short)id, out nint text))
            _text.Set(text, value);
    }

    // CharaInfo.Powers is a begin/end vector of 0x20-byte refs: vtable, object at +8, component at +0x10, component
    // name hash at +0x18, YAML ObjectRefId at +0x1E. Returns the components of the refs with ids in [firstId, lastId].
    private static Dictionary<short, nint> FindRefs(nint charaInfo, short firstId, short lastId)
    {
        var found = new Dictionary<short, nint>();
        nint begin = *(nint*)(charaInfo + Powers), end = *(nint*)(charaInfo + Powers + 8);
        long bytes = end - begin;
        if (begin == 0 || bytes <= 0 || bytes % 0x20 != 0 || bytes > MaxRefs * 0x20)
            return found;

        for (nint r = begin; r < end; r += 0x20)
        {
            short id = *(short*)(r + 0x1E);
            if (id >= firstId && id <= lastId)
                found[id] = *(nint*)(r + 0x10);
        }
        return found;
    }

    // master_traits.tsv, from tools/scripts/gen_master_traits.py: skillboard_effect key, style, rank, position
    // (0 for perks), label (the style's title on rank 1 perks), and for labels with button icons the hash of their
    // text tags
    private static Dictionary<uint, Cell> LoadCells()
    {
        var cells = new Dictionary<uint, Cell>();
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("master_traits.tsv")!;
        using var reader = new StreamReader(stream);
        while (reader.ReadLine() is { } line)
        {
            string[] fields = line.Split('\t');
            uint hash = fields[5].Length > 0 ? Convert.ToUInt32(fields[5], 16) : TextHooks.NoHash;
            cells[Convert.ToUInt32(fields[0], 16)] = new Cell(int.Parse(fields[1]), int.Parse(fields[2]), int.Parse(fields[3]), fields[4], hash);
        }
        return cells;
    }

    private readonly record struct Cell(int Style, int Rank, int Position, string Label, uint TextHash);
}
