using System.Numerics;
using System.Runtime.InteropServices;

namespace gbfr.qol.buildcard.Hooks;

// Writes build data the page doesn't show onto the card's own Text objects, right after the game fills the page.
// The card's Text objects are referenced from CharaInfo.Powers in status01, which is how they're found at runtime;
// the game writes PWR into them first.
public unsafe class CardWriter
{
    private const short SummaryTextId = 462;  // bc_text01
    private const int CharaInfoSize = 0x900;
    private const int MaxRefs = 64;

    private const int MasterTraits = 0x32A8;
    private const int MasterTraitCount = 99;
    private const int MasterTraitSize = 0x38;
    private const int OverMasteries = 0x58B8;
    private const int OverMasterySize = 0x10;

    private readonly TextHooks _text;

    [DllImport("kernel32.dll")]
    private static extern bool ReadProcessMemory(nint process, nint address, void* buffer, nint size, out nint read);

    public CardWriter(TextHooks text)
    {
        _text = text;
    }

    public void OnFilled(nint charaInfo, nint chara)
    {
        nint summary = FindRef(charaInfo, SummaryTextId);
        if (summary == 0)
            return;

        int chosen = 0;
        for (int i = 0; i < MasterTraitCount; i++)
            chosen += *(int*)(chara + MasterTraits + i * MasterTraitSize + 4) == 1 ? 1 : 0;

        var levels = new int[4];
        for (int i = 0; i < 4; i++)
        {
            uint bits = *(uint*)(chara + OverMasteries + i * OverMasterySize + 4);
            levels[i] = bits == 0 ? 0 : BitOperations.Log2(bits) + 1;
        }

        _text.Set(summary, $"Master traits: {chosen} chosen    Over Mastery: Lv {string.Join(" / ", levels)}");
    }

    // A ref is 0x20 bytes: vtable, object at +8, component at +0x10, component name hash at +0x18, YAML ObjectRefId
    // at +0x1E. Ref lists are begin/end pairs. Every CharaInfo the game fills is searched, and most qword pairs in
    // one aren't lists, so reads go through ReadProcessMemory (-1: this process).
    private static nint FindRef(nint component, short objectId)
    {
        var block = new nint[CharaInfoSize / 8];
        fixed (nint* blockPtr = block)
        {
            if (!ReadProcessMemory(-1, component, blockPtr, CharaInfoSize, out _))
                return 0;
        }

        var refs = new byte[MaxRefs * 0x20];
        fixed (byte* refsPtr = refs)
        {
            for (int i = 0; i + 1 < block.Length; i++)
            {
                nint begin = block[i], end = block[i + 1];
                long bytes = end - begin;
                if (begin == 0 || bytes <= 0 || bytes % 0x20 != 0 || bytes > refs.Length)
                    continue;
                if (!ReadProcessMemory(-1, begin, refsPtr, (nint)bytes, out _))
                    continue;

                for (int r = 0; r < bytes; r += 0x20)
                {
                    if (*(short*)(refsPtr + r + 0x1E) == objectId)
                        return *(nint*)(refsPtr + r + 0x10);
                }
            }
        }
        return 0;
    }
}
