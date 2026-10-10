using System.Runtime.InteropServices;

namespace gbfr.qol.buildcard.Export;

// Calls COM methods by vtable index.
internal static unsafe class Com
{
    public static readonly Guid ID3D11Texture2D = new("6f15aaf2-d208-4e89-9ab4-489535d34f9c");

    public static nint Method(nint obj, int index) => (*(nint**)obj)[index];

    public static void Release(nint obj)
    {
        if (obj != 0)
            ((delegate* unmanaged<nint, uint>)Method(obj, 2))(obj);
    }

    public static void Check(int hr, string call)
    {
        if (hr < 0)
            throw new COMException(call, hr);
    }
}
