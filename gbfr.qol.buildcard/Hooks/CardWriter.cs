using System.Diagnostics;
using System.Drawing;
using System.Reflection;

using NenTools.Reloaded.ScanManager.Interfaces;
using Reloaded.Mod.Interfaces;

namespace gbfr.qol.buildcard.Hooks;

// Writes the card's texts, Over Mastery rows and summon slots after the game fills the page, and sets the weapon whose
// art WeaponArtHooks loads.
public unsafe class CardWriter
{
    private const short SummaryTextId = 462;      // bc_text01
    private const short MasterTraitTextsId = 557;  // bc_mt_heading
    private const short OverMasteryTextId = 769;  // bc_om_heading
    private const short OverMasteryRowsId = 770;  // bc_om_0
    private const int OverMasteryRowObjects = 32;
    private const short SummonSlotsId = 899;  // bc_smn_0
    private const int SummonObjects = 56;
    private const int Powers = 0x3D0;
    private const int MaxRefs = 1024;
    private const int WrapLength = 19;

    // chara: masteries, then the master trait cells
    private const int Entries = 0x170;
    private const int EntriesEnd = 0x58B8;
    private const int EntrySize = 0x38;

    // Over Mastery line: [limit_bonus_param key, 1 << (level - 1), unknown, float value]
    private const int OverMastery = 0x58B8;
    private const int OverMasteryLineSize = 0x10;
    private const int OverMasteryLines = 4;

    // summon: [summon key, summon id, trait key, equip bonus key, trait level, equip bonus level, unknown]
    private const int Summons = 0x5DD8;
    private const int SummonSize = 0x1C;
    private const int SummonCount = 4;

    private static readonly int[] Slots = [4, 8, 8, 10];
    private static readonly int[] Budgets = [10, 10, 10, 20];
    private static readonly string[] StyleNames = ["Insight", "Essence", "Crux"];
    private static readonly string[] RankNames = ["1", "2", "3", "EX"];
    private static readonly int TextsPerStyle = 2 + 2 * Slots.Length + 2 * Slots.Sum();
    private static readonly int MasterTraitTextCount = 1 + StyleNames.Length * TextsPerStyle;

    private readonly TextHooks _text;
    private readonly WeaponArtHooks _weaponArt;
    private readonly ILogger _logger;
    private readonly Dictionary<uint, Cell> _cells = LoadCells();
    private delegate* unmanaged<nint, byte, void> _setActive;
    private delegate* unmanaged<nint, nint, void> _setOverMasteryLine;
    private delegate* unmanaged<nint, uint, void> _setSummonInfo;
    private readonly nint _exeBase = Process.GetCurrentProcess().MainModule!.BaseAddress;
    private readonly nint _limitBonusInfoVtable;
    private readonly nint _summonInfoVtable;
    private bool _loggedComponents;

    public CardWriter(TextHooks text, WeaponArtHooks weaponArt, ILogger logger)
    {
        _text = text;
        _weaponArt = weaponArt;
        _logger = logger;
        _limitBonusInfoVtable = PeImage.FindVtable(_exeBase, ".?AVLimitBonusInfo@component@ui@@");
        if (_limitBonusInfoVtable == 0)
            _logger.WriteLine("[gbfr.qol.buildcard] LimitBonusInfo vtable not found", Color.Red);
        _summonInfoVtable = PeImage.FindVtable(_exeBase, ".?AVSummonInfo@component@ui@@");
        if (_summonInfoVtable == 0)
            _logger.WriteLine("[gbfr.qol.buildcard] SummonInfo vtable not found", Color.Red);
    }

    // SetObjectActive(object, active), SetOverMasteryLine(LimitBonusInfo component, Over Mastery line),
    // SetSummonInfo(SummonInfo component, summon id)
    public void Init(IScanManager scanManager, string signatureGroup)
    {
        scanManager.AddScan("SetObjectActive", signatureGroup, address =>
            _setActive = (delegate* unmanaged<nint, byte, void>)(nint)address);
        scanManager.AddScan("SetOverMasteryLine", signatureGroup, address =>
            _setOverMasteryLine = (delegate* unmanaged<nint, nint, void>)(nint)address);
        scanManager.AddScan("SetSummonInfo", signatureGroup, address =>
            _setSummonInfo = (delegate* unmanaged<nint, uint, void>)(nint)address);
    }

    // Catches and logs exceptions.
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
        var refs = FindRefs(charaInfo, SummaryTextId, (short)(SummonSlotsId + (SummonCount - 1) * SummonObjects));
        if (refs.Count != 1 + MasterTraitTextCount + 1 + OverMasteryLines + SummonCount)
            return;

