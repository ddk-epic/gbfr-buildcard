using System.Runtime.InteropServices;

namespace gbfr.qol.buildcard.Hooks;

// Checks the layout of game structures and finds the allocation an address is in.
public static unsafe partial class GameMemory
{
    // Whether the MSVC vector at vector (begin, end, capacity) holds whole elements of elementSize
    public static bool IsVector(nint vector, int elementSize)
    {
        nint* v = (nint*)vector;
        return (v[0] != 0 || v[2] == 0) && v[0] <= v[1] && v[1] <= v[2]
            && (v[1] - v[0]) % elementSize == 0 && (v[2] - v[0]) % elementSize == 0;
    }

    // The base of the allocation holding address, a module's base for an address in it; 0 when unmapped
    public static nint AllocationBase(nint address) =>
        VirtualQuery(address, out var info, (nuint)sizeof(MemoryBasicInformation)) == 0 ? 0 : info.AllocationBase;

    [LibraryImport("kernel32.dll")]
    private static partial nuint VirtualQuery(nint address, out MemoryBasicInformation info, nuint length);

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryBasicInformation
    {
        public nint BaseAddress;
        public nint AllocationBase;
        public uint AllocationProtect;
        public ushort PartitionId;
        public nint RegionSize;
        public uint State;
        public uint Protect;
        public uint Type;
    }
}
