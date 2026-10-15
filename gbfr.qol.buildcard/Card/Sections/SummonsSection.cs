using gbfr.qol.buildcard.Card;
using gbfr.qol.buildcard.Core;
using gbfr.qol.buildcard.Game;
using gbfr.qol.buildcard.Interop.Game;
using gbfr.qol.buildcard.Interop.Native;
using gbfr.qol.buildcard.Model;
using gbfr.qol.buildcard.Sources;

namespace gbfr.qol.buildcard.Card.Sections;

// Fills the summon cells, an empty slot with summon id 0.
public unsafe class SummonsSection : ICardSection, IFeature
{
    // SetSummonInfo's mov rdi, [rip + disp32] loading the summon inventory's owner
    private const int InventoryLoad = 0x11;

    // the summon inventory's list head node, on its owner
    private const int InventoryHead = 0xE6F0;

    // a list node's record pointer
    private const int NodeRecord = 0x18;

    private readonly UiTree _tree;
    private readonly GameApi _api;
    private delegate* unmanaged<nint, uint, void> _setSummonInfo;
    private nint* _inventoryOwner;

    public SummonsSection(UiTree tree, GameApi api)
    {
        _tree = tree;
        _api = api;
    }

    public Capability[] Requires => [_api.SetSummonInfo, _api.SummonInfoVtable];

    public void Start()
    {
        _setSummonInfo = (delegate* unmanaged<nint, uint, void>)_api.SetSummonInfo.Address;
        _inventoryOwner = PeImage.RipGlobal((byte*)_api.SetSummonInfo.Address + InventoryLoad, [0x48, 0x8B, 0x3D]);
    }

    public void Write(IReadOnlyDictionary<int, UiObject> objects, BuildCard card)
    {
        for (int i = 0; i < card.Summons.Length; i++)
        {
            if (_setSummonInfo == null
                || _tree.FindComponent(objects[CardLayout.Summons.Slots[i]], _api.SummonInfoVtable) is not { } summonInfo)
                continue;
            if (card.Summons[i] is { Id: 0 } slot)
                SetBorrowed(summonInfo.Address, slot);
            else
                _setSummonInfo(summonInfo.Address, card.Summons[i]?.Id ?? 0);
        }
    }

    // Fills a slot without an id through a borrowed inventory record
    private void SetBorrowed(nint summonInfo, SummonSlot slot)
    {
        SummonSlot* record = FirstRecord();
        if (record == null)
        {
            _setSummonInfo(summonInfo, 0);
            return;
        }
        var saved = *record;
        *record = slot with { Id = saved.Id, Unknown = saved.Unknown };
        try
        {
            _setSummonInfo(summonInfo, saved.Id);
        }
        finally
        {
            *record = saved;
        }
    }

    private SummonSlot* FirstRecord()
    {
        if (_inventoryOwner == null || *_inventoryOwner == 0)
            return null;
        nint head = *(nint*)(*_inventoryOwner + InventoryHead);
        if (head == 0)
            return null;
        nint node = *(nint*)head;
        return node == 0 || node == head ? null : *(SummonSlot**)(node + NodeRecord);
    }
}
