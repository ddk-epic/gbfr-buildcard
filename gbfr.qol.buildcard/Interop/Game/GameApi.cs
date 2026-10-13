using NenTools.Reloaded.ScanManager.Interfaces;

using gbfr.qol.buildcard.Interop.Native;

namespace gbfr.qol.buildcard.Interop.Game;

// The game functions and vtables the mod uses.
public sealed class GameApi
{
    private const string SignatureGroup = "granblue_fantasy_relink_er";

    // scalar deleting destructor
    private const int DestroyIndex = 7;
    private const string DestroyStart = "56 57 48 83 EC 28 ?? ?? ?? ?? ?? E8";
    private const int ShowInfoIndex = 35;

    private const string ControllerStatusType = ".?AVControllerStatus@component@ui@@";
    private const string ControllerStatusBgType = ".?AVControllerStatusBg@component@ui@@";
    private const string ControllerStatusGuideType = ".?AVControllerStatusGuide@component@ui@@";
    private const string ControllerInformationSystemType = ".?AVControllerInformationSystem@component@ui@@";
    private const string ControllerInformationToastType = ".?AVControllerInformationToast@component@ui@@";
    private const string LoadWeaponPartyType = ".?AVLoadWeaponParty@icon@ui@@";
    private const string EnableSkinViewType = ".?AVEnableSkinView@action@fsm@ui@@";

    private readonly IScanManager _scanManager;
    private readonly Vtables _vtables;
    private int _pending = 1;  // the scans not found yet and the constructor

    // Signatures/granblue_fantasy_relink_er.ini
    public readonly Capability FillCharacterStatus;
    public readonly Capability SetObjectActive;
    public readonly Capability TextComponentSetText;
    public readonly Capability TextLookup;
    public readonly Capability TextJoinLines;
    public readonly Capability TextFitLine;
    public readonly Capability TextSplitLine;
    public readonly Capability TextBuildGlyphs;
    public readonly Capability TextInsertGlyphs;
    public readonly Capability SetSkillBoardDescription;
    public readonly Capability MasteryPercent;
    public readonly Capability SetMasteryPercents;
    public readonly Capability SetOverMasteryLine;
    public readonly Capability SetSummonInfo;
    public readonly Capability StartFlow;
    public readonly Capability EndFlow;
    public readonly Capability FindView;
    public readonly Capability OpenView;
    public readonly Capability CloseView;
    public readonly Capability SetCharacterDetailsFooter;
    public readonly Capability UpdateShortcutInput;
    public readonly Capability GetButtonBits;
    public readonly Capability PushInfo;

    // component vtables
    public readonly Capability TextVtable;
    public readonly Capability SummonInfoVtable;
    public readonly Capability LimitBonusInfoVtable;
    public readonly Capability ControllerStatusBgVtable;

    // virtual functions
    public readonly Capability ControllerStatusDestroy;
    public readonly Capability ControllerStatusBgDestroy;
    public readonly Capability ControllerInformationToastDestroy;
    public readonly Capability ControllerStatusBgSetUp;
    public readonly Capability ControllerStatusGuideSetUpPageArrows;
    public readonly Capability ControllerInformationSystemShowInfo;
    public readonly Capability ControllerInformationToastShowInfo;
    public readonly Capability LoadWeaponPartyIsOpen;
    public readonly Capability LoadWeaponPartyCollect;
    public readonly Capability EnableSkinViewApply;

    // Raised once every signature is found, on the thread that found the last one.
    public event Action? ScansFound;

    public bool AllScansFound => Volatile.Read(ref _pending) == 0;

