using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;

using gbfr.qol.buildcard.Interop.D3D11;
using gbfr.qol.buildcard.Interop.Game;

using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace gbfr.qol.buildcard.Core;

// Hooks the game's present of its swapchain.
public unsafe class SwapChainHooks : IFeature
{
    // gpu::SwapChainImpl's IDXGISwapChain3
    private const int SwapChainOffset = 0x8;

    private readonly IReloadedHooks _hooks;
    private readonly GameApi _api;
    private readonly ILogger _logger;

    // Raised on the render thread before the game presents its swapchain.
    public event Action<SwapChain>? Presenting;

    private delegate nint Present(nint swapChainImpl);
    private IHook<Present>? _presentHook;

    public SwapChainHooks(IReloadedHooks hooks, GameApi api, ILogger logger)
    {
        _hooks = hooks;
        _api = api;
        _logger = logger;
    }

    public Capability[] Requires => [_api.SwapChainImplPresent];

    public bool IsHooked { get; private set; }

    public void Start()
    {
        _presentHook = _hooks.CreateHook<Present>(PresentImpl, _api.SwapChainImplPresent.Address).Activate();
        IsHooked = true;
    }

    private nint PresentImpl(nint swapChainImpl)
    {
        nint swapChain = *(nint*)(swapChainImpl + SwapChainOffset);
        if (swapChain != 0)
            Dispatch.Raise(_logger, nameof(Presenting), Presenting, new SwapChain(swapChain));
        return _presentHook!.OriginalFunction(swapChainImpl);
    }
}
