using System.Runtime.InteropServices;

using gbfr.qol.buildcard.Card;
using gbfr.qol.buildcard.Game;
using gbfr.qol.buildcard.Host;
using gbfr.qol.buildcard.Interop.Game;
using gbfr.qol.buildcard.Model;
using gbfr.qol.buildcard.Sources;

namespace gbfr.qol.buildcard.Card.Binders;

// Fills the Over Mastery rows and writes each row's value after its name.
public unsafe class OverMasteryBinder : ICardBinder, IFeature
{
    private const string ValuePrefix = " +";

    private readonly TextService _text;
    private readonly UiTree _tree;
    private readonly GameApi _api;
    private delegate* unmanaged<nint, nint, void> _setLine;
    private readonly OverMasteryLine* _lines = (OverMasteryLine*)Marshal.AllocHGlobal(sizeof(OverMasteryLine) * CharaSnapshot.OverMasteryLines);

    public OverMasteryBinder(TextService text, UiTree tree, GameApi api)
    {
        _text = text;
        _tree = tree;
        _api = api;
    }

    public Capability[] Requires => [_api.SetOverMasteryLine, _api.LimitBonusInfoVtable];

    public void Start() => _setLine = (delegate* unmanaged<nint, nint, void>)_api.SetOverMasteryLine.Address;

    public void Write(IReadOnlyDictionary<int, UiObject> objects, BuildCard card)
    {
        for (int i = 0; i < card.OverMastery.Length; i++)
        {
            var row = objects[CardLayout.OverMastery.Rows[i]];
            if (card.OverMastery[i] is { } line)
                SetLine(row, i, line);
            _tree.SetActive(row, card.OverMastery[i] != null);
        }
    }

    // Appends the value to the name and hides the value's objects
    public void Layout(IReadOnlyDictionary<int, UiObject> objects, BuildCard card)
    {
        for (int i = 0; i < card.OverMastery.Length; i++)
        {
            if (card.OverMastery[i] == null)
                continue;
            short[] values = CardLayout.OverMastery.Values[i];
            var number = objects[values[1]];
            var percent = objects[values[2]];
            string suffix = ValuePrefix + _text.Read(number) + (percent.IsActive ? _text.Read(percent) : "");
            _text.Rewrap(objects[CardLayout.OverMastery.Names[i]], CardLayout.OverMastery.NameWidth, suffix: suffix);
            foreach (short id in values)
                _tree.SetActive(objects[id], false);
        }
    }

    private void SetLine(UiObject row, int index, OverMasteryLine line)
    {
        if (_setLine == null || _tree.FindComponent(row, _api.LimitBonusInfoVtable) is not { } limitBonusInfo)
            return;
        _lines[index] = line;
        _setLine(limitBonusInfo.Address, (nint)(_lines + index));
    }
}
