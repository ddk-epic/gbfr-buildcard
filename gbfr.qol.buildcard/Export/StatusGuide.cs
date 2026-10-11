using System.Diagnostics;
using System.Drawing;

using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;

using gbfr.qol.buildcard.Hooks;

using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace gbfr.qol.buildcard.Export;

// Hides and shows the page arrows' button prompts (status_guide01), found through their controller's setup.
public unsafe class StatusGuide
{
    // ControllerStatusGuide's vtable index of SetUpPageArrows
    private const int SetUpPageArrowsIndex = 21;
    private const string SetUpPageArrowsStart = "56 57 48 83 EC 78 48 89 CE 48 81 C1 A0 01 00 00";

    // name hashes of loc_button_system01_l and loc_button_system01_r
    private const uint ButtonLeft = 0x6030E86D;
    private const uint ButtonRight = 0xFB687EBB;

    private readonly IReloadedHooks _hooks;
    private readonly ILogger _logger;
    private readonly UiObjects _objects;
    private List<nint> _buttons = [];

    private delegate void SetUpPageArrowsFn(nint controller);
    private IHook<SetUpPageArrowsFn>? _setUpPageArrowsHook;

    public StatusGuide(IReloadedHooks hooks, ILogger logger, UiObjects objects)
    {
        _hooks = hooks;
        _logger = logger;
        _objects = objects;
    }

    public void Init()
    {
        nint vtable = PeImage.FindVtable(Process.GetCurrentProcess().MainModule!.BaseAddress,
            ".?AVControllerStatusGuide@component@ui@@");
        if (vtable == 0)
        {
            _logger.WriteLine("[gbfr.qol.buildcard] ControllerStatusGuide vtable not found", Color.Red);
            return;
        }
        nint setUpPageArrows = *(nint*)(vtable + SetUpPageArrowsIndex * 8);
        if (!PeImage.Matches(setUpPageArrows, SetUpPageArrowsStart))
        {
            _logger.WriteLine("[gbfr.qol.buildcard] ControllerStatusGuide SetUpPageArrows not found", Color.Red);
            return;
        }
        _setUpPageArrowsHook = _hooks.CreateHook<SetUpPageArrowsFn>(SetUpPageArrowsImpl, setUpPageArrows).Activate();
    }

    public void Show(bool shown)
    {
        foreach (nint button in _buttons)
            _objects.SetActive(button, shown);
    }

    private void SetUpPageArrowsImpl(nint controller)
    {
        _setUpPageArrowsHook!.OriginalFunction(controller);
        nint obj = _objects.ObjectOf(controller);
        _buttons = obj == 0 ? [] : _objects.FindByName(obj, ButtonLeft, ButtonRight);
    }
}
