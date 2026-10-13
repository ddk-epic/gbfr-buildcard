using System.Runtime.InteropServices;

namespace gbfr.qol.buildcard.Interop.Native;

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
    private const long MaxSize = 0x100000;

    public MapNode* Head;
    public long Size;

    // Every node's key and value in list order; null when the list is broken
    public readonly List<(ulong Key, nint Value)>? Read()
    {
        var head = Head;
        if (Size < 0 || Size > MaxSize || head == null)
            return null;
        var entries = new List<(ulong, nint)>((int)Size);
        var prev = head;
        for (var node = head->Next; node != head; prev = node, node = node->Next)
        {
            if (entries.Count == Size || node == null || node->Prev != prev)
                return null;
            entries.Add((node->Key, node->Value));
        }
        return entries.Count == Size ? entries : null;
    }

    // The values by the keys' low 32 bits, the first of each; null when the list is broken
    public readonly Dictionary<uint, nint>? ReadByUintKey()
    {
        if (Read() is not { } entries)
            return null;
        var map = new Dictionary<uint, nint>(entries.Count);
        foreach (var (key, value) in entries)
            map.TryAdd((uint)key, value);
        return map;
    }
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct MapNode
{
    public MapNode* Next;
    public MapNode* Prev;
    public ulong Key;
    public nint Value;
}
