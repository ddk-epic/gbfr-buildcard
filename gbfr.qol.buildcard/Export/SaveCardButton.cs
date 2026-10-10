using NenTools.Reloaded.ScanManager.Interfaces;
using Reloaded.Hooks.Definitions;

using gbfr.qol.buildcard.Hooks;

using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace gbfr.qol.buildcard.Export;

// Adds Save Card to the Character Details footer and reads its button, polled from the game's shortcut updates.
public unsafe class SaveCardButton
{
    // SetCharacterDetailsFooter's mov rbx, [rip + disp32] loading the footer's label list: a count, then label hashes
    private const int ListLoad = 0x36;
    private const int ListCapacity = 10;

    // the GetButtonBits signature's mov r15, [rip + disp32] loading the input manager, and its call of GetButtonBits
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

    // SetCharacterDetailsFooter() sets the Character Details footer, UpdateShortcutInput(shortcut) reads a shortcut's
    // button, GetButtonBits(input context, button, 1, mode) returns a button's input bits
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

    // Appends Save Card's label to the footer's label list once.
    private void AddLabel()
    {
        long count = *_labels;
        uint* labels = (uint*)(_labels + 1);
        for (long i = 0; i < count; i++)
        {
            if (labels[i] == CardIds.SaveCardLabel)
                return;
        }
        if (count is < 0 or >= ListCapacity)
            return;
        labels[count] = CardIds.SaveCardLabel;
        *_labels = count + 1;
    }
}
