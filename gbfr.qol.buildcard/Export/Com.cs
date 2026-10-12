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

// A DXGI swapchain the game presents.
public readonly unsafe record struct SwapChain(nint Address)
{
    private const int GetBufferIndex = 9;

    // GetBuffer of buffer 0 as an ID3D11Texture2D
    internal int GetBackbuffer(out nint texture)
    {
        Guid iid = Com.ID3D11Texture2D;
        nint buffer = 0;
        int hr = ((delegate* unmanaged<nint, uint, Guid*, nint*, int>)Com.Method(Address, GetBufferIndex))(
            Address, 0, &iid, &buffer);
        texture = buffer;
        return hr;
    }
}

// D3D11_TEXTURE2D_DESC
[StructLayout(LayoutKind.Sequential)]
internal struct TextureDesc
{
    public uint Width;
    public uint Height;
    public uint MipLevels;
    public uint ArraySize;
    public uint Format;
    public uint SampleCount;
    public uint SampleQuality;
    public uint Usage;
    public uint BindFlags;
    public uint CpuAccessFlags;
    public uint MiscFlags;
}

// D3D11_RENDER_TARGET_VIEW_DESC, its union as Texture2DArray's
[StructLayout(LayoutKind.Sequential)]
internal struct RenderTargetViewDesc
{
    public uint Format;
    public uint ViewDimension;
    public uint MipSlice;
    public uint FirstArraySlice;
    public uint ArraySize;
}

// D3D11_VIEWPORT
[StructLayout(LayoutKind.Sequential)]
internal struct Viewport
{
    public float TopLeftX;
    public float TopLeftY;
    public float Width;
    public float Height;
    public float MinDepth;
    public float MaxDepth;
}

// D3D11_RECT
[StructLayout(LayoutKind.Sequential)]
internal struct ScissorRect
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}