    public GameApi(IScanManager scanManager, Vtables vtables)
    {
        _scanManager = scanManager;
        _vtables = vtables;

        FillCharacterStatus = Scan(nameof(FillCharacterStatus));
        SetObjectActive = Scan(nameof(SetObjectActive));
        TextComponentSetText = Scan(nameof(TextComponentSetText));
        TextLookup = Scan(nameof(TextLookup));
        TextJoinLines = Scan(nameof(TextJoinLines));
        TextFitLine = Scan(nameof(TextFitLine));
        TextSplitLine = Scan(nameof(TextSplitLine));
        TextBuildGlyphs = Scan(nameof(TextBuildGlyphs));
        TextInsertGlyphs = Scan(nameof(TextInsertGlyphs));
        SetSkillBoardDescription = Scan(nameof(SetSkillBoardDescription));
        MasteryPercent = Scan(nameof(MasteryPercent));
        SetMasteryPercents = Scan(nameof(SetMasteryPercents));
        SetOverMasteryLine = Scan(nameof(SetOverMasteryLine));
        SetSummonInfo = Scan(nameof(SetSummonInfo));
        StartFlow = Scan(nameof(StartFlow));
        EndFlow = Scan(nameof(EndFlow));
        FindView = Scan(nameof(FindView));
        OpenView = Scan(nameof(OpenView));
        CloseView = Scan(nameof(CloseView));
        SetCharacterDetailsFooter = Scan(nameof(SetCharacterDetailsFooter));
        UpdateShortcutInput = Scan(nameof(UpdateShortcutInput));
        GetButtonBits = Scan(nameof(GetButtonBits));
        PushInfo = Scan(nameof(PushInfo));

        TextVtable = Vtable(".?AVText@component@ui@@");
        SummonInfoVtable = Vtable(".?AVSummonInfo@component@ui@@");
        LimitBonusInfoVtable = Vtable(".?AVLimitBonusInfo@component@ui@@");
        ControllerStatusBgVtable = Vtable(ControllerStatusBgType);

        ControllerStatusDestroy = Function(ControllerStatusType, DestroyIndex, DestroyStart);
        ControllerStatusBgDestroy = Function(ControllerStatusBgType, DestroyIndex, DestroyStart);
        ControllerInformationToastDestroy = Function(ControllerInformationToastType, DestroyIndex, DestroyStart);
        // the controllers' shared setup
        ControllerStatusBgSetUp = Function(ControllerStatusBgType, 8, "41 57 41 56 56 57 53 48 83 EC 30");
        ControllerStatusGuideSetUpPageArrows = Function(ControllerStatusGuideType, 21,
            "56 57 48 83 EC 78 48 89 CE 48 81 C1 A0 01 00 00");
        ControllerInformationSystemShowInfo = Function(ControllerInformationSystemType, ShowInfoIndex,
            "55 41 57 41 56 41 55 41 54 56 57 53 48 81 EC 18 03 00 00");
        ControllerInformationToastShowInfo = Function(ControllerInformationToastType, ShowInfoIndex,
            "41 57 41 56 41 55 41 54 56 57 55 53 48 81 EC 78 01 00 00 48 89 D7");
        LoadWeaponPartyIsOpen = Function(LoadWeaponPartyType, 4, "48 8B 05 ?? ?? ?? ?? 48 8B 48 40 48 8B 50 48 48 39 D1");
        LoadWeaponPartyCollect = Function(LoadWeaponPartyType, 5, null);
        EnableSkinViewApply = Function(EnableSkinViewType, 9, "48 8B 05 ?? ?? ?? ?? 0F B6 51 30 88 90 13 11 00 00");
        Resolve();
    }

    private Capability Scan(string name)
    {
        var capability = new Capability(name);
        Interlocked.Increment(ref _pending);
        _scanManager.AddScan(name, SignatureGroup, address =>
        {
            capability.Address = (nint)address;
            Resolve();
        });
        return capability;
    }

    private void Resolve()
    {
        if (Interlocked.Decrement(ref _pending) == 0)
            ScansFound?.Invoke();
    }

    private Capability Vtable(string typeName) =>
        new($"{Vtables.ClassName(typeName)} vtable") { Address = _vtables.Find(typeName) };

    private Capability Function(string typeName, int index, string? start) =>
        new($"{Vtables.ClassName(typeName)} function {index}") { Address = _vtables.Function(typeName, index, start) };
}

// A game function or vtable, 0 until found.
public sealed class Capability
{
    public Capability(string name)
    {
        Name = name;
    }

    public string Name { get; }
    public nint Address { get; internal set; }
    public bool Found => Address != 0;
}
