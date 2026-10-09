using System.Runtime.InteropServices;

using NenTools.Reloaded.ScanManager.Interfaces;

namespace gbfr.qol.buildcard.Hooks;

// Reads a chara's master trait cells from the game and fills their descriptions with the game's setter.
public unsafe class MasterTraits
{
    // SetSkillBoardDescription's mov r15, [rip + disp32] loading the skill board tables
    private const int TablesLoad = 0x43;

    // tables: MSVC unordered_maps, each a node list sentinel pointer and size; nodes are [next, prev, key, value]
    private const int LayoutNodes = 0x6D0;  // chara key | slot << 32 | LayoutType << 48 to layout id
    private const int TablesSize = LayoutNodes + 0x10;
    private const int NodeSize = 0x20;
    private const long MaxMapSize = 0x100000;
    private const int LayoutRecordNodes = 0x320;  // layout id to layout record
    private const int EffectNodes = 0x8;  // skillboard_effect key to effect row
    private const ulong LayoutType = 4;
    private const int LayoutEffect = 0x48;  // in a layout record
    private const int EffectTitle = 0x44;  // in an effect row

    // fake Master Traits cell component
    private const int ComponentSize = 0x88;
    private const int ComponentTexts = 0x60;
    private const int EntrySize = 0x20;
    private const int EntryText = 0x10;

    private readonly nint _component = (nint)NativeMemory.AllocZeroed(ComponentSize);
    private readonly nint _entry = (nint)NativeMemory.AllocZeroed(EntrySize);
    private readonly Dictionary<uint, List<MasterTraitCell>> _cells = [];
    private nint* _tables;

    // SetSkillBoardDescription(cell component, chara key, slot)
    private delegate* unmanaged<nint, uint, int, void> _setDescription;

    public MasterTraits()
    {
        nint* texts = (nint*)(_component + ComponentTexts);
        texts[0] = _entry;
        texts[1] = texts[2] = _entry + EntrySize;
    }

    public void Init(IScanManager scanManager, string signatureGroup)
    {
        scanManager.AddScan("SetSkillBoardDescription", signatureGroup, address =>
        {
            _setDescription = (delegate* unmanaged<nint, uint, int, void>)(nint)address;
            _tables = PeImage.RipGlobal((byte*)address + TablesLoad, [0x4C, 0x8B, 0x3D]);
        });
    }

    // The chara's master trait cells, empty when the data is unavailable
    public IReadOnlyList<MasterTraitCell> ReadCells(uint charaKey)
    {
        if (_cells.TryGetValue(charaKey, out var cells))
            return cells;
        if (_tables == null || !GameMemory.IsReadable(*_tables, TablesSize))
            return [];
        nint tables = *_tables;
        var region = new GameMemory.Region();
        if (ReadMap(tables + LayoutNodes, ref region) is not { } layouts
            || ReadMap(tables + LayoutRecordNodes, ref region) is not { } records
            || ReadMap(tables + EffectNodes, ref region) is not { } effects)
            return [];
        var recordsById = ByUintKey(records);
        var effectsByKey = ByUintKey(effects);

        cells = [];
        foreach (var (key, value) in layouts)
        {
            if ((uint)key != charaKey || key >> 48 != LayoutType)
                continue;
            if (!recordsById.TryGetValue((uint)value, out nint record) || !region.Covers(record, LayoutEffect + sizeof(uint)))
                continue;
            uint effect = *(uint*)(record + LayoutEffect);
            uint title = effectsByKey.TryGetValue(effect, out nint row) && region.Covers(row, EffectTitle + sizeof(uint))
                ? *(uint*)(row + EffectTitle) : GameText.EmptyIdHash;
            cells.Add(new MasterTraitCell((int)(key >> 32 & 0xFFFF), effect, title));
        }
        _cells[charaKey] = cells;
        return cells;
    }

    // Sets text to the cell's description in the loaded language, as the Master Traits menu shows it
    public void FillDescription(nint text, uint charaKey, int slot)
    {
        if (_setDescription == null)
            return;
        *(nint*)(_entry + EntryText) = text;
        _setDescription(_component, charaKey, slot);
        *(nint*)(_entry + EntryText) = 0;
    }

    // Every node's key and value in the map at map, in list order; null when the list is broken
    private static List<(ulong Key, nint Value)>? ReadMap(nint map, ref GameMemory.Region region)
    {
        nint head = *(nint*)map;
        long size = *(long*)(map + 8);
        if (size < 0 || size > MaxMapSize || !region.Covers(head, NodeSize))
            return null;
        var entries = new List<(ulong, nint)>((int)size);
        nint prev = head;
        for (nint node = *(nint*)head; node != head; prev = node, node = *(nint*)node)
        {
            if (entries.Count == size || !region.Covers(node, NodeSize) || *(nint*)(node + 8) != prev)
                return null;
            entries.Add((*(ulong*)(node + 0x10), *(nint*)(node + 0x18)));
        }
        return entries.Count == size ? entries : null;
    }

    // A uint-keyed map's entries by key
    private static Dictionary<uint, nint> ByUintKey(List<(ulong Key, nint Value)> entries)
    {
        var map = new Dictionary<uint, nint>(entries.Count);
        foreach (var (key, value) in entries)
            map.TryAdd((uint)key, value);
        return map;
    }
}

// A master trait cell read from the game.
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
