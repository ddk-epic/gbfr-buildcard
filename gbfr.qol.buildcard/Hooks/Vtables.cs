using System.Diagnostics;
using System.Drawing;

using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;

using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace gbfr.qol.buildcard.Hooks;

// Finds game classes' vtables through the exe's RTTI and hooks their virtual functions.
public unsafe class Vtables
{
    private readonly IReloadedHooks _hooks;
    private readonly ILogger _logger;
    private readonly nint _exeBase = Process.GetCurrentProcess().MainModule!.BaseAddress;
    private readonly Dictionary<string, nint> _found = [];

    public Vtables(IReloadedHooks hooks, ILogger logger)
    {
        _hooks = hooks;
        _logger = logger;
    }

    // The vtable of an RTTI type name such as ".?AVText@component@ui@@", or 0
    public nint Find(string typeName)
    {
        if (_found.TryGetValue(typeName, out nint vtable))
            return vtable;
        vtable = PeImage.FindVtable(_exeBase, typeName);
        if (vtable == 0)
            _logger.WriteLine($"[gbfr.qol.buildcard] {ClassName(typeName)} vtable not found", Color.Red);
        _found[typeName] = vtable;
        return vtable;
    }

    // The virtual function at index, or 0 when its code does not start with start
    public nint Function(string typeName, int index, string? start)
    {
        nint vtable = Find(typeName);
        if (vtable == 0)
            return 0;
        nint function = *(nint*)(vtable + index * sizeof(nint));
        if (start != null && !PeImage.Matches(function, start))
        {
            _logger.WriteLine($"[gbfr.qol.buildcard] {ClassName(typeName)} function {index} not found", Color.Red);
            return 0;
        }
        return function;
    }

    // Hooks the virtual function's code, run by every class sharing it
    public IHook<T>? HookFunction<T>(string typeName, int index, string? start, T impl)
    {
        nint function = Function(typeName, index, start);
        return function == 0 ? null : _hooks.CreateHook(impl, function).Activate();
    }

    private static string ClassName(string typeName) => typeName[4..typeName.IndexOf('@')];
}
