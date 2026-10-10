using System.Drawing;

using Reloaded.Hooks.Definitions;

using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace gbfr.qol.buildcard.Export;

// Redraws one frame into off-screen frame buffers with a rect of the screen filling a given size.
public unsafe class CardRedraw
{
    // context vtable indices
    private const int DrawIndexed = 12;
    private const int Draw = 13;
    private const int DrawIndexedInstanced = 20;
    private const int DrawInstanced = 21;
    private const int SetRenderTargets = 33;
    private const int SetRenderTargetsAndUavs = 34;
    private const int SetViewports = 44;
    private const int SetScissorRects = 45;
    private const int ClearRenderTargetView = 50;
    private const int GetViewports = 95;
    private const int GetScissorRects = 96;

    // other vtable indices
    private const int SwapChainGetBuffer = 9;
    private const int ChildGetDevice = 3;
    private const int ResourceGetType = 7;
    private const int ViewGetResource = 7;
    private const int ViewGetDesc = 8;
    private const int TextureGetDesc = 10;
    private const int DeviceCreateTexture2D = 5;
    private const int DeviceCreateRenderTargetView = 9;
    private const int DeviceGetImmediateContext = 40;

    private const int DimensionTexture2D = 3;
    private const uint BindRenderTarget = 0x20;
    private const uint ViewTexture2D = 4;
    private const int MaxTargets = 8;
    private const int MaxRects = 16;

    // frame buffers kept at once
    private const int MaxFrameBuffers = 6;

    private readonly IReloadedHooks _hooks;
    private bool _redrawing;
    private nint _device, _context, _backbuffer;
    private uint _width, _height;
    private uint _outWidth, _outHeight;
    private float _scaleX, _scaleY, _offsetX, _offsetY;
    private nint _bound, _lastDrawn, _final;
    private FrameBuffer? _boundFrameBuffer;
    private readonly Dictionary<nint, FrameBuffer> _frameBuffers = [];
    private readonly HashSet<nint> _skipped = [];
    private long _draws;
    private uint _boundCount;
    private readonly nint[] _boundViews = new nint[MaxTargets];

    public string Stats { get; private set; } = "";

    private sealed class FrameBuffer
    {
        public nint Texture, View;
        public uint Format;
        public long LastDraw;
        public int Draws;
    }

    private delegate void DrawIndexedFn(nint context, uint indexCount, uint startIndex, int baseVertex);
    private delegate void DrawFn(nint context, uint vertexCount, uint startVertex);
    private delegate void DrawIndexedInstancedFn(nint context, uint indexCount, uint instanceCount, uint startIndex,
        int baseVertex, uint startInstance);
    private delegate void DrawInstancedFn(nint context, uint vertexCount, uint instanceCount, uint startVertex,
        uint startInstance);
    private delegate void SetRenderTargetsFn(nint context, uint count, nint* views, nint depth);
    private delegate void SetRenderTargetsAndUavsFn(nint context, uint count, nint* views, nint depth, uint uavStart,
        uint uavCount, nint uavs, nint initialCounts);
    private IHook<DrawIndexedFn>? _drawIndexedHook;
    private IHook<DrawFn>? _drawHook;
    private IHook<DrawIndexedInstancedFn>? _drawIndexedInstancedHook;
    private IHook<DrawInstancedFn>? _drawInstancedHook;
    private IHook<SetRenderTargetsFn>? _setTargetsHook;
    private IHook<SetRenderTargetsAndUavsFn>? _setTargetsAndUavsHook;

    public CardRedraw(IReloadedHooks hooks)
    {
        _hooks = hooks;
    }

