using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace gbfr.qol.buildcard.Sources;

public sealed record CharaSnapshot(uint CharaKey, uint WeaponKey, uint MirageKey, bool AlternateArt, CharaEntry[] Entries,
    OverMasteryLine?[] OverMastery, uint?[] Summons)
{
    // from the CharaInfo's Names text
    public string CharaName { get; init; } = "";

    private const byte AlternateArtFlag = 0x20;
    public const int OverMasteryLines = 4;
    public const int SummonCount = 4;

    // Reads chara, leaving out empty and invalid values.
    public static unsafe CharaSnapshot Decode(nint address, Action<string> logOnce)
    {
        ref readonly var chara = ref *(Chara*)address;
        if (chara.CharaKey == 0)
            logOnce("chara key is 0");

        var entries = new List<CharaEntry>();
        foreach (var entry in chara.Entries)
        {
            if (entry.Key != 0)
                entries.Add(new CharaEntry(entry.Key, entry.Bits));
        }

        var overMastery = new OverMasteryLine?[OverMasteryLines];
        for (int i = 0; i < OverMasteryLines; i++)
        {
            var line = chara.OverMastery[i];
            if (line.Value == 0)
                continue;
            if (!float.IsFinite(line.Value) || !BitOperations.IsPow2(line.LevelBit))
                logOnce($"Over Mastery line {i} is invalid: level bit {line.LevelBit:X}, value {line.Value}");
            else
                overMastery[i] = line;
        }

        var summons = new uint?[SummonCount];
        for (int i = 0; i < SummonCount; i++)
            summons[i] = chara.Summons[i].SummonId is var id and not 0 ? id : null;

        return new CharaSnapshot(chara.CharaKey, chara.WeaponKey, chara.MirageKey, (chara.WeaponFlags & AlternateArtFlag) != 0,
            entries.ToArray(), overMastery, summons);
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct Chara
    {
        [FieldOffset(0x54)] public uint WeaponKey;
        [FieldOffset(0x58)] public uint MirageKey;
        [FieldOffset(0x5C)] public byte WeaponFlags;
        [FieldOffset(0x138)] public EntryArray Entries;
        [FieldOffset(0x58B8)] public OverMasteryArray OverMastery;
        [FieldOffset(0x5DD8)] public SummonArray Summons;
        [FieldOffset(0x5EA8)] public uint CharaKey;
    }

    // masteries [limit_bonus key, taken bit per LimitBonusParamIndex], then the master trait cells
    [StructLayout(LayoutKind.Sequential, Size = 0x38)]
    private struct Entry
    {
        public uint Key;
        public int Bits;
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x1C)]
    private struct Summon
    {
        public uint SummonKey;
        public uint SummonId;
        public uint TraitKey;
        public uint EquipBonusKey;
        public uint TraitLevel;
        public uint EquipBonusLevel;
        public uint Unknown;
    }

    [InlineArray(400)]
    private struct EntryArray
    {
        private Entry _element;
    }

    [InlineArray(OverMasteryLines)]
    private struct OverMasteryArray
    {
        private OverMasteryLine _element;
    }

    [InlineArray(SummonCount)]
    private struct SummonArray
    {
        private Summon _element;
    }
}

// A chara entry: master trait cell or mastery key, with its picked or taken bits.
public readonly record struct CharaEntry(uint Key, int Bits);

// Over Mastery line: [limit_bonus_param key, 1 << (level - 1), unknown, float value]
[StructLayout(LayoutKind.Sequential, Size = 0x10)]
public readonly record struct OverMasteryLine(uint Key, uint LevelBit, uint Unknown, float Value);
