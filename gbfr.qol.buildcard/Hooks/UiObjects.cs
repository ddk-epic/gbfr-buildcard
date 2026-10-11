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

    // component
    private const int ComponentObject = 0x10;

    // component entry
    private const int EntrySize = 0x20;
    private const int EntryComponent = 0x18;

    private const uint Status01 = 0xD54E236E;  // name hash
    private const int MaxObjects = 0x4000;

    private readonly ILogger _logger;
    private readonly nint _exeBase = Process.GetCurrentProcess().MainModule!.BaseAddress;
    private readonly Dictionary<nint, string> _typeNames = [];
    private delegate* unmanaged<nint, byte, void> _setActive;
    private bool _loggedComponents;

    public UiObjects(ILogger logger)
    {
        _logger = logger;
    }

    public void Init(IScanManager scanManager, string signatureGroup)
    {
        scanManager.AddScan("SetObjectActive", signatureGroup, address =>
            _setActive = (delegate* unmanaged<nint, byte, void>)(nint)address);
    }

    // the objects by Id, or null when CharaInfo is not on status01
    public Dictionary<int, nint>? Find(nint charaInfo)
    {
        nint root = ObjectOf(charaInfo);
        if (root == 0 || *(uint*)(root + NameHash) != Status01)
            return null;

        var objects = new Dictionary<int, nint>();
        var pending = new Stack<nint>([root]);
        while (pending.Count > 0 && objects.Count < MaxObjects)
        {
            nint obj = pending.Pop();
            objects[*(int*)(obj + Id)] = obj;
            for (nint child = *(nint*)(obj + Children); child < *(nint*)(obj + Children + 8); child += 8)
                pending.Push(*(nint*)child);
        }
        return objects;
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
            for (nint child = *(nint*)(obj + Children); child < *(nint*)(obj + Children + 8); child += 8)
                pending.Push(*(nint*)child);
        }
        return found;
    }

    public nint ObjectOf(nint component) => *(nint*)(component + ComponentObject);

    public void SetActive(nint obj, bool active)
    {
        if (_setActive != null)
            _setActive(obj, active ? (byte)1 : (byte)0);
    }

    // The vtable of the RTTI type, or 0
    public nint FindVtable(string typeName)
    {
        nint vtable = PeImage.FindVtable(_exeBase, typeName);
        if (vtable == 0)
            _logger.WriteLine($"[gbfr.qol.buildcard] {typeName} vtable not found", Color.Red);
        else
            _typeNames[vtable] = typeName;
        return vtable;
    }

    // The object's component with the vtable, or 0
    public nint FindComponent(nint obj, nint vtable)
    {
        if (vtable == 0)
            return 0;
        for (nint entry = *(nint*)(obj + Components); entry < *(nint*)(obj + Components + 8); entry += EntrySize)
        {
            nint component = *(nint*)(entry + EntryComponent);
            if (component != 0 && *(nint*)component == vtable)
                return component;
        }
        LogComponentsOnce(obj, vtable);
        return 0;
    }

    private void LogComponentsOnce(nint obj, nint vtable)
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
        _logger.WriteLine($"[gbfr.qol.buildcard] No {_typeNames.GetValueOrDefault(vtable)} on an object (vtable exe+{vtable - _exeBase:X}); components: {string.Join(", ", vtables)}", Color.Yellow);
    }
}
