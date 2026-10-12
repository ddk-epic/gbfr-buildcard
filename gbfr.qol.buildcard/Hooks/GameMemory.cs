using System.Runtime.InteropServices;

namespace gbfr.qol.buildcard.Hooks;

// Finds the allocation an address is in.
public static unsafe partial class GameMemory
{
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

// An MSVC vector of T.
[StructLayout(LayoutKind.Sequential)]
public unsafe struct GameVector<T> where T : unmanaged
{
    public T* Begin;
    public T* End;
    public T* Capacity;

    // Whether it holds whole elements
    public readonly bool IsValid => (Begin != null || Capacity == null) && Begin <= End && End <= Capacity
        && ((nint)End - (nint)Begin) % sizeof(T) == 0 && ((nint)Capacity - (nint)Begin) % sizeof(T) == 0;

    public readonly int Count => (int)(End - Begin);

    public readonly ref T this[int index] => ref Begin[index];
}

// An MSVC unordered map's node list: the sentinel node and the size.
[StructLayout(LayoutKind.Sequential)]
public unsafe struct GameMap
{
    public MapNode* Head;
    public long Size;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct MapNode
{
    public MapNode* Next;
    public MapNode* Prev;
    public ulong Key;
    public nint Value;
}
