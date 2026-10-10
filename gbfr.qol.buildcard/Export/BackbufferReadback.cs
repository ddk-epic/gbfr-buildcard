using System.Drawing;
using System.Runtime.InteropServices;

namespace gbfr.qol.buildcard.Export;

// Copies a rect of the backbuffer or a texture to the CPU through the game's D3D11 immediate context.
public static unsafe class BackbufferReadback
{
    // vtable indices
    private const int SwapChainGetBuffer = 9;
    private const int ChildGetDevice = 3;
    private const int TextureGetDesc = 10;
    private const int DeviceCreateTexture2D = 5;
    private const int DeviceGetImmediateContext = 40;
    private const int ContextMap = 14;
    private const int ContextUnmap = 15;
    private const int ContextCopySubresourceRegion = 46;
    private const int ContextResolveSubresource = 57;

    // DXGI formats
    private const uint R10G10B10A2 = 24;
    private const uint R8G8B8A8Typeless = 27;
    private const uint R8G8B8A8 = 28;
    private const uint R8G8B8A8Srgb = 29;
    private const uint B8G8R8A8 = 87;
    private const uint B8G8R8A8Srgb = 91;

    private const uint UsageDefault = 0;
    private const uint UsageStaging = 3;
    private const uint CpuAccessRead = 0x20000;
    private const uint MapRead = 1;

    // On the render thread: returns the rect crop picks from the backbuffer's width and height, as rows of 8-bit RGB.
    public static byte[] Read(nint swapChain, Func<int, int, Rectangle> crop, out int width, out int height)
    {
        nint backbuffer = 0, device = 0, context = 0;
        try
        {
            Guid iid = Com.ID3D11Texture2D;
            Com.Check(((delegate* unmanaged<nint, uint, Guid*, nint*, int>)Com.Method(swapChain, SwapChainGetBuffer))(
                swapChain, 0, &iid, &backbuffer), "GetBuffer");

            TextureDesc desc;
            ((delegate* unmanaged<nint, TextureDesc*, void>)Com.Method(backbuffer, TextureGetDesc))(backbuffer, &desc);
            uint format = desc.Format;
            if (format is not (R10G10B10A2 or R8G8B8A8 or R8G8B8A8Srgb or B8G8R8A8 or B8G8R8A8Srgb))
                throw new NotSupportedException($"Backbuffer format {format} not supported");

            Rectangle rect = Rectangle.Intersect(crop((int)desc.Width, (int)desc.Height),
                new Rectangle(0, 0, (int)desc.Width, (int)desc.Height));
            if (rect.Width <= 0 || rect.Height <= 0)
                throw new InvalidOperationException("Crop outside the backbuffer");
            width = rect.Width;
            height = rect.Height;

            ((delegate* unmanaged<nint, nint*, void>)Com.Method(backbuffer, ChildGetDevice))(backbuffer, &device);
            ((delegate* unmanaged<nint, nint*, void>)Com.Method(device, DeviceGetImmediateContext))(device, &context);
            return ReadTexture(device, context, backbuffer, format, rect);
        }
        finally
        {
            Com.Release(context);
            Com.Release(device);
            Com.Release(backbuffer);
        }
    }

