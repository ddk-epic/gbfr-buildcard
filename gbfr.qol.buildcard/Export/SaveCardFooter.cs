using Reloaded.Hooks.Definitions;

using gbfr.qol.buildcard.Core;
using gbfr.qol.buildcard.Interop.Game;
using gbfr.qol.buildcard.Interop.Native;

using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace gbfr.qol.buildcard.Export;

// Adds Save Card to the Character Details footer.
public unsafe class SaveCardFooter : IFeature
{
    // SetCharacterDetailsFooter's mov rbx, [rip + disp32]
    private const int ListLoad = 0x36;
    private const int ListCapacity = 10;

    private readonly IReloadedHooks _hooks;
    private readonly GameApi _api;
    private long* _labels;

    private delegate void SetCharacterDetailsFooter();
    private IHook<SetCharacterDetailsFooter>? _footerHook;

    public SaveCardFooter(IReloadedHooks hooks, GameApi api)
    {
        _hooks = hooks;
        _api = api;
    }

    public Capability[] Requires => [_api.SetCharacterDetailsFooter];

    public void Start()
    {
        nint footer = _api.SetCharacterDetailsFooter.Address;
        _labels = (long*)PeImage.RipGlobal((byte*)footer + ListLoad, [0x48, 0x8B, 0x1D]);
        if (_labels != null)
            _footerHook = _hooks.CreateHook<SetCharacterDetailsFooter>(FooterImpl, footer).Activate();
    }

    private void FooterImpl()
    {
        AddLabel();
        _footerHook!.OriginalFunction();
    }

    private void AddLabel()
    {
        long count = *_labels;
        if (count is < 0 or >= ListCapacity)
            return;
        uint* labels = (uint*)(_labels + 1);
        for (long i = 0; i < count; i++)
        {
            if (labels[i] == CardLayout.Footer.SaveCardLabel)
                return;
        }
        labels[count] = CardLayout.Footer.SaveCardLabel;
        *_labels = count + 1;
    }
}