    // On the render thread: hooks the immediate context's draws and render target binds, read from the swapchain.
    public void Init(nint swapChain)
    {
        if (_setTargetsHook != null)
            return;
        nint backbuffer = Backbuffer(swapChain), device = 0, context = 0;
        ((delegate* unmanaged<nint, nint*, void>)Com.Method(backbuffer, ChildGetDevice))(backbuffer, &device);
        ((delegate* unmanaged<nint, nint*, void>)Com.Method(device, DeviceGetImmediateContext))(device, &context);
        nint drawIndexed = Com.Method(context, DrawIndexed), draw = Com.Method(context, Draw),
            drawIndexedInstanced = Com.Method(context, DrawIndexedInstanced),
            drawInstanced = Com.Method(context, DrawInstanced), setTargets = Com.Method(context, SetRenderTargets),
            setTargetsAndUavs = Com.Method(context, SetRenderTargetsAndUavs);
        Com.Release(context);
        Com.Release(device);
        Com.Release(backbuffer);

        _drawIndexedHook = _hooks.CreateHook<DrawIndexedFn>(DrawIndexedImpl, drawIndexed).Activate();
        _drawHook = _hooks.CreateHook<DrawFn>(DrawImpl, draw).Activate();
        _drawIndexedInstancedHook = _hooks.CreateHook<DrawIndexedInstancedFn>(DrawIndexedInstancedImpl, drawIndexedInstanced).Activate();
        _drawInstancedHook = _hooks.CreateHook<DrawInstancedFn>(DrawInstancedImpl, drawInstanced).Activate();
        _setTargetsHook = _hooks.CreateHook<SetRenderTargetsFn>(SetTargetsImpl, setTargets).Activate();
        _setTargetsAndUavsHook = _hooks.CreateHook<SetRenderTargetsAndUavsFn>(SetTargetsAndUavsImpl, setTargetsAndUavs).Activate();
    }

    // On the render thread at a Present: redraws the next frame with rect filling width x height.
    public void Begin(nint swapChain, Func<int, int, RectangleF> rect, int width, int height)
    {
        Release();
        _backbuffer = Backbuffer(swapChain);
        nint device = 0, context = 0;
        ((delegate* unmanaged<nint, nint*, void>)Com.Method(_backbuffer, ChildGetDevice))(_backbuffer, &device);
        ((delegate* unmanaged<nint, nint*, void>)Com.Method(device, DeviceGetImmediateContext))(device, &context);
        _device = device;
        _context = context;

        uint* desc = stackalloc uint[11];
        ((delegate* unmanaged<nint, uint*, void>)Com.Method(_backbuffer, TextureGetDesc))(_backbuffer, desc);
        _width = desc[0];
        _height = desc[1];
        RectangleF source = rect((int)_width, (int)_height);
        _outWidth = (uint)width;
        _outHeight = (uint)height;
        _scaleX = width / source.Width;
        _scaleY = height / source.Height;
        _offsetX = -source.X * _scaleX;
        _offsetY = -source.Y * _scaleY;
        _bound = _lastDrawn = _final = 0;
        _boundFrameBuffer = null;
        _draws = 0;
        _redrawing = true;
    }

    // On the render thread at the next Present: the UI's frame buffer as rows of 8-bit RGB, or null.
    public byte[]? End()
    {
        _redrawing = false;
        try
        {
            if (_final == 0 || !_frameBuffers.TryGetValue(_final, out var frameBuffer))
            {
                Stats = $"no UI frame buffer, {_frameBuffers.Count} frame buffers";
                return null;
            }
            Stats = $"format {frameBuffer.Format} scale {_scaleX}x{_scaleY} offset {_offsetX},{_offsetY} frame buffers {_frameBuffers.Count} UI draws {frameBuffer.Draws}";
            return BackbufferReadback.ReadTexture(_device, _context, frameBuffer.Texture, frameBuffer.Format,
                new Rectangle(0, 0, (int)_outWidth, (int)_outHeight));
        }
        finally
        {
            Release();
        }
    }

    private void Release()
    {
        foreach (var frameBuffer in _frameBuffers.Values)
            ReleaseFrameBuffer(frameBuffer);
        _frameBuffers.Clear();
        _skipped.Clear();
        Com.Release(_backbuffer);
        Com.Release(_context);
        Com.Release(_device);
        _backbuffer = _context = _device = 0;
    }

    private static void ReleaseFrameBuffer(FrameBuffer frameBuffer)
    {
        Com.Release(frameBuffer.View);
        Com.Release(frameBuffer.Texture);
    }

    private static nint Backbuffer(nint swapChain)
    {
        Guid iid = Com.ID3D11Texture2D;
        nint backbuffer = 0;
        Com.Check(((delegate* unmanaged<nint, uint, Guid*, nint*, int>)Com.Method(swapChain, SwapChainGetBuffer))(
            swapChain, 0, &iid, &backbuffer), "GetBuffer");
        return backbuffer;
    }

    private void SetTargetsImpl(nint context, uint count, nint* views, nint depth)
    {
        if (_redrawing)
            OnBind(count, views, depth, true);
        _setTargetsHook!.OriginalFunction(context, count, views, depth);
    }

