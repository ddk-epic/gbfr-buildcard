using NenTools.Reloaded.ScanManager.Interfaces;
using Reloaded.Hooks.Definitions;

using gbfr.qol.buildcard.Hooks;

using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace gbfr.qol.buildcard.Export;

// Adds Save Card to the Character Details footer.
public unsafe class SaveCardButton
{
    // SetCharacterDetailsFooter's mov rbx, [rip + disp32] loading the footer's label list: a count, then label hashes
    private const int ListLoad = 0x36;
    private const int ListCapacity = 10;

    private readonly IReloadedHooks _hooks;
    private long* _labels;

    private delegate void SetCharacterDetailsFooter();
    private IHook<SetCharacterDetailsFooter>? _footerHook;

    public SaveCardButton(IReloadedHooks hooks)
    {
        _hooks = hooks;
    }

    // SetCharacterDetailsFooter() sets the Character Details footer
    public void Init(IScanManager scanManager, string signatureGroup)
    {
        scanManager.AddScan(nameof(SetCharacterDetailsFooter), signatureGroup, address =>
        {
            _labels = (long*)PeImage.RipGlobal((byte*)address + ListLoad, [0x48, 0x8B, 0x1D]);
            if (_labels != null)
                _footerHook = _hooks.CreateHook<SetCharacterDetailsFooter>(FooterImpl, address).Activate();
        });
    }

    private void FooterImpl()
    {
        AddLabel();
        _footerHook!.OriginalFunction();
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
