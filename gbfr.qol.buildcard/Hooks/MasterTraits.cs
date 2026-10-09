using System.Runtime.InteropServices;

using NenTools.Reloaded.ScanManager.Interfaces;

namespace gbfr.qol.buildcard.Hooks;

// Reads a chara's master trait cells from the game and fills their descriptions with the game's setter.
public unsafe class MasterTraits
{
    // SetSkillBoardDescription's mov r15, [rip + disp32] loading the skill board tables
    private const int TablesLoad = 0x43;

    // tables: MSVC unordered_maps, each a node list sentinel pointer; nodes are [next, prev, key, value]
    private const int LayoutNodes = 0x6D0;  // chara key | slot << 32 | LayoutType << 48 to layout id
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
            _tables = TablesFrom((byte*)address + TablesLoad);
        });
    }

    // The chara's master trait cells, empty when the data is unavailable
    public IReadOnlyList<MasterTraitCell> ReadCells(uint charaKey)
    {
        if (_cells.TryGetValue(charaKey, out var cells))
            return cells;
        if (_tables == null || *_tables == 0)
            return [];
        nint tables = *_tables;
        cells = [];
        nint head = *(nint*)(tables + LayoutNodes);
        for (nint node = *(nint*)head; node != head; node = *(nint*)node)
        {
            ulong key = *(ulong*)(node + 0x10);
            if ((uint)key != charaKey || key >> 48 != LayoutType)
                continue;
            nint record = Find(tables + LayoutRecordNodes, *(uint*)(node + 0x18));
            if (record == 0)
                continue;
            uint effect = *(uint*)(record + LayoutEffect);
            nint row = Find(tables + EffectNodes, effect);
            cells.Add(new MasterTraitCell((int)(key >> 32 & 0xFFFF), effect, row == 0 ? GameText.EmptyIdHash : *(uint*)(row + EffectTitle)));
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

    // The value of a uint key in the map whose node list sentinel is at list
    private static nint Find(nint list, uint key)
    {
        nint head = *(nint*)list;
        for (nint node = *(nint*)head; node != head; node = *(nint*)node)
        {
            if (*(uint*)(node + 0x10) == key)
                return *(nint*)(node + 0x18);
        }
        return 0;
    }

    private static nint* TablesFrom(byte* load)
    {
        if (load[0] != 0x4C || load[1] != 0x8B || load[2] != 0x3D)
            return null;
        return (nint*)(load + 7 + *(int*)(load + 3));
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
