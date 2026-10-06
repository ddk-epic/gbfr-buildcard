using NenTools.Reloaded.ScanManager.Interfaces;
using Reloaded.Hooks.Definitions;

using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace gbfr.qol.buildcard.Hooks;

// Hooks the function that fills a CharaInfo from a character.
public unsafe class CharaStatusHooks
{
    private readonly IReloadedHooks _hooks;

    // Raised after the game fills a CharaInfo: the component and the character struct.
    public event Action<nint, nint>? Filled;

    private delegate void FillCharacterStatus(nint charaInfo, nint chara, int index);
    private IHook<FillCharacterStatus>? _fillHook;

    public CharaStatusHooks(IReloadedHooks hooks)
    {
        _hooks = hooks;
    }

    public void Init(IScanManager scanManager, string signatureGroup)
    {
        scanManager.AddScan(nameof(FillCharacterStatus), signatureGroup, address =>
            _fillHook = _hooks.CreateHook<FillCharacterStatus>(FillImpl, address).Activate());
    }

    private void FillImpl(nint charaInfo, nint chara, int index)
    {
        _fillHook!.OriginalFunction(charaInfo, chara, index);
        if (chara != 0)
            Filled?.Invoke(charaInfo, chara);
    }
}
