using System.Diagnostics;
using System.Drawing;

using NenTools.Reloaded.ScanManager.Interfaces;
using Reloaded.Mod.Interfaces;

namespace gbfr.qol.buildcard.Hooks;

// Finds UI objects and their components and shows or hides objects.
public unsafe class UiObjects
{
    // object
    private const int Children = 0x10;
    private const int Components = 0x28;
    private const int NameHash = 0x1C4;
    private const int Id = 0x1CC;
    private const int Active = 0x1D0;

    // component
    private const int ComponentObject = 0x10;

    // component entry
    private const int EntrySize = 0x20;
    private const int EntryComponent = 0x18;

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
    public Dictionary<int, nint>? Find(nint charaInfo)
    {
        if (!IsOnCard(charaInfo))
            return null;

        nint root = ObjectOf(charaInfo);
        var objects = new Dictionary<int, nint>();
        var pending = new Stack<nint>([root]);
        for (int visited = 0; pending.Count > 0 && visited < MaxObjects; visited++)
        {
            nint obj = pending.Pop();
            objects[*(int*)(obj + Id)] = obj;
            PushChildren(pending, obj);
        }
        return objects;
    }

    public bool IsOnCard(nint charaInfo)
    {
        nint root = ObjectOf(charaInfo);
        return root != 0 && *(uint*)(root + NameHash) == Status01;
    }

    // The objects under root, root included, whose name hash is one of names
    public List<nint> FindByName(nint root, params uint[] names)
    {
        var found = new List<nint>();
        var pending = new Stack<nint>([root]);
        for (int visited = 0; pending.Count > 0 && visited < MaxObjects; visited++)
        {
            nint obj = pending.Pop();
            if (names.Contains(*(uint*)(obj + NameHash)))
                found.Add(obj);
            PushChildren(pending, obj);
        }
        return found;
    }

    private static void PushChildren(Stack<nint> pending, nint obj)
    {
        if (!GameMemory.IsVector(obj + Children, sizeof(nint)))
            return;
        for (nint child = *(nint*)(obj + Children); child < *(nint*)(obj + Children + 8); child += 8)
        {
            if (*(nint*)child != 0)
                pending.Push(*(nint*)child);
        }
    }

    public nint ObjectOf(nint component) => *(nint*)(component + ComponentObject);

    public void SetActive(nint obj, bool active)
    {
        if (_setActive != null)
            _setActive(obj, active ? (byte)1 : (byte)0);
    }

    public bool IsActive(nint obj) => *(byte*)(obj + Active) != 0;

    // The object's component of the RTTI type, or 0
    public nint FindComponent(nint obj, string typeName)
    {
        nint vtable = _vtables.Find(typeName);
        if (vtable == 0 || !GameMemory.IsVector(obj + Components, EntrySize))
            return 0;
        for (nint entry = *(nint*)(obj + Components); entry < *(nint*)(obj + Components + 8); entry += EntrySize)
        {
            nint component = *(nint*)(entry + EntryComponent);
            if (component != 0 && *(nint*)component == vtable)
                return component;
        }
        LogComponentsOnce(obj, typeName, vtable);
        return 0;
    }

    private void LogComponentsOnce(nint obj, string typeName, nint vtable)
    {
        if (_loggedComponents)
            return;
        _loggedComponents = true;
        var vtables = new List<string>();
        for (nint entry = *(nint*)(obj + Components); entry < *(nint*)(obj + Components + 8); entry += EntrySize)
        {
            nint component = *(nint*)(entry + EntryComponent);
            vtables.Add(component == 0 ? "null" : $"exe+{*(nint*)component - _exeBase:X}");
        }
        _logger.WriteLine($"[gbfr.qol.buildcard] No {typeName} on an object (vtable exe+{vtable - _exeBase:X}); components: {string.Join(", ", vtables)}", Color.Yellow);
    }
}
