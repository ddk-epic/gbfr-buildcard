using System.Drawing;

using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;

using gbfr.qol.buildcard.Host;
using gbfr.qol.buildcard.Interop.Game;
using gbfr.qol.buildcard.Interop.Native;

using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace gbfr.qol.buildcard.Game;

// Reads Save Card's button and raises ShortcutUpdated at each shortcut update.
public unsafe class Input : IFeature
{
    // offsets in the GetButtonBits signature
    private const int InputLoad = 0x4;
    private const int MaskCall = 0x1A;

    // input manager offsets
    private const int InputContext = 0xA8;
    private const int HeldButtons = 0x84;
    private const int HeldMenuKeys = 0x0C;
    private const int InputLock = 0xB0;

    // keyboard 1 in the held menu keys
    private const uint MenuKey = 0x4000;

    // Shortcut component
    private const int UpdateableBase = 0x68;
    private const int ComponentObject = 0x10;
    private const int InputFlags = 0x20;
    private const int InputBlocked = 0x24;
    private const int ButtonType = 0x70;

    // input flags
    private const uint InputEnabled = 0x1;
    private const uint InputButtons = 0x4;

    // the mode a Shortcut without a context passes
    private const uint DefaultMode = 3;

    private readonly IReloadedHooks _hooks;
    private readonly GameApi _api;
    private readonly ILogger _logger;
    private nint* _input;
    private delegate* unmanaged<uint, int, byte, uint, uint> _getButtonBits;
    private bool _held;
    private bool _enabled;

    // Raised on the game thread for each shortcut update.
    public event Action? ShortcutUpdated;

    private delegate void UpdateShortcutInput(nint shortcut);
    private IHook<UpdateShortcutInput>? _updateHook;

    public Input(IReloadedHooks hooks, GameApi api, ILogger logger)
    {
        _hooks = hooks;
        _api = api;
        _logger = logger;
    }

    public Capability[] Requires => [_api.UpdateShortcutInput, _api.GetButtonBits];

    // GetButtonBits(input context, button, 1, mode)
    public void Start()
    {
        _updateHook = _hooks.CreateHook<UpdateShortcutInput>(UpdateImpl, _api.UpdateShortcutInput.Address).Activate();
        byte* bits = (byte*)_api.GetButtonBits.Address;
        byte* call = bits + MaskCall;
        if (*call != 0xE8)
            return;
        _input = PeImage.RipGlobal(bits + InputLoad, [0x4C, 0x8B, 0x3D]);
        _getButtonBits = (delegate* unmanaged<uint, int, byte, uint, uint>)(call + 5 + *(int*)(call + 1));
    }

    // Whether the button went down since the last call while status01's shortcut takes input.
    public bool Pressed()
    {
        if (_input == null || *_input == 0 || _getButtonBits == null)
            return false;
        nint input = *_input;
        uint mask = _getButtonBits(*(uint*)(input + InputContext), CardLayout.Footer.SaveCardButton, 1, DefaultMode);
        bool down = *(int*)(input + InputLock) < 1
            && ((*(uint*)(input + HeldButtons) & mask) != 0 || (*(uint*)(input + HeldMenuKeys) & MenuKey) != 0);
        bool pressed = down && !_held;
        _held = down;
        return pressed && _enabled;
    }

    private void UpdateImpl(nint shortcut)
    {
        nint component = shortcut - UpdateableBase;
        try
        {
            if (*(int*)(component + ButtonType) == CardLayout.Footer.SaveCardButton && OnStatus01(component))
                _enabled = TakesInput(component);
        }
        catch (Exception e)
        {
            _logger.WriteLine($"[gbfr.qol.buildcard] {e}", Color.Red);
        }
        Dispatch.Raise(_logger, nameof(ShortcutUpdated), ShortcutUpdated);
        _updateHook!.OriginalFunction(shortcut);
    }

    private static bool OnStatus01(nint component)
    {
        nint obj = *(nint*)(component + ComponentObject);
        return obj != 0 && new UiObject(obj).NameHash == CardLayout.Status01.NameHash;
    }

    // UpdateShortcutInput's input check
    private static bool TakesInput(nint component)
    {
        uint flags = *(uint*)(component + InputFlags);
        return *(byte*)(component + InputBlocked) == 0 && (flags & InputEnabled) != 0 && (flags & InputButtons) != 0;
    }
}
