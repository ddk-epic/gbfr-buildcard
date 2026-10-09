using System.Runtime.InteropServices;

namespace gbfr.qol.buildcard.Hooks;

// Checks that game memory is committed and readable before the mod reads it.
public static unsafe partial class GameMemory
{
    private const uint MemCommit = 0x1000;
    private const uint PageGuard = 0x100;
    private const uint PageReadable = 0x02 | 0x04 | 0x08 | 0x20 | 0x40 | 0x80;  // PAGE_ flags

    public static bool IsReadable(nint address, nint size) => new Region().Covers(address, size);

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

    // The readable region last queried
    public struct Region
    {
        private nint _start, _end;

        // Whether [address, address + size) is committed and readable
        public bool Covers(nint address, nint size)
        {
            nint end = address + size;
            if (address == 0 || size <= 0 || end < address)
                return false;
            while (true)
            {
                if ((address < _start || address >= _end) && !Query(address))
                    return false;
                if (end <= _end)
                    return true;
                address = _end;
            }
        }

        private bool Query(nint address)
        {
            _start = _end = 0;
            if (VirtualQuery(address, out var info, (nuint)sizeof(MemoryBasicInformation)) == 0
                || info.State != MemCommit || (info.Protect & PageGuard) != 0 || (info.Protect & PageReadable) == 0)
                return false;
            _start = info.BaseAddress;
            _end = info.BaseAddress + info.RegionSize;
            return true;
        }
    }

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