    private void SetTargetsAndUavsImpl(nint context, uint count, nint* views, nint depth, uint uavStart, uint uavCount,
        nint uavs, nint initialCounts)
    {
        // count -1 keeps the bound render targets
        if (_redrawing && count != 0xFFFFFFFF)
            OnBind(count, views, depth, false);
        _setTargetsAndUavsHook!.OriginalFunction(context, count, views, depth, uavStart, uavCount, uavs, initialCounts);
    }

    // Tracks the bound texture and its frame buffer.
    private void OnBind(uint count, nint* views, nint depth, bool plain)
    {
        nint view = count > 0 && views != null ? views[0] : 0;
        bool single = view != 0 && depth == 0 && plain && count <= MaxTargets;
        for (int i = 1; single && i < count; i++)
            single = views[i] == 0;
        _bound = view == 0 ? 0 : Resource(view);
        _boundFrameBuffer = single && _bound != _backbuffer ? FrameBufferFor(_bound, view) : null;
        if (_boundFrameBuffer == null)
            return;
        _boundCount = count;
        for (int i = 0; i < count; i++)
            _boundViews[i] = views[i];
    }

    // The frame buffer of a backbuffer-sized texture, made on its first bind.
    private FrameBuffer? FrameBufferFor(nint texture, nint view)
    {
        if (_frameBuffers.TryGetValue(texture, out var frameBuffer))
            return frameBuffer;
        if (_skipped.Contains(texture))
            return null;

        int type = 0;
        ((delegate* unmanaged<nint, int*, void>)Com.Method(texture, ResourceGetType))(texture, &type);
        uint* desc = stackalloc uint[11];
        if (type == DimensionTexture2D)
            ((delegate* unmanaged<nint, uint*, void>)Com.Method(texture, TextureGetDesc))(texture, desc);
        if (type != DimensionTexture2D || desc[0] != _width || desc[1] != _height || desc[5] != 1)
        {
            _skipped.Add(texture);
            return null;
        }

        if (_frameBuffers.Count >= MaxFrameBuffers)
        {
            var oldest = _frameBuffers.MinBy(c => c.Value.LastDraw);
            ReleaseFrameBuffer(oldest.Value);
            _frameBuffers.Remove(oldest.Key);
        }

        uint* viewDesc = stackalloc uint[5];
        ((delegate* unmanaged<nint, uint*, void>)Com.Method(view, ViewGetDesc))(view, viewDesc);
        frameBuffer = new FrameBuffer { Format = viewDesc[0], LastDraw = _draws };
        desc[0] = _outWidth;
        desc[1] = _outHeight;
        desc[2] = 1;
        desc[3] = 1;
        desc[6] = 0;
        desc[7] = 0;
        desc[8] = BindRenderTarget;
        desc[9] = 0;
        desc[10] = 0;
        nint bufferTexture = 0, bufferView = 0;
        if (((delegate* unmanaged<nint, uint*, void*, nint*, int>)Com.Method(_device, DeviceCreateTexture2D))(
                _device, desc, null, &bufferTexture) < 0)
        {
            _skipped.Add(texture);
            return null;
        }
        uint* bufferViewDesc = stackalloc uint[5];
        bufferViewDesc[0] = frameBuffer.Format;
        bufferViewDesc[1] = ViewTexture2D;
        bufferViewDesc[2] = bufferViewDesc[3] = bufferViewDesc[4] = 0;
        if (((delegate* unmanaged<nint, nint, uint*, nint*, int>)Com.Method(_device, DeviceCreateRenderTargetView))(
                _device, bufferTexture, bufferViewDesc, &bufferView) < 0)
        {
            Com.Release(bufferTexture);
            _skipped.Add(texture);
            return null;
        }
        frameBuffer.Texture = bufferTexture;
        frameBuffer.View = bufferView;
        float* black = stackalloc float[4];
        ((delegate* unmanaged<nint, nint, float*, void>)Com.Method(_context, ClearRenderTargetView))(_context, bufferView, black);
        _frameBuffers[texture] = frameBuffer;
        return frameBuffer;
    }

    private static nint Resource(nint view)
    {
        nint resource = 0;
        ((delegate* unmanaged<nint, nint*, void>)Com.Method(view, ViewGetResource))(view, &resource);
        Com.Release(resource);
        return resource;
    }

    // Notes the UI's texture; returns the bound frame buffer to redraw into.
    private FrameBuffer? BeforeDraw()
    {
        if (!_redrawing)
            return null;
        _draws++;
        if (_bound == _backbuffer && _final == 0)
            _final = _lastDrawn;
        if (_boundFrameBuffer == null)
            return null;
        _lastDrawn = _bound;
        _boundFrameBuffer.LastDraw = _draws;
        _boundFrameBuffer.Draws++;
        return _boundFrameBuffer;
    }

