using gbfr.qol.buildcard.Card;
using gbfr.qol.buildcard.Game;
using gbfr.qol.buildcard.Host;
using gbfr.qol.buildcard.Interop.Game;
using gbfr.qol.buildcard.Model;

namespace gbfr.qol.buildcard.Card.Sections;

// Fills the summon cells, an empty slot with summon id 0.
public unsafe class SummonsSection : ICardSection, IFeature
{
    private readonly UiTree _tree;
    private readonly GameApi _api;
    private delegate* unmanaged<nint, uint, void> _setSummonInfo;

    public SummonsSection(UiTree tree, GameApi api)
    {
        _tree = tree;
        _api = api;
    }

    public Capability[] Requires => [_api.SetSummonInfo, _api.SummonInfoVtable];

    public void Start() => _setSummonInfo = (delegate* unmanaged<nint, uint, void>)_api.SetSummonInfo.Address;

    public void Write(IReadOnlyDictionary<int, UiObject> objects, BuildCard card)
    {
        for (int i = 0; i < card.Summons.Length; i++)
        {
            if (_setSummonInfo != null && _tree.FindComponent(objects[CardLayout.Summons.Slots[i]], _api.SummonInfoVtable) is { } summonInfo)
                _setSummonInfo(summonInfo.Address, card.Summons[i] ?? 0);
        }
    }
}
