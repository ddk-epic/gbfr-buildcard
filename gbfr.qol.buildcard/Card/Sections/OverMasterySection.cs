using System.Runtime.InteropServices;

using gbfr.qol.buildcard.Card;
using gbfr.qol.buildcard.Game;
using gbfr.qol.buildcard.Host;
using gbfr.qol.buildcard.Interop.Game;
using gbfr.qol.buildcard.Model;
using gbfr.qol.buildcard.Sources;

namespace gbfr.qol.buildcard.Card.Sections;

// Fills the Over Mastery rows and writes each row's value after its name.
public unsafe class OverMasterySection : ICardSection, IFeature
{
    private const string ValuePrefix = " +";

    private readonly TextService _text;
    private readonly TextTable _table;
    private readonly UiTree _tree;
    private readonly GameApi _api;
    private delegate* unmanaged<nint, nint, void> _setLine;
    private readonly OverMasteryLine* _lines = (OverMasteryLine*)Marshal.AllocHGlobal(sizeof(OverMasteryLine) * CharaSnapshot.OverMasteryLines);
    private readonly bool[] _filled = new bool[CharaSnapshot.OverMasteryLines];

    public OverMasterySection(TextService text, TextTable table, UiTree tree, GameApi api)
    {
        _text = text;
        _table = table;
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
            _filled[i] = card.OverMastery[i] is { } line && SetLine(row, i, line.Line);
            _tree.SetActive(row, _filled[i]);
        }
    }

    // Appends the value to the name and hides the value's objects
    public void Layout(IReadOnlyDictionary<int, UiObject> objects, BuildCard card)
    {
        for (int i = 0; i < card.OverMastery.Length; i++)
        {
            if (!_filled[i] || card.OverMastery[i] is not { } line)
                continue;
            string suffix = ValuePrefix + line.Value + (line.Percent ? _table.Find(CardLayout.OverMastery.PercentText) : "");
            _text.Rewrap(objects[CardLayout.OverMastery.Names[i]], CardLayout.OverMastery.NameWidth, suffix: suffix);
            foreach (short id in CardLayout.OverMastery.Values[i])
                _tree.SetActive(objects[id], false);
        }
    }

    private bool SetLine(UiObject row, int index, OverMasteryLine line)
    {
        if (_setLine == null || _tree.FindComponent(row, _api.LimitBonusInfoVtable) is not { } limitBonusInfo)
            return false;
        _lines[index] = line;
        _setLine(limitBonusInfo.Address, (nint)(_lines + index));
        return true;
    }
}
