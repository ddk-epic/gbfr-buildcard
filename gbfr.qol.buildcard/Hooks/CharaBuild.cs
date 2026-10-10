using System.Numerics;
using System.Runtime.InteropServices;

namespace gbfr.qol.buildcard.Hooks;

public sealed record CharaBuild(uint CharaKey, CharaEntry[] Entries, OverMasteryLine?[] OverMastery, uint?[] Summons)
{
    // chara: 400 entries, masteries [limit_bonus key, taken bit per LimitBonusParamIndex], then the master trait cells
    private const int EntriesOffset = 0x138;
    private const int EntriesEnd = 0x58B8;
    private const int EntrySize = 0x38;
    private const int CharaKeyOffset = 0x5EA8;

    private const int OverMasteryOffset = 0x58B8;
    public const int OverMasteryLines = 4;

    // summon: [summon key, summon id, trait key, equip bonus key, trait level, equip bonus level, unknown]
    private const int SummonsOffset = 0x5DD8;
    private const int SummonSize = 0x1C;
    public const int SummonCount = 4;

    // Reads chara, leaving out empty and invalid values.
    public static unsafe CharaBuild Decode(nint chara, Action<string> logOnce)
    {
        uint charaKey = *(uint*)(chara + CharaKeyOffset);
        if (charaKey == 0)
            logOnce("chara key is 0");

        var entries = new List<CharaEntry>();
        for (int offset = EntriesOffset; offset < EntriesEnd; offset += EntrySize)
        {
            uint key = *(uint*)(chara + offset);
            if (key != 0)
                entries.Add(new CharaEntry(key, *(int*)(chara + offset + 4)));
        }

        var overMastery = new OverMasteryLine?[OverMasteryLines];
        for (int i = 0; i < OverMasteryLines; i++)
        {
            var line = ((OverMasteryLine*)(chara + OverMasteryOffset))[i];
            if (line.Value == 0)
                continue;
            if (!float.IsFinite(line.Value) || !BitOperations.IsPow2(line.LevelBit))
                logOnce($"Over Mastery line {i} is invalid: level bit {line.LevelBit:X}, value {line.Value}");
            else
                overMastery[i] = line;
        }

        var summons = new uint?[SummonCount];
        for (int i = 0; i < SummonCount; i++)
        {
            uint id = *(uint*)(chara + SummonsOffset + i * SummonSize + 4);
            summons[i] = id != 0 ? id : null;
        }

        return new CharaBuild(charaKey, entries.ToArray(), overMastery, summons);
    }
}

// A chara entry: master trait cell or mastery key, with its picked or taken bits.
public readonly record struct CharaEntry(uint Key, int Bits);

// Over Mastery line: [limit_bonus_param key, 1 << (level - 1), unknown, float value]
[StructLayout(LayoutKind.Sequential, Size = 0x10)]
public readonly record struct OverMasteryLine(uint Key, uint LevelBit, uint Unknown, float Value);
