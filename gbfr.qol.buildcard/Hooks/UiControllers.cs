using Reloaded.Hooks.Definitions;

namespace gbfr.qol.buildcard.Hooks;

// Raises Destroyed before a UI controller is destroyed.
public class UiControllers
{
    // vtable index of the scalar deleting destructor
    private const int DestroyIndex = 7;
    private const string DestroyStart = "56 57 48 83 EC 28 ?? ?? ?? ?? ?? E8";

    // ControllerStatusBg's destructor is also ControllerStatusGuide's and ControllerInformationSystem's
    private static readonly string[] Destructors =
    [
        ".?AVControllerStatusBg@component@ui@@",
        ".?AVControllerInformationToast@component@ui@@",
    ];

    private readonly Vtables _vtables;
    private readonly List<IHook<DestroyFn>> _destroyHooks = [];

    // Raised on the game thread.
    public event Action<UiComponent>? Destroyed;

    private delegate nint DestroyFn(nint controller, int flags);

    public UiControllers(Vtables vtables)
    {
        _vtables = vtables;
    }

    public bool IsHooked => _destroyHooks.Count == Destructors.Length;

    // Hooks all destructors or none
    public void Init()
    {
        if (Destructors.Any(type => _vtables.Function(type, DestroyIndex, DestroyStart) == 0))
            return;
        foreach (string type in Destructors)
        {
            IHook<DestroyFn>? hook = null;
            hook = _vtables.HookFunction<DestroyFn>(type, DestroyIndex, DestroyStart,
                (controller, flags) => DestroyImpl(hook!, controller, flags));
            if (hook != null)
                _destroyHooks.Add(hook);
        }
    }

    private nint DestroyImpl(IHook<DestroyFn> hook, nint controller, int flags)
    {
        Destroyed?.Invoke(new UiComponent(controller));
        return hook.OriginalFunction(controller, flags);
    }
}
