using System.Runtime.InteropServices;

using gbfr.qol.buildcard.Game;
using gbfr.qol.buildcard.Interop.Game;

namespace gbfr.qol.buildcard.Tests;

public unsafe class UiTreeTests
{
    private const int ObjectSize = 0x1D8;
    private const int NameHash = 0x1C4;

    // tools/build/tests/test_codegen.py asserts the same value
    [Fact]
    public void PrefabHashMatchesTheBuild()
    {
        uint[] names = [0xD54E236E, 0xE2ED9F09];  // status01, a
        byte* memory = (byte*)NativeMemory.AllocZeroed((nuint)(ObjectSize * names.Length));
        try
        {
            var objects = new Dictionary<int, UiObject>();
            for (int id = 0; id < names.Length; id++)
            {
                byte* obj = memory + id * ObjectSize;
                *(uint*)(obj + NameHash) = names[id];
                objects[id] = new UiObject((nint)obj);
            }

            Assert.Equal(0xBB11DC59u, UiTree.PrefabHash(objects));
            objects.Remove(0);
            objects[2] = new UiObject((nint)memory);
            Assert.Equal(0u, UiTree.PrefabHash(objects));
        }
        finally
        {
            NativeMemory.Free(memory);
        }
    }
}