    // On the render thread: returns a rect of a 2D texture whose pixels are in format, as rows of 8-bit RGB.
    public static byte[] ReadTexture(nint device, nint context, nint texture, uint format, Rectangle rect)
    {
        nint resolved = 0, staging = 0;
        try
        {
            TextureDesc desc;
            ((delegate* unmanaged<nint, TextureDesc*, void>)Com.Method(texture, TextureGetDesc))(texture, &desc);
            if (format is not (R10G10B10A2 or R8G8B8A8Typeless or R8G8B8A8 or R8G8B8A8Srgb or B8G8R8A8 or B8G8R8A8Srgb))
                throw new NotSupportedException($"Texture format {format} not supported");
            int width = rect.Width, height = rect.Height;

            nint source = texture;
            if (desc.SampleCount > 1)
            {
                TextureDesc resolveDesc = desc;
                resolveDesc.SampleCount = 1;
                resolveDesc.SampleQuality = 0;
                resolveDesc.Usage = UsageDefault;
                resolveDesc.BindFlags = 0;
                resolveDesc.CpuAccessFlags = 0;
                resolveDesc.MiscFlags = 0;
                CreateTexture(device, &resolveDesc, &resolved);
                ((delegate* unmanaged<nint, nint, uint, nint, uint, uint, void>)Com.Method(context, ContextResolveSubresource))(
                    context, resolved, 0, texture, 0, format);
                source = resolved;
            }

            TextureDesc stagingDesc = new()
            {
                Width = (uint)width, Height = (uint)height, MipLevels = 1, ArraySize = 1, Format = desc.Format,
                SampleCount = 1, Usage = UsageStaging, CpuAccessFlags = CpuAccessRead,
            };
            CreateTexture(device, &stagingDesc, &staging);

            Box box = new()
            {
                Left = (uint)rect.Left, Top = (uint)rect.Top, Front = 0,
                Right = (uint)rect.Right, Bottom = (uint)rect.Bottom, Back = 1,
            };
            ((delegate* unmanaged<nint, nint, uint, uint, uint, uint, nint, uint, Box*, void>)
                Com.Method(context, ContextCopySubresourceRegion))(context, staging, 0, 0, 0, 0, source, 0, &box);

            MappedSubresource mapped;
            Com.Check(((delegate* unmanaged<nint, nint, uint, uint, uint, MappedSubresource*, int>)Com.Method(context, ContextMap))(
                context, staging, 0, MapRead, 0, &mapped), "Map");
            try
            {
                return ToRgb((byte*)mapped.Data, mapped.RowPitch, width, height, format);
            }
            finally
            {
                ((delegate* unmanaged<nint, nint, uint, void>)Com.Method(context, ContextUnmap))(context, staging, 0);
            }
        }
        finally
        {
            Com.Release(staging);
            Com.Release(resolved);
        }
    }

    private static void CreateTexture(nint device, TextureDesc* desc, nint* texture)
    {
        Com.Check(((delegate* unmanaged<nint, TextureDesc*, void*, nint*, int>)Com.Method(device, DeviceCreateTexture2D))(
            device, desc, null, texture), "CreateTexture2D");
    }

    private static byte[] ToRgb(byte* data, uint rowPitch, int width, int height, uint format)
    {
        var rgb = new byte[width * height * 3];
        fixed (byte* output = rgb)
        {
            for (int y = 0; y < height; y++)
            {
                byte* src = data + (long)y * rowPitch;
                byte* dst = output + (long)y * width * 3;
                for (int x = 0; x < width; x++, src += 4, dst += 3)
                {
                    switch (format)
                    {
                        case R10G10B10A2:
                            uint v = *(uint*)src;
                            dst[0] = (byte)((v >> 2) & 0xFF);
                            dst[1] = (byte)((v >> 12) & 0xFF);
                            dst[2] = (byte)((v >> 22) & 0xFF);
                            break;
                        case B8G8R8A8 or B8G8R8A8Srgb:
                            dst[0] = src[2];
                            dst[1] = src[1];
                            dst[2] = src[0];
                            break;
                        default:
                            dst[0] = src[0];
                            dst[1] = src[1];
                            dst[2] = src[2];
                            break;
                    }
                }
            }
        }
        return rgb;
    }

    // D3D11_TEXTURE2D_DESC
    [StructLayout(LayoutKind.Sequential)]
    private struct TextureDesc
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

    // D3D11_MAPPED_SUBRESOURCE
    [StructLayout(LayoutKind.Sequential)]
    private struct MappedSubresource
    {
        public nint Data;
        public uint RowPitch;
        public uint DepthPitch;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Box
    {
        public uint Left, Top, Front, Right, Bottom, Back;
    }
}
