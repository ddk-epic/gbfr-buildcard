using System.Drawing;
using System.Runtime.InteropServices;

using NenTools.Reloaded.ScanManager.Interfaces;
using Reloaded.Mod.Interfaces;

using gbfr.qol.buildcard.Model;

namespace gbfr.qol.buildcard.Hooks;

// Applies CardContents' writes to the card's objects.
public unsafe class CardWriter
{
    private const string SummonInfoType = ".?AVSummonInfo@component@ui@@";
    private const string LimitBonusInfoType = ".?AVLimitBonusInfo@component@ui@@";

    private readonly CardContents _contents;
    private readonly GameText _text;
    private readonly MasterTraits _masterTraits;
    private readonly UiObjects _objects;
    private readonly ILogger _logger;
    private delegate* unmanaged<nint, nint, void> _setOverMasteryLine;
    private delegate* unmanaged<nint, uint, void> _setSummonInfo;
    private readonly OverMasteryLine* _overMasteryLines = (OverMasteryLine*)Marshal.AllocHGlobal(sizeof(OverMasteryLine) * CharaBuild.OverMasteryLines);
    private bool _loggedHash;

    public CardWriter(CardContents contents, GameText text, MasterTraits masterTraits, UiObjects objects, Vtables vtables,
        ILogger logger)
    {
        _contents = contents;
        _text = text;
        _masterTraits = masterTraits;
        _objects = objects;
        _logger = logger;
        // found at load
        vtables.Find(SummonInfoType);
        vtables.Find(LimitBonusInfoType);
    }

    public void Init(IScanManager scanManager, string signatureGroup)
    {
        scanManager.AddScan("SetOverMasteryLine", signatureGroup, address =>
            _setOverMasteryLine = (delegate* unmanaged<nint, nint, void>)(nint)address);
        scanManager.AddScan("SetSummonInfo", signatureGroup, address =>
            _setSummonInfo = (delegate* unmanaged<nint, uint, void>)(nint)address);
    }

    public void OnShown(CharaInfo charaInfo, BuildCard card)
    {
        var objects = _objects.Find(charaInfo);
        if (objects == null)
            return;
        uint hash = UiObjects.PrefabHash(objects);
        if (hash != CardLayout.Status01.PrefabHash)
        {
            if (!_loggedHash)
                _logger.WriteLine($"[gbfr.qol.buildcard] status01 has {objects.Count} objects hashing to {hash:X8}, the build {CardLayout.Status01.ObjectCount} hashing to {CardLayout.Status01.PrefabHash:X8}: card not written", Color.Red);
            _loggedHash = true;
            return;
        }

        foreach (var write in _contents.Compose(card))
        {
            if (!objects.TryGetValue(write.Id, out UiObject obj))
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

    private void SetSummonInfo(UiObject obj, uint summonId)
    {
        if (_setSummonInfo != null && _objects.FindComponent(obj, SummonInfoType) is { } summonInfo)
            _setSummonInfo(summonInfo.Address, summonId);
    }

    private void SetOverMasteryLine(UiObject obj, int index, OverMasteryLine line)
    {
        if (_setOverMasteryLine == null || _objects.FindComponent(obj, LimitBonusInfoType) is not { } limitBonusInfo)
            return;
        _overMasteryLines[index] = line;
        _setOverMasteryLine(limitBonusInfo.Address, (nint)(_overMasteryLines + index));
    }

    private void SetSuffix(UiObject obj, SuffixWrite write, Dictionary<int, UiObject> objects)
    {
        var values = write.Values.Select(id => objects[id])
            .Concat(write.ActiveValues.Select(id => objects[id]).Where(value => value.IsActive))
            .Select(_text.Read);
        _text.Rewrap(obj, write.Width, suffix: write.Prefix + string.Concat(values));
        foreach (short id in write.Hidden)
            _objects.SetActive(objects[id], false);
    }
}