    // Repeats a draw into a frame buffer with the viewports and scissor rects scaled.
    private void Redraw(FrameBuffer frameBuffer, Action draw)
    {
        nint context = _context;
        uint viewportCount = MaxRects, rectCount = MaxRects;
        float* viewports = stackalloc float[6 * MaxRects];
        int* rects = stackalloc int[4 * MaxRects];
        ((delegate* unmanaged<nint, uint*, float*, void>)Com.Method(context, GetViewports))(context, &viewportCount, viewports);
        ((delegate* unmanaged<nint, uint*, int*, void>)Com.Method(context, GetScissorRects))(context, &rectCount, rects);

        float* scaledViewports = stackalloc float[6 * MaxRects];
        int* scaledRects = stackalloc int[4 * MaxRects];
        for (int i = 0; i < viewportCount; i++)
        {
            float* v = viewports + i * 6, s = scaledViewports + i * 6;
            s[0] = v[0] * _scaleX + _offsetX;
            s[1] = v[1] * _scaleY + _offsetY;
            s[2] = v[2] * _scaleX;
            s[3] = v[3] * _scaleY;
            s[4] = v[4];
            s[5] = v[5];
        }
        for (int i = 0; i < rectCount; i++)
        {
            int* r = rects + i * 4, s = scaledRects + i * 4;
            s[0] = (int)MathF.Floor(r[0] * _scaleX + _offsetX);
            s[1] = (int)MathF.Floor(r[1] * _scaleY + _offsetY);
            s[2] = (int)MathF.Ceiling(r[2] * _scaleX + _offsetX);
            s[3] = (int)MathF.Ceiling(r[3] * _scaleY + _offsetY);
        }

        nint view = frameBuffer.View;
        _setTargetsHook!.OriginalFunction(context, 1, &view, 0);
        ((delegate* unmanaged<nint, uint, float*, void>)Com.Method(context, SetViewports))(context, viewportCount, scaledViewports);
        ((delegate* unmanaged<nint, uint, int*, void>)Com.Method(context, SetScissorRects))(context, rectCount, scaledRects);
        draw();

        fixed (nint* views = _boundViews)
            _setTargetsHook.OriginalFunction(context, _boundCount, views, 0);
        ((delegate* unmanaged<nint, uint, float*, void>)Com.Method(context, SetViewports))(context, viewportCount, viewports);
        ((delegate* unmanaged<nint, uint, int*, void>)Com.Method(context, SetScissorRects))(context, rectCount, rects);
    }

    private void DrawIndexedImpl(nint context, uint indexCount, uint startIndex, int baseVertex)
    {
        var frameBuffer = BeforeDraw();
        _drawIndexedHook!.OriginalFunction(context, indexCount, startIndex, baseVertex);
        if (frameBuffer != null)
            Redraw(frameBuffer, () => _drawIndexedHook.OriginalFunction(context, indexCount, startIndex, baseVertex));
    }

    private void DrawImpl(nint context, uint vertexCount, uint startVertex)
    {
        var frameBuffer = BeforeDraw();
        _drawHook!.OriginalFunction(context, vertexCount, startVertex);
        if (frameBuffer != null)
            Redraw(frameBuffer, () => _drawHook.OriginalFunction(context, vertexCount, startVertex));
    }

    private void DrawIndexedInstancedImpl(nint context, uint indexCount, uint instanceCount, uint startIndex,
        int baseVertex, uint startInstance)
    {
        var frameBuffer = BeforeDraw();
        _drawIndexedInstancedHook!.OriginalFunction(context, indexCount, instanceCount, startIndex, baseVertex, startInstance);
        if (frameBuffer != null)
            Redraw(frameBuffer, () => _drawIndexedInstancedHook.OriginalFunction(context, indexCount, instanceCount, startIndex,
                baseVertex, startInstance));
    }

    private void DrawInstancedImpl(nint context, uint vertexCount, uint instanceCount, uint startVertex,
        uint startInstance)
    {
        var frameBuffer = BeforeDraw();
        _drawInstancedHook!.OriginalFunction(context, vertexCount, instanceCount, startVertex, startInstance);
        if (frameBuffer != null)
            Redraw(frameBuffer, () => _drawInstancedHook.OriginalFunction(context, vertexCount, instanceCount, startVertex, startInstance));
    }
}
