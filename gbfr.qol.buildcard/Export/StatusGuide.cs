using Reloaded.Hooks.Definitions;

using gbfr.qol.buildcard.Hooks;

namespace gbfr.qol.buildcard.Export;

// Hides and shows the page arrows' button prompts (status_guide01), found through their controller's setup.
public class StatusGuide : IOverlay
{
    private const string ControllerStatusGuide = ".?AVControllerStatusGuide@component@ui@@";
    // vtable index of SetUpPageArrows
    private const int SetUpPageArrowsIndex = 21;
    private const string SetUpPageArrowsStart = "56 57 48 83 EC 78 48 89 CE 48 81 C1 A0 01 00 00";

    // name hashes of loc_button_system01_l and loc_button_system01_r
    private const uint ButtonLeft = 0x6030E86D;
    private const uint ButtonRight = 0xFB687EBB;

    private readonly Vtables _vtables;
    private readonly UiControllers _controllers;
    private readonly UiObjects _objects;
    private readonly CharacterDetailsMenu _menu;
    private UiComponent? _controller;
    private List<UiObject> _buttons = [];

    private delegate void SetUpPageArrowsFn(nint controller);
    private IHook<SetUpPageArrowsFn>? _setUpPageArrowsHook;

    public StatusGuide(Vtables vtables, UiControllers controllers, UiObjects objects, CharacterDetailsMenu menu)
    {
        _vtables = vtables;
        _controllers = controllers;
        _objects = objects;
        _menu = menu;
        controllers.Destroyed += OnDestroyed;
    }

    public void Init()
    {
        if (!_controllers.IsHooked)
            return;
        _setUpPageArrowsHook = _vtables.HookFunction<SetUpPageArrowsFn>(ControllerStatusGuide, SetUpPageArrowsIndex,
            SetUpPageArrowsStart, SetUpPageArrowsImpl);
    }

    // Shows the prompts only on the status page
    public void Show(bool shown)
    {
        if (shown && !_menu.IsOnStatusPage)
            return;
        foreach (var button in _buttons)
            _objects.SetActive(button, shown);
    }

    private void SetUpPageArrowsImpl(nint controller)
    {
        _setUpPageArrowsHook!.OriginalFunction(controller);
        _controller = new UiComponent(controller);
        _buttons = _controller.Value.Object is { } obj ? _objects.FindByName(obj, ButtonLeft, ButtonRight) : [];
    }

    private void OnDestroyed(UiComponent controller)
    {
        if (controller != _controller)
            return;
        _controller = null;
        _buttons = [];
    }
}