        WriteMasterTraits(refs, chara);
        WriteOverMastery(refs, chara);
        WriteSummons(refs, chara);
        _weaponArt.Show(chara);
    }

    private void WriteMasterTraits(Dictionary<short, nint> refs, nint chara)
    {
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

    // Sets each row's LimitBonusInfo to its Over Mastery line and hides rows whose line has no value.
    private void WriteOverMastery(Dictionary<short, nint> refs, nint chara)
    {
        Set(refs, OverMasteryTextId, "OVER MASTERY");
        for (int i = 0; i < OverMasteryLines; i++)
        {
            nint line = chara + OverMastery + i * OverMasteryLineSize;
            int id = OverMasteryRowsId + i * OverMasteryRowObjects;
            bool shown = *(float*)(line + 0xC) != 0;
            if (shown)
                SetOverMasteryLine(refs, id, line);
            SetActive(refs, id, shown);
        }
    }

    private void SetOverMasteryLine(Dictionary<short, nint> refs, int id, nint line)
    {
        if (_setOverMasteryLine == null || _limitBonusInfoVtable == 0 || !refs.TryGetValue((short)id, out nint r))
            return;
        nint limitBonusInfo = FindComponent(*(nint*)(r + 8), _limitBonusInfoVtable);
        if (limitBonusInfo == 0)
            LogComponentsOnce(*(nint*)(r + 8), "LimitBonusInfo", _limitBonusInfoVtable);
        else
            _setOverMasteryLine(limitBonusInfo, line);
    }

    // Sets each slot's SummonInfo to the equipped summon's id.
    private void WriteSummons(Dictionary<short, nint> refs, nint chara)
    {
        for (int i = 0; i < SummonCount; i++)
            SetSummonInfo(refs, SummonSlotsId + i * SummonObjects, *(uint*)(chara + Summons + i * SummonSize + 4));
    }

    private void SetSummonInfo(Dictionary<short, nint> refs, int id, uint summonId)
    {
        if (_setSummonInfo == null || _summonInfoVtable == 0 || !refs.TryGetValue((short)id, out nint r))
            return;
        nint summonInfo = FindComponent(*(nint*)(r + 8), _summonInfoVtable);
        if (summonInfo == 0)
            LogComponentsOnce(*(nint*)(r + 8), "SummonInfo", _summonInfoVtable);
        else
            _setSummonInfo(summonInfo, summonId);
    }

    // Returns the object's first component with the given vtable, or 0. Components: 0x20-byte entries from +0x28,
    // component at +0x18.
    private static nint FindComponent(nint obj, nint vtable)
    {
        for (nint entry = *(nint*)(obj + 0x28); entry < *(nint*)(obj + 0x30); entry += 0x20)
        {
            nint component = *(nint*)(entry + 0x18);
            if (component != 0 && *(nint*)component == vtable)
                return component;
        }
        return 0;
    }

    private void LogComponentsOnce(nint obj, string name, nint vtable)
    {
        if (_loggedComponents)
            return;
        _loggedComponents = true;
        var vtables = new List<string>();
        for (nint entry = *(nint*)(obj + 0x28); entry < *(nint*)(obj + 0x30); entry += 0x20)
        {
            nint component = *(nint*)(entry + 0x18);
            vtables.Add(component == 0 ? "null" : $"exe+{*(nint*)component - _exeBase:X}");
        }
        _logger.WriteLine($"[gbfr.qol.buildcard] No {name} on a card object (vtable exe+{vtable - _exeBase:X}); components: {string.Join(", ", vtables)}", Color.Yellow);
    }

    private void SetCell(Dictionary<short, nint> refs, int id, Cell? cell)
    {
        if (refs.TryGetValue((short)id, out nint r))
            _text.Set(*(nint*)(r + 0x10), cell is { } c ? Wrap(c.Label) : "", cell?.TextHash ?? TextHooks.NoHash);
    }

    // Breaks a label of 19 or more characters into two lines at the space nearest its middle; <d> counts as two.
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
        if (refs.TryGetValue((short)id, out nint r))
            _text.Set(*(nint*)(r + 0x10), value);
    }

    private void SetActive(Dictionary<short, nint> refs, int id, bool active)
    {
        if (_setActive != null && refs.TryGetValue((short)id, out nint r))
            _setActive(*(nint*)(r + 8), active ? (byte)1 : (byte)0);
    }

    // Returns the refs in CharaInfo.Powers with ids in [firstId, lastId]. Refs: 0x20 bytes, object at +8, component at
    // +0x10, component name hash at +0x18, ObjectRefId at +0x1E.
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
                found[id] = r;
        }
        return found;
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
            uint hash = fields[5].Length > 0 ? Convert.ToUInt32(fields[5], 16) : TextHooks.NoHash;
            cells[Convert.ToUInt32(fields[0], 16)] = new Cell(int.Parse(fields[1]), int.Parse(fields[2]), int.Parse(fields[3]), fields[4], hash);
        }
        return cells;
    }

    private readonly record struct Cell(int Style, int Rank, int Position, string Label, uint TextHash);
}
