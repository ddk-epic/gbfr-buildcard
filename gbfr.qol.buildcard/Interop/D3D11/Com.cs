using System.Runtime.InteropServices;

namespace gbfr.qol.buildcard.Interop.D3D11;

// Calls COM methods by vtable index.
internal static unsafe class Com
{
    // vtable indices by interface
    public static class IUnknown
    {
        public const int Release = 2;
    }

    public static class ID3D11DeviceChild
    {
        public const int GetDevice = 3;
    }

    public static class ID3D11Resource
    {
        public new const int GetType = 7;
    }

    public static class ID3D11View
    {
        public const int GetResource = 7;
        public const int GetDesc = 8;
    }

    public static class ID3D11Texture2D
    {
        public static readonly Guid Iid = new("6f15aaf2-d208-4e89-9ab4-489535d34f9c");
        public const int GetDesc = 10;
    }

    public static class ID3D11Device
    {
        public const int CreateTexture2D = 5;
        public const int CreateRenderTargetView = 9;
        public const int GetImmediateContext = 40;
    }

    public static class ID3D11DeviceContext
    {
        public const int DrawIndexed = 12;
        public const int Draw = 13;
        public const int Map = 14;
        public const int Unmap = 15;
        public const int DrawIndexedInstanced = 20;
        public const int DrawInstanced = 21;
        public const int OMSetRenderTargets = 33;
        public const int OMSetRenderTargetsAndUnorderedAccessViews = 34;
        public const int RSSetViewports = 44;
        public const int RSSetScissorRects = 45;
        public const int CopySubresourceRegion = 46;
        public const int ClearRenderTargetView = 50;
        public const int ResolveSubresource = 57;
        public const int RSGetViewports = 95;
        public const int RSGetScissorRects = 96;
    }

    public static class IDXGISwapChain
    {
        public const int GetBuffer = 9;
    }

    public static nint Method(nint obj, int index) => (*(nint**)obj)[index];

    public static void Release(nint obj)
    {
        if (obj != 0)
            ((delegate* unmanaged<nint, uint>)Method(obj, IUnknown.Release))(obj);
    }

    public static void Check(int hr, string call)
    {
        if (hr < 0)
            throw new COMException(call, hr);
    }

    // The device of a device child, referenced
    public static nint Device(nint child)
    {
        nint device = 0;
        ((delegate* unmanaged<nint, nint*, void>)Method(child, ID3D11DeviceChild.GetDevice))(child, &device);
        return device;
    }

    // The device's immediate context, referenced
    public static nint ImmediateContext(nint device)
    {
        nint context = 0;
        ((delegate* unmanaged<nint, nint*, void>)Method(device, ID3D11Device.GetImmediateContext))(device, &context);
        return context;
    }

    public static TextureDesc GetTextureDesc(nint texture)
    {
        TextureDesc desc;
        ((delegate* unmanaged<nint, TextureDesc*, void>)Method(texture, ID3D11Texture2D.GetDesc))(texture, &desc);
        return desc;
    }

    public static void CreateTexture(nint device, TextureDesc* desc, nint* texture)
    {
        Check(((delegate* unmanaged<nint, TextureDesc*, void*, nint*, int>)Method(device, ID3D11Device.CreateTexture2D))(
            device, desc, null, texture), "CreateTexture2D");
    }
}

// A DXGI swapchain the game presents.
public readonly unsafe record struct SwapChain(nint Address)
{
    // GetBuffer of buffer 0 as an ID3D11Texture2D
    internal int GetBackbuffer(out nint texture)
    {
        Guid iid = Com.ID3D11Texture2D.Iid;
        nint buffer = 0;
        int hr = ((delegate* unmanaged<nint, uint, Guid*, nint*, int>)Com.Method(Address, Com.IDXGISwapChain.GetBuffer))(
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
