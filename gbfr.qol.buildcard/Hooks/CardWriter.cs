using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

using NenTools.Reloaded.ScanManager.Interfaces;
using Reloaded.Mod.Interfaces;

namespace gbfr.qol.buildcard.Hooks;

// Applies CardContents' writes to the card's objects and sets the weapon WeaponArtHooks loads.
public unsafe class CardWriter
{
    // Text component
    private const int TextString = 0x40;
    private const int TextHash = 0x188;
    private const int MaxTextLength = 0x400;
    private const float IconScale = 0.7f;  // times the game's icon size


    private readonly GameText _text;
    private readonly TextWrap _wrap;
    private readonly MasterTraits _masterTraits;
    private readonly Masteries _masteries;
    private readonly WeaponArtHooks _weaponArt;
    private readonly UiObjects _objects;
    private readonly ILogger _logger;
    private readonly CardContents _contents;
    private delegate* unmanaged<nint, nint, void> _setOverMasteryLine;
    private delegate* unmanaged<nint, uint, void> _setSummonInfo;
    private readonly nint _textVtable;
    private readonly nint _summonInfoVtable;
    private readonly nint _limitBonusInfoVtable;
    private readonly OverMasteryLine* _overMasteryLines = (OverMasteryLine*)Marshal.AllocHGlobal(sizeof(OverMasteryLine) * CharaBuild.OverMasteryLines);
    private nint _charaNameObject;
    private bool _loggedBuild;
    private bool _loggedCount;

    public CardWriter(GameText text, TextWrap wrap, MasterTraits masterTraits, Masteries masteries, WeaponArtHooks weaponArt,
        UiObjects objects, ILogger logger)
    {
        _text = text;
        _wrap = wrap;
        _masterTraits = masterTraits;
        _masteries = masteries;
        _weaponArt = weaponArt;
        _objects = objects;
        _logger = logger;
        _contents = new CardContents(text.Find);
        _textVtable = objects.FindVtable(".?AVText@component@ui@@");
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

    public void OnFilled(nint charaInfo, nint chara)
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
            LogOnce(ref _loggedCount, $"status01 has {objects.Count} objects, the build {CardIds.ObjectCount}: card not written", Color.Red);
            return;
        }

        var build = CharaBuild.Decode(chara, message => LogOnce(ref _loggedBuild, $"Chara build: {message}", Color.Yellow));
        foreach (var write in _contents.Compose(build, _masterTraits.ReadCells(build.CharaKey), _masteries.ReadPercents(build.CharaKey)))
        {
            if (!objects.TryGetValue(write.Id, out nint obj))
                continue;
            switch (write)
            {
                case TextWrite text:
                    SetText(obj, text.Value, text.Hash);
                    break;
                case MasterTraitDescriptionWrite description:
                    SetMasterTraitDescription(obj, description);
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
            }
        }
        WrapSkillNames(objects);
        _weaponArt.Show(chara);
        _charaNameObject = objects.GetValueOrDefault(CardIds.CharaName);
    }

    public string CharaName()
    {
        nint text = _charaNameObject == 0 ? 0 : _objects.FindComponent(_charaNameObject, _textVtable);
        return text == 0 ? "" : ReadString(text + TextString);
    }

    private void SetText(nint obj, string value, uint hash)
    {
        if (_objects.FindComponent(obj, _textVtable) is var text and not 0)
            _text.Set(text, value, hash);
    }

    private void SetMasterTraitDescription(nint obj, MasterTraitDescriptionWrite write)
    {
        if (_objects.FindComponent(obj, _textVtable) is not (var text and not 0))
            return;
        _wrap.Limit(text, CardIds.CellTextWidth);
        _wrap.ScaleIcons(text, IconScale);
        _masterTraits.FillDescription(text, write.CharaKey, write.Slot);
        _wrap.Cap(text);
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

    private void WrapSkillNames(Dictionary<int, nint> objects)
    {
        foreach (short id in CardIds.SkillNames)
        {
            nint text = _objects.FindComponent(objects[id], _textVtable);
            if (text == 0)
                continue;
            _wrap.Limit(text, CardIds.SkillNameWidth);
            _text.Set(text, ReadString(text + TextString), *(uint*)(text + TextHash));
            _wrap.Cap(text);
        }
    }

    private static string ReadString(nint str)
    {
        long size = *(long*)(str + 0x10);
        if (size <= 0 || size > MaxTextLength)
            return "";
        nint data = *(long*)(str + 0x18) > 15 ? *(nint*)str : str;
        return Encoding.UTF8.GetString((byte*)data, (int)size);
    }

    private void LogOnce(ref bool logged, string message, Color color)
    {
        if (logged)
            return;
        logged = true;
        _logger.WriteLine($"[gbfr.qol.buildcard] {message}", color);
    }
}
