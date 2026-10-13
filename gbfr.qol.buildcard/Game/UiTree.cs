using System.Diagnostics;
using System.Drawing;

using Reloaded.Mod.Interfaces;

using gbfr.qol.buildcard.Core;
using gbfr.qol.buildcard.Interop.Game;

namespace gbfr.qol.buildcard.Game;

// Finds UI objects and their components and shows or hides objects.
public unsafe class UiTree : IFeature
{
    private const int MaxObjects = 0x4000;
    private const uint FnvOffset = 0x811C9DC5, FnvPrime = 0x01000193;

    private readonly ILogger _logger;
    private readonly GameApi _api;
    private readonly nint _exeBase = Process.GetCurrentProcess().MainModule!.BaseAddress;
    private delegate* unmanaged<nint, byte, void> _setActive;
    private bool _loggedComponents;

    public UiTree(ILogger logger, GameApi api)
    {
        _logger = logger;
        _api = api;
    }

    public Capability[] Requires => [_api.SetObjectActive];

    public void Start() => _setActive = (delegate* unmanaged<nint, byte, void>)_api.SetObjectActive.Address;

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

    public bool IsOnCard(CharaInfo charaInfo) => charaInfo.Object is { NameHash: CardLayout.Status01.NameHash };

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

    // FNV-1a over each object's Id and name hash in Id order, as codegen.py; 0 for a missing Id
    public static uint PrefabHash(Dictionary<int, UiObject> objects)
    {
        uint hash = FnvOffset;
        for (int id = 0; id < objects.Count; id++)
        {
            if (!objects.TryGetValue(id, out UiObject obj))
                return 0;
            hash = Fnv(Fnv(hash, (uint)id), obj.NameHash);
        }
        return hash;
    }

    private static uint Fnv(uint hash, uint value)
    {
        for (int i = 0; i < 4; i++, value >>= 8)
            hash = (hash ^ (value & 0xFF)) * FnvPrime;
        return hash;
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

    // The object's component with the vtable, or null
    public UiComponent? FindComponent(UiObject obj, Capability vtable)
    {
        if (!vtable.Found)
            return null;
        for (int i = 0, count = obj.ComponentCount; i < count; i++)
        {
            if (obj.Component(i) is { } component && component.Vtable == vtable.Address)
                return component;
        }
        LogComponentsOnce(obj, vtable);
        return null;
    }

    private void LogComponentsOnce(UiObject obj, Capability vtable)
    {
        if (_loggedComponents)
            return;
        _loggedComponents = true;
        var vtables = new List<string>();
        for (int i = 0, count = obj.ComponentCount; i < count; i++)
            vtables.Add(obj.Component(i) is { } component ? $"exe+{component.Vtable - _exeBase:X}" : "null");
        _logger.WriteLine($"[gbfr.qol.buildcard] No {vtable.Name} component on an object (exe+{vtable.Address - _exeBase:X}); components: {string.Join(", ", vtables)}", Color.Yellow);
    }
}
