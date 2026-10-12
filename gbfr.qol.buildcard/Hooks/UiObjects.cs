using System.Diagnostics;
using System.Drawing;

using NenTools.Reloaded.ScanManager.Interfaces;
using Reloaded.Mod.Interfaces;

namespace gbfr.qol.buildcard.Hooks;

// Finds UI objects and their components and shows or hides objects.
public unsafe class UiObjects
{
    private const uint Status01 = 0xD54E236E;  // name hash
    private const int MaxObjects = 0x4000;

    private readonly ILogger _logger;
    private readonly Vtables _vtables;
    private readonly nint _exeBase = Process.GetCurrentProcess().MainModule!.BaseAddress;
    private delegate* unmanaged<nint, byte, void> _setActive;
    private bool _loggedComponents;

    public UiObjects(ILogger logger, Vtables vtables)
    {
        _logger = logger;
        _vtables = vtables;
    }

    public void Init(IScanManager scanManager, string signatureGroup)
    {
        scanManager.AddScan("SetObjectActive", signatureGroup, address =>
            _setActive = (delegate* unmanaged<nint, byte, void>)(nint)address);
    }

    // the objects by Id, or null when CharaInfo is not on status01
    public Dictionary<int, UiObject>? Find(CharaInfo charaInfo)
    {
        if (!IsOnCard(charaInfo))
            return null;

        var objects = new Dictionary<int, UiObject>();
        var pending = new Stack<UiObject>([charaInfo.Object!.Value]);
        for (int visited = 0; pending.Count > 0 && visited < MaxObjects; visited++)
        {
            var obj = pending.Pop();
            objects[obj.Id] = obj;
            PushChildren(pending, obj);
        }
        return objects;
    }

    public bool IsOnCard(CharaInfo charaInfo) => charaInfo.Object is { NameHash: Status01 };

    // The objects under root, root included, whose name hash is one of names
    public List<UiObject> FindByName(UiObject root, params uint[] names)
    {
        var found = new List<UiObject>();
        var pending = new Stack<UiObject>([root]);
        for (int visited = 0; pending.Count > 0 && visited < MaxObjects; visited++)
        {
            var obj = pending.Pop();
            if (names.Contains(obj.NameHash))
                found.Add(obj);
            PushChildren(pending, obj);
        }
        return found;
    }

    private static void PushChildren(Stack<UiObject> pending, UiObject obj)
    {
        for (int i = 0, count = obj.ChildCount; i < count; i++)
        {
            if (obj.Child(i) is { } child)
                pending.Push(child);
        }
    }

    public void SetActive(UiObject obj, bool active)
    {
        if (_setActive != null)
            _setActive(obj.Address, active ? (byte)1 : (byte)0);
    }

    // The object's component of the RTTI type, or null
    public UiComponent? FindComponent(UiObject obj, string typeName)
    {
        nint vtable = _vtables.Find(typeName);
        if (vtable == 0)
            return null;
        for (int i = 0, count = obj.ComponentCount; i < count; i++)
        {
            if (obj.Component(i) is { } component && component.Vtable == vtable)
                return component;
        }
        LogComponentsOnce(obj, typeName, vtable);
        return null;
    }

    private void LogComponentsOnce(UiObject obj, string typeName, nint vtable)
    {
        if (_loggedComponents)
            return;
        _loggedComponents = true;
        var vtables = new List<string>();
        for (int i = 0, count = obj.ComponentCount; i < count; i++)
            vtables.Add(obj.Component(i) is { } component ? $"exe+{component.Vtable - _exeBase:X}" : "null");
        _logger.WriteLine($"[gbfr.qol.buildcard] No {typeName} on an object (vtable exe+{vtable - _exeBase:X}); components: {string.Join(", ", vtables)}", Color.Yellow);
    }
}
