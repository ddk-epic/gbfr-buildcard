using System.Runtime.InteropServices;

using NenTools.Reloaded.ScanManager.Interfaces;

using gbfr.qol.buildcard.Model;

namespace gbfr.qol.buildcard.Hooks;

// Fills the Over Mastery rows and writes each row's value after its name.
public unsafe class OverMasteryBinder : ICardBinder
{
    private const string LimitBonusInfoType = ".?AVLimitBonusInfo@component@ui@@";
    private const string ValuePrefix = " +";

    private readonly GameText _text;
    private readonly UiObjects _objects;
    private delegate* unmanaged<nint, nint, void> _setLine;
    private readonly OverMasteryLine* _lines = (OverMasteryLine*)Marshal.AllocHGlobal(sizeof(OverMasteryLine) * CharaBuild.OverMasteryLines);

    public OverMasteryBinder(GameText text, UiObjects objects, Vtables vtables)
    {
        _text = text;
        _objects = objects;
        // found at load
        vtables.Find(LimitBonusInfoType);
    }

    public void Init(IScanManager scanManager, string signatureGroup)
    {
        scanManager.AddScan("SetOverMasteryLine", signatureGroup, address =>
            _setLine = (delegate* unmanaged<nint, nint, void>)(nint)address);
    }

    public void Write(IReadOnlyDictionary<int, UiObject> objects, BuildCard card)
    {
        for (int i = 0; i < card.OverMastery.Length; i++)
        {
            var row = objects[CardLayout.OverMastery.Rows[i]];
            if (card.OverMastery[i] is { } line)
                SetLine(row, i, line);
            _objects.SetActive(row, card.OverMastery[i] != null);
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
                _objects.SetActive(objects[id], false);
        }
    }

    private void SetLine(UiObject row, int index, OverMasteryLine line)
    {
        if (_setLine == null || _objects.FindComponent(row, LimitBonusInfoType) is not { } limitBonusInfo)
            return;
        _lines[index] = line;
        _setLine(limitBonusInfo.Address, (nint)(_lines + index));
    }
}
