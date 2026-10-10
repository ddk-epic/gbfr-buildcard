using NenTools.Reloaded.ScanManager.Interfaces;
using Reloaded.Hooks.Definitions;

using gbfr.qol.buildcard.Hooks;

using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace gbfr.qol.buildcard.Export;

// Adds Save Card to the Character Details footer and reads its button.
public unsafe class SaveCardButton
{
    // SetCharacterDetailsFooter's mov rbx, [rip + disp32]
    private const int ListLoad = 0x36;
    private const int ListCapacity = 10;

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

    // the mode a Shortcut without a context passes
    private const uint DefaultMode = 3;

    private readonly IReloadedHooks _hooks;
    private long* _labels;
    private nint* _input;
    private delegate* unmanaged<uint, int, byte, uint, uint> _getButtonBits;
    private bool _held;

    // Raised on the game thread for each shortcut update.
    public event Action? Tick;

    private delegate void SetCharacterDetailsFooter();
    private delegate void UpdateShortcutInput(nint shortcut);
    private IHook<SetCharacterDetailsFooter>? _footerHook;
    private IHook<UpdateShortcutInput>? _updateHook;

    public SaveCardButton(IReloadedHooks hooks)
    {
        _hooks = hooks;
    }

    // GetButtonBits(input context, button, 1, mode)
    public void Init(IScanManager scanManager, string signatureGroup)
    {
        scanManager.AddScan(nameof(SetCharacterDetailsFooter), signatureGroup, address =>
        {
            _labels = (long*)PeImage.RipGlobal((byte*)address + ListLoad, [0x48, 0x8B, 0x1D]);
            if (_labels != null)
                _footerHook = _hooks.CreateHook<SetCharacterDetailsFooter>(FooterImpl, address).Activate();
        });
        scanManager.AddScan(nameof(UpdateShortcutInput), signatureGroup, address =>
            _updateHook = _hooks.CreateHook<UpdateShortcutInput>(UpdateImpl, address).Activate());
        scanManager.AddScan("GetButtonBits", signatureGroup, address =>
        {
            byte* call = (byte*)address + MaskCall;
            if (*call != 0xE8)
                return;
            _input = PeImage.RipGlobal((byte*)address + InputLoad, [0x4C, 0x8B, 0x3D]);
            _getButtonBits = (delegate* unmanaged<uint, int, byte, uint, uint>)(call + 5 + *(int*)(call + 1));
        });
    }

    // Whether the button went down since the last call.
    public bool Pressed()
    {
        if (_input == null || *_input == 0 || _getButtonBits == null)
            return false;
        nint input = *_input;
        uint mask = _getButtonBits(*(uint*)(input + InputContext), CardIds.SaveCardButton, 1, DefaultMode);
        bool down = *(int*)(input + InputLock) < 1
            && ((*(uint*)(input + HeldButtons) & mask) != 0 || (*(uint*)(input + HeldMenuKeys) & MenuKey) != 0);
        bool pressed = down && !_held;
        _held = down;
        return pressed;
    }

    private void FooterImpl()
    {
        AddLabel();
        _footerHook!.OriginalFunction();
    }

    private void UpdateImpl(nint shortcut)
    {
        Tick?.Invoke();
        _updateHook!.OriginalFunction(shortcut);
    }

    private void AddLabel()
    {
        long count = *_labels;
        if (count is < 0 or >= ListCapacity)
            return;
        uint* labels = (uint*)(_labels + 1);
        for (long i = 0; i < count; i++)
        {
            if (labels[i] == CardIds.SaveCardLabel)
                return;
        }
        labels[count] = CardIds.SaveCardLabel;
        *_labels = count + 1;
    }
}
