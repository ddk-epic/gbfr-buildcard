using NenTools.Reloaded.ScanManager.Interfaces;

using gbfr.qol.buildcard.Model;

namespace gbfr.qol.buildcard.Hooks;

// Fills the summon cells, an empty slot with summon id 0.
public unsafe class SummonsBinder : ICardBinder
{
    private const string SummonInfoType = ".?AVSummonInfo@component@ui@@";

    private readonly UiObjects _objects;
    private delegate* unmanaged<nint, uint, void> _setSummonInfo;

    public SummonsBinder(UiObjects objects, Vtables vtables)
    {
        _objects = objects;
        // found at load
        vtables.Find(SummonInfoType);
    }

    public void Init(IScanManager scanManager, string signatureGroup)
    {
        scanManager.AddScan("SetSummonInfo", signatureGroup, address =>
            _setSummonInfo = (delegate* unmanaged<nint, uint, void>)(nint)address);
    }

    public void Write(IReadOnlyDictionary<int, UiObject> objects, BuildCard card)
    {
        for (int i = 0; i < card.Summons.Length; i++)
        {
            if (_setSummonInfo != null && _objects.FindComponent(objects[CardLayout.Summons.Slots[i]], SummonInfoType) is { } summonInfo)
                _setSummonInfo(summonInfo.Address, card.Summons[i] ?? 0);
        }
    }
}
