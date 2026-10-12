using System.Drawing;
using System.Runtime.InteropServices;

using NenTools.Reloaded.ScanManager.Interfaces;
using Reloaded.Mod.Interfaces;

namespace gbfr.qol.buildcard.Hooks;

// Applies CardContents' writes to the card's objects.
public unsafe class CardWriter
{
    private readonly CardContents _contents;
    private readonly GameText _text;
    private readonly MasterTraits _masterTraits;
    private readonly UiObjects _objects;
    private readonly ILogger _logger;
    private delegate* unmanaged<nint, nint, void> _setOverMasteryLine;
    private delegate* unmanaged<nint, uint, void> _setSummonInfo;
    private readonly nint _summonInfoVtable;
    private readonly nint _limitBonusInfoVtable;
    private readonly OverMasteryLine* _overMasteryLines = (OverMasteryLine*)Marshal.AllocHGlobal(sizeof(OverMasteryLine) * CharaBuild.OverMasteryLines);
    private bool _loggedCount;

    public CardWriter(CardContents contents, GameText text, MasterTraits masterTraits, UiObjects objects, ILogger logger)
    {
        _contents = contents;
        _text = text;
        _masterTraits = masterTraits;
        _objects = objects;
        _logger = logger;
        _summonInfoVtable = objects.FindVtable(".?AVSummonInfo@component@ui@@");
        _limitBonusInfoVtable = objects.FindVtable(".?AVLimitBonusInfo@component@ui@@");
    }

    public void Init(IScanManager scanManager, string signatureGroup)
    {
        scanManager.AddScan("SetOverMasteryLine", signatureGroup, address =>
            _setOverMasteryLine = (delegate* unmanaged<nint, nint, void>)(nint)address);
        scanManager.AddScan("SetSummonInfo", signatureGroup, address =>
            _setSummonInfo = (delegate* unmanaged<nint, uint, void>)(nint)address);
    }

    public void OnCharacterShown(nint charaInfo, nint chara)
    {
        try
        {
            Write(charaInfo, chara);
        }
        catch (Exception e)
        {
            _logger.WriteLine($"[gbfr.qol.buildcard] Writing the card failed: {e}", Color.Red);
        }
    }

    private void Write(nint charaInfo, nint chara)
    {
        var objects = _objects.Find(charaInfo);
        if (objects == null)
            return;
        if (objects.Count != CardIds.ObjectCount)
        {
            if (!_loggedCount)
                _logger.WriteLine($"[gbfr.qol.buildcard] status01 has {objects.Count} objects, the build {CardIds.ObjectCount}: card not written", Color.Red);
            _loggedCount = true;
            return;
        }

        foreach (var write in _contents.Compose(chara))
        {
            if (!objects.TryGetValue(write.Id, out nint obj))
                continue;
            switch (write)
            {
                case TextWrite text:
                    _text.Set(obj, text.Value, text.Hash);
                    break;
                case MasterTraitDescriptionWrite description:
                    _text.Wrap(obj, description.Width,
                        text => _masterTraits.FillDescription(text, description.CharaKey, description.Slot), description.IconScale);
                    break;
                case ActiveWrite active:
                    _objects.SetActive(obj, active.Active);
                    break;
                case SummonWrite summon:
                    SetSummonInfo(obj, summon.SummonId);
                    break;
                case OverMasteryWrite overMastery:
                    SetOverMasteryLine(obj, overMastery.Line, overMastery.Value);
                    break;
                case RewrapWrite rewrap:
                    _text.Rewrap(obj, rewrap.Width, rewrap.MaxLines);
                    break;
                case SuffixWrite suffix:
                    SetSuffix(obj, suffix, objects);
                    break;
            }
        }
    }

    private void SetSummonInfo(nint obj, uint summonId)
    {
        if (_setSummonInfo != null && _objects.FindComponent(obj, _summonInfoVtable) is var summonInfo and not 0)
            _setSummonInfo(summonInfo, summonId);
    }

    private void SetOverMasteryLine(nint obj, int index, OverMasteryLine line)
    {
        if (_setOverMasteryLine == null || _objects.FindComponent(obj, _limitBonusInfoVtable) is not (var limitBonusInfo and not 0))
            return;
        _overMasteryLines[index] = line;
        _setOverMasteryLine(limitBonusInfo, (nint)(_overMasteryLines + index));
    }

    private void SetSuffix(nint obj, SuffixWrite write, Dictionary<int, nint> objects)
    {
        var values = write.Values.Select(id => objects[id])
            .Concat(write.ActiveValues.Select(id => objects[id]).Where(_objects.IsActive))
            .Select(_text.Read);
        _text.Rewrap(obj, write.Width, suffix: write.Prefix + string.Concat(values));
        foreach (short id in write.Hidden)
            _objects.SetActive(objects[id], false);
    }
}
