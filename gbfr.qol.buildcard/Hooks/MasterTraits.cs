using System.Runtime.InteropServices;

using NenTools.Reloaded.ScanManager.Interfaces;

namespace gbfr.qol.buildcard.Hooks;

// Reads a chara's master trait cells from the game and fills their descriptions with the game's setter.
public unsafe class MasterTraits
{
    // SetSkillBoardDescription's mov r15, [rip + disp32]
    private const int TablesLoad = 0x43;

    private const long MaxMapSize = 0x100000;
    private const ulong LayoutType = 4;

    private readonly CellComponent* _component = (CellComponent*)NativeMemory.AllocZeroed((nuint)sizeof(CellComponent));
    private readonly TextEntry* _entry = (TextEntry*)NativeMemory.AllocZeroed((nuint)sizeof(TextEntry));
    private readonly Dictionary<uint, List<MasterTraitCell>> _cells = [];
    private SkillBoardTables** _tables;

    // SetSkillBoardDescription(cell component, chara key, slot)
    private delegate* unmanaged<CellComponent*, uint, int, void> _setDescription;

    public MasterTraits()
    {
        _component->Texts.Begin = _entry;
        _component->Texts.End = _component->Texts.Capacity = _entry + 1;
    }

    public void Init(IScanManager scanManager, string signatureGroup)
    {
        scanManager.AddScan("SetSkillBoardDescription", signatureGroup, address =>
        {
            _setDescription = (delegate* unmanaged<CellComponent*, uint, int, void>)(nint)address;
            _tables = (SkillBoardTables**)PeImage.RipGlobal((byte*)address + TablesLoad, [0x4C, 0x8B, 0x3D]);
        });
    }

    // The chara's master trait cells, empty when the data is unavailable
    public IReadOnlyList<MasterTraitCell> ReadCells(uint charaKey)
    {
        if (_cells.TryGetValue(charaKey, out var cells))
            return cells;
        if (_tables == null || *_tables == null)
            return [];
        ref var tables = ref **_tables;
        if (ReadMap(tables.Layouts) is not { } layouts
            || ReadMap(tables.LayoutRecords) is not { } records
            || ReadMap(tables.Effects) is not { } effects)
            return [];
        var recordsById = ByUintKey(records);
        var effectsByKey = ByUintKey(effects);

        cells = [];
        foreach (var (key, value) in layouts)
        {
            if ((uint)key != charaKey || key >> 48 != LayoutType)
                continue;
            if (!recordsById.TryGetValue((uint)value, out nint record) || record == 0)
                continue;
            uint effect = ((LayoutRecord*)record)->Effect;
            uint title = effectsByKey.TryGetValue(effect, out nint row) && row != 0
                ? ((EffectRow*)row)->Title : GameText.EmptyIdHash;
            cells.Add(new MasterTraitCell((int)(key >> 32 & 0xFFFF), effect, title));
        }
        _cells[charaKey] = cells;
        return cells;
    }

    // Sets text to the cell's description in the loaded language, as the Master Traits menu shows it
    public void FillDescription(TextComponent text, uint charaKey, int slot)
    {
        if (_setDescription == null)
            return;
        _entry->Text = text.Address;
        _setDescription(_component, charaKey, slot);
        _entry->Text = 0;
    }

    // Every node's key and value in the map, in list order; null when the list is broken
    private static List<(ulong Key, nint Value)>? ReadMap(in GameMap map)
    {
        var head = map.Head;
        if (map.Size < 0 || map.Size > MaxMapSize || head == null)
            return null;
        var entries = new List<(ulong, nint)>((int)map.Size);
        var prev = head;
        for (var node = head->Next; node != head; prev = node, node = node->Next)
        {
            if (entries.Count == map.Size || node == null || node->Prev != prev)
                return null;
            entries.Add((node->Key, node->Value));
        }
        return entries.Count == map.Size ? entries : null;
    }

    private static Dictionary<uint, nint> ByUintKey(List<(ulong Key, nint Value)> entries)
    {
        var map = new Dictionary<uint, nint>(entries.Count);
        foreach (var (key, value) in entries)
            map.TryAdd((uint)key, value);
        return map;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct SkillBoardTables
    {
        [FieldOffset(0x8)] public GameMap Effects;  // skillboard_effect key to effect row
        [FieldOffset(0x320)] public GameMap LayoutRecords;  // layout id to layout record
        [FieldOffset(0x6D0)] public GameMap Layouts;  // chara key | slot << 32 | LayoutType << 48 to layout id
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct LayoutRecord
    {
        [FieldOffset(0x48)] public uint Effect;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct EffectRow
    {
        [FieldOffset(0x44)] public uint Title;
    }

    // fake Master Traits cell component
    [StructLayout(LayoutKind.Explicit, Size = 0x88)]
    private struct CellComponent
    {
        [FieldOffset(0x60)] public GameVector<TextEntry> Texts;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x20)]
    private struct TextEntry
    {
        [FieldOffset(0x10)] public nint Text;
    }
}

public readonly record struct MasterTraitCell(int Slot, uint EffectKey, uint TitleTextId)
{
    private const int ExSlots = 50;
    private const int ExRank = 3;

    public int Style => Slot / 100;
    public bool IsPerk => Slot % 100 < 10;
    public int Rank => IsPerk ? Slot % 100 : Slot % 100 < ExSlots ? Slot % 100 / 10 - 1 : ExRank;
    // 1-based position in its rank; 0 for a perk
    public int Position => IsPerk ? 0 : Slot % 100 < ExSlots ? Slot % 10 + 1 : Slot % 100 - ExSlots + 1;
}
