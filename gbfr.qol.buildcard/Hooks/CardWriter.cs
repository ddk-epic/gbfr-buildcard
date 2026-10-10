using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

using NenTools.Reloaded.ScanManager.Interfaces;
using Reloaded.Mod.Interfaces;

namespace gbfr.qol.buildcard.Hooks;

// Applies CardContents' writes to the card's objects and sets the weapon WeaponArtHooks loads.
public unsafe class CardWriter
{
    // Text component: MSVC std::string at +0x40 (inline up to 15 bytes, else a pointer), text id hash at +0x188
    private const int TextString = 0x40;
    private const int TextHash = 0x188;
    private const int MaxTextLength = 0x400;
    private const float IconScale = 0.7f;  // times the game's icon size

    private readonly GameText _text;
    private readonly TextWrap _wrap;
    private readonly MasterTraits _masterTraits;
    private readonly Masteries _masteries;
    private readonly WeaponArtHooks _weaponArt;
    private readonly ILogger _logger;
    private readonly CardContents _contents = new();
    private delegate* unmanaged<nint, byte, void> _setActive;
    private delegate* unmanaged<nint, nint, void> _setOverMasteryLine;
    private delegate* unmanaged<nint, uint, void> _setSummonInfo;
    private readonly nint _exeBase = Process.GetCurrentProcess().MainModule!.BaseAddress;
    private readonly nint _limitBonusInfoVtable;
    private readonly nint _summonInfoVtable;
    private readonly nint _textVtable;
    // Over Mastery lines passed to SetOverMasteryLine, one per row
    private readonly OverMasteryLine* _overMasteryLines = (OverMasteryLine*)Marshal.AllocHGlobal(sizeof(OverMasteryLine) * CharaBuild.OverMasteryLines);
    private bool _loggedBuild;
    private bool _loggedComponents;
    private bool _loggedCount;

    public CardWriter(GameText text, TextWrap wrap, MasterTraits masterTraits, Masteries masteries, WeaponArtHooks weaponArt, ILogger logger)
    {
        _text = text;
        _wrap = wrap;
        _masterTraits = masterTraits;
        _masteries = masteries;
        _weaponArt = weaponArt;
        _logger = logger;
        _limitBonusInfoVtable = PeImage.FindVtable(_exeBase, ".?AVLimitBonusInfo@component@ui@@");
        if (_limitBonusInfoVtable == 0)
            _logger.WriteLine("[gbfr.qol.buildcard] LimitBonusInfo vtable not found", Color.Red);
        _summonInfoVtable = PeImage.FindVtable(_exeBase, ".?AVSummonInfo@component@ui@@");
        if (_summonInfoVtable == 0)
            _logger.WriteLine("[gbfr.qol.buildcard] SummonInfo vtable not found", Color.Red);
        _textVtable = PeImage.FindVtable(_exeBase, ".?AVText@component@ui@@");
        if (_textVtable == 0)
            _logger.WriteLine("[gbfr.qol.buildcard] Text vtable not found", Color.Red);
    }

    // SetObjectActive(object, active), SetOverMasteryLine(LimitBonusInfo component, Over Mastery line),
    // SetSummonInfo(SummonInfo component, summon id)
    public void Init(IScanManager scanManager, string signatureGroup)
    {
        scanManager.AddScan("SetObjectActive", signatureGroup, address =>
            _setActive = (delegate* unmanaged<nint, byte, void>)(nint)address);
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
        var objects = ObjectTree.Find(charaInfo);
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
                    SetText(obj, text.Value);
                    break;
                case LocalizedTextWrite localized:
                    SetLocalizedText(obj, localized.TextId, localized.SubId);
                    break;
                case LabeledTextWrite labeled:
                    SetText(obj, $"{_text.Find(labeled.LabelId)} {labeled.Value}");
                    break;
                case StyleNameWrite styleName:
                    SetText(obj, StyleName(_text.Find(styleName.TitleId)));
                    break;
                case MasterTraitDescriptionWrite description:
                    SetMasterTraitDescription(obj, description);
                    break;
                case ActiveWrite active:
                    SetActive(obj, active.Active);
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
    }

