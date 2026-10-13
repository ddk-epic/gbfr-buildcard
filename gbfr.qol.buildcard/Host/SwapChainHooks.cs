using System.Drawing;
using System.Runtime.InteropServices;

using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;

using gbfr.qol.buildcard.Interop.D3D11;
using gbfr.qol.buildcard.Interop.Game;

using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace gbfr.qol.buildcard.Host;

// Hooks DXGI's Present, found on a throwaway D3D11 device and swapchain.
public unsafe partial class SwapChainHooks : IFeature
{
    private const int DriverHardware = 1;
    private const uint SdkVersion = 7;

    private const uint PresentTest = 0x1;

    private readonly IReloadedHooks _hooks;
    private readonly ILogger _logger;

    [ThreadStatic]
    private static bool _inPresent;

    // Raised on the render thread before the game presents a swapchain.
    public event Action<SwapChain>? Presenting;

    private delegate int Present(nint swapChain, uint syncInterval, uint flags);
    private delegate int Present1(nint swapChain, uint syncInterval, uint flags, nint parameters);
    private IHook<Present>? _presentHook;
    private IHook<Present1>? _present1Hook;

    public SwapChainHooks(IReloadedHooks hooks, ILogger logger)
    {
        _hooks = hooks;
        _logger = logger;
    }

    // the swapchain's methods are found at start
    public Capability[] Requires => [];

    public void Start()
    {
        nint present, present1;
        try
        {
            FindMethods(out present, out present1);
        }
        catch (Exception e)
        {
            _logger.WriteLine($"[gbfr.qol.buildcard] Swapchain methods not found: {e.Message}", Color.Red);
            return;
        }
        _presentHook = _hooks.CreateHook<Present>(PresentImpl, present).Activate();
        _present1Hook = _hooks.CreateHook<Present1>(Present1Impl, present1).Activate();
        _logger.WriteLine("[gbfr.qol.buildcard] Swapchain hooked");
    }

    // Reads the methods from the vtable of a swapchain on a hidden window.
    private static void FindMethods(out nint present, out nint present1)
    {
        var createDevice = (delegate* unmanaged<nint, int, nint, uint, uint*, uint, uint, SwapChainDesc*, nint*, nint*, uint*, nint*, int>)
            NativeLibrary.GetExport(NativeLibrary.Load("d3d11.dll"), "D3D11CreateDeviceAndSwapChain");

        nint window = CreateWindowExW(0, "STATIC", "", 0, 0, 0, 64, 64, 0, 0, 0, 0);
        nint swapChain = 0, device = 0, context = 0;
        try
        {
            if (window == 0)
                throw new InvalidOperationException("CreateWindowExW failed");
            SwapChainDesc desc = default;
            desc.Width = 64;
            desc.Height = 64;
            desc.Format = 28;
            desc.SampleCount = 1;
            desc.BufferUsage = 0x20;
            desc.BufferCount = 2;
            desc.OutputWindow = window;
            desc.Windowed = 1;
            desc.SwapEffect = 4;
            uint level;
            Com.Check(createDevice(0, DriverHardware, 0, 0, null, 0, SdkVersion, &desc, &swapChain, &device, &level, &context),
                "D3D11CreateDeviceAndSwapChain");

            present = Com.Method(swapChain, Com.IDXGISwapChain.Present);
            present1 = Com.Method(swapChain, Com.IDXGISwapChain.Present1);
        }
        finally
        {
            Com.Release(context);
            Com.Release(device);
            Com.Release(swapChain);
            if (window != 0)
                DestroyWindow(window);
        }
    }

    private int PresentImpl(nint swapChain, uint syncInterval, uint flags)
    {
        if (_inPresent)
            return _presentHook!.OriginalFunction(swapChain, syncInterval, flags);
        _inPresent = true;
        try
        {
            OnPresenting(swapChain, flags);
            return _presentHook!.OriginalFunction(swapChain, syncInterval, flags);
        }
        finally
        {
            _inPresent = false;
        }
    }

    private int Present1Impl(nint swapChain, uint syncInterval, uint flags, nint parameters)
    {
        if (_inPresent)
            return _present1Hook!.OriginalFunction(swapChain, syncInterval, flags, parameters);
        _inPresent = true;
        try
        {
            OnPresenting(swapChain, flags);
            return _present1Hook!.OriginalFunction(swapChain, syncInterval, flags, parameters);
        }
        finally
        {
            _inPresent = false;
        }
    }

    private void OnPresenting(nint swapChain, uint flags)
    {
        if ((flags & PresentTest) == 0)
            Dispatch.Raise(_logger, nameof(Presenting), Presenting, new SwapChain(swapChain));
    }

    // DXGI_SWAP_CHAIN_DESC
    [StructLayout(LayoutKind.Sequential)]
    private struct SwapChainDesc
    {
        public uint Width;
        public uint Height;
        public uint RefreshNumerator;
        public uint RefreshDenominator;
        public uint Format;
        public uint ScanlineOrdering;
        public uint Scaling;
        public uint SampleCount;
        public uint SampleQuality;
        public uint BufferUsage;
        public uint BufferCount;
        public nint OutputWindow;
        public int Windowed;
        public uint SwapEffect;
        public uint Flags;
    }

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial nint CreateWindowExW(uint exStyle, string className, string windowName, uint style,
        int x, int y, int width, int height, nint parent, nint menu, nint instance, nint param);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DestroyWindow(nint window);
}
