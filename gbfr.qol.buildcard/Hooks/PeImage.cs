using System.Text;

namespace gbfr.qol.buildcard.Hooks;

// Reads the loaded exe's sections, MSVC RTTI and RIP-relative globals.
public static unsafe class PeImage
{
    private static (nint Start, nint End)? FindSection(nint image, string name)
    {
        byte* nt = (byte*)image + *(int*)(image + 0x3C);
        int sections = *(ushort*)(nt + 6);
        byte* header = nt + 24 + *(ushort*)(nt + 20);
        for (int i = 0; i < sections; i++, header += 40)
        {
            if (Encoding.ASCII.GetString(header, 8).TrimEnd('\0') == name)
                return (image + *(int*)(header + 12), image + *(int*)(header + 12) + *(int*)(header + 8));
        }
        return null;
    }

    // Returns the vtable for an RTTI type name such as ".?AVSummonInfo@component@ui@@", or 0.
    public static nint FindVtable(nint image, string typeName)
    {
        if (FindSection(image, ".data") is not var (dataStart, dataEnd) || FindSection(image, ".rdata") is not var (rdataStart, rdataEnd))
            return 0;

        var data = new ReadOnlySpan<byte>((void*)dataStart, (int)(dataEnd - dataStart));
        int name = data.IndexOf(Encoding.ASCII.GetBytes(typeName + "\0"));
        if (name < 0x10)
            return 0;
        int typeDescriptor = (int)(dataStart + name - 0x10 - image);

        var rdata = new ReadOnlySpan<byte>((void*)rdataStart, (int)(rdataEnd - rdataStart));
        for (int i = 0; i + 24 <= rdata.Length; i += 4)
        {
            int* col = (int*)(rdataStart + i);
            if (col[3] != typeDescriptor || col[0] != 1 || col[1] != 0 || col[5] != (int)(rdataStart + i - image))
                continue;

            long pointer = (long)(rdataStart + i);
            for (int j = 0; j + 16 <= rdata.Length; j += 8)
            {
                if (*(long*)(rdataStart + j) == pointer)
                    return rdataStart + j + 8;
            }
        }
        return 0;
    }

    // Returns the global read by the mov reg, [rip + disp32] at load whose opcode bytes are mov, or null.
    public static nint* RipGlobal(byte* load, ReadOnlySpan<byte> mov)
    {
        if (!new ReadOnlySpan<byte>(load, mov.Length).SequenceEqual(mov))
            return null;
        return (nint*)(load + mov.Length + 4 + *(int*)(load + mov.Length));
    }
}