    private void SetText(nint obj, string value)
    {
        if (FindComponent(obj, _textVtable, "Text") is var text and not 0)
            _text.Set(text, value);
    }

    // Sets the text id's text of the sub-id in the loaded language
    private void SetLocalizedText(nint obj, uint textId, uint subId)
    {
        if (FindComponent(obj, _textVtable, "Text") is var text and not 0)
            _text.Set(text, _text.Find(textId, subId), textId);
    }

    // The style name before the title's colon
    private static string StyleName(string title)
    {
        int colon = title.IndexOfAny([':', '：']);
        return colon < 0 ? title : title[..colon].TrimEnd();
    }

    // Sets the cell's description as the Master Traits menu shows it, wrapped to the cell
    private void SetMasterTraitDescription(nint obj, MasterTraitDescriptionWrite write)
    {
        if (FindComponent(obj, _textVtable, "Text") is not (var text and not 0))
            return;
        _wrap.Limit(text, CardIds.CellTextWidth);
        _wrap.ScaleIcons(text, IconScale);
        _masterTraits.FillDescription(text, write.CharaKey, write.Slot);
        _wrap.Cap(text);
    }

    public void SetActive(nint obj, bool active)
    {
        if (_setActive != null)
            _setActive(obj, active ? (byte)1 : (byte)0);
    }

    private void SetSummonInfo(nint obj, uint summonId)
    {
        if (_setSummonInfo != null && FindComponent(obj, _summonInfoVtable, "SummonInfo") is var summonInfo and not 0)
            _setSummonInfo(summonInfo, summonId);
    }

    private void SetOverMasteryLine(nint obj, int index, OverMasteryLine line)
    {
        if (_setOverMasteryLine == null || FindComponent(obj, _limitBonusInfoVtable, "LimitBonusInfo") is not (var limitBonusInfo and not 0))
            return;
        _overMasteryLines[index] = line;
        _setOverMasteryLine(limitBonusInfo, (nint)(_overMasteryLines + index));
    }

    // Sets the skill names again, wrapped to their width
    private void WrapSkillNames(Dictionary<int, nint> objects)
    {
        foreach (short id in CardIds.SkillNames)
        {
            nint text = FindComponent(objects[id], _textVtable, "Text");
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

    // Components: 0x20-byte entries from +0x28, component at +0x18.
    private nint FindComponent(nint obj, nint vtable, string name)
    {
        if (vtable == 0)
            return 0;
        for (nint entry = *(nint*)(obj + 0x28); entry < *(nint*)(obj + 0x30); entry += 0x20)
        {
            nint component = *(nint*)(entry + 0x18);
            if (component != 0 && *(nint*)component == vtable)
                return component;
        }
        LogComponentsOnce(obj, name, vtable);
        return 0;
    }

    private void LogComponentsOnce(nint obj, string name, nint vtable)
    {
        if (_loggedComponents)
            return;
        var vtables = new List<string>();
        for (nint entry = *(nint*)(obj + 0x28); entry < *(nint*)(obj + 0x30); entry += 0x20)
        {
            nint component = *(nint*)(entry + 0x18);
            vtables.Add(component == 0 ? "null" : $"exe+{*(nint*)component - _exeBase:X}");
        }
        LogOnce(ref _loggedComponents, $"No {name} on a card object (vtable exe+{vtable - _exeBase:X}); components: {string.Join(", ", vtables)}", Color.Yellow);
    }

    private void LogOnce(ref bool logged, string message, Color color)
    {
        if (logged)
            return;
        logged = true;
        _logger.WriteLine($"[gbfr.qol.buildcard] {message}", color);
    }
}
