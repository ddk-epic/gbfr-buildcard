#nullable disable

using System.Numerics;
using System.Runtime.InteropServices;

using NenTools.Reloaded.ScanManager.Interfaces;
using Reloaded.Hooks.Definitions;

using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace gbfr.qol.buildcard.Hooks.Reflection;

// Collects the game's reflected object types and dumps them as C# classes.
// Ported from Nenkai's gbfr.utility.modtools (MIT); dumps to a file instead of the overlay log.
public unsafe class ReflectionHooks
{
    private readonly IScanManager _scanManager;
    private readonly IReloadedHooks _hooks;
    private TextWriter _out = TextWriter.Null;

    private readonly static Dictionary<string, RelinkObjectType> _knownObjects = new();
    public int ObjectCount => _knownObjects.Count;
    public bool HasLoadedObjects { get; private set; } = false;

    private nint _charaParameterBaseList;

    public static IHook<RegisterReflectionObjectDelegate> HOOK_ReflectionAddObject { get; private set; }

    public static IHook<CharaParameterBaseList__StaticCtor> FUNC_CharaParameterBaseList__StaticCtor { get; private set; }
    public static IHook<CharaParameterBaseList__GetByName> FUNC_CharaParameterBaseList__GetByName { get; private set; }

    public static IHook<RegisterBehaviorTreeComponentFactories> HOOK_RegisterBehaviorTreeComponentFactories { get; private set; }
    public static BehaviorTreeComponentObjectExists FUNC_BehaviorTreeComponentObjectExists { get; private set; }
    public static GetNewBehaviorTreeComponentByName FUNC_GetNewBehaviorTreeComponentByName { get; private set; }

    public delegate ulong RegisterReflectionObjectDelegate(ObjectDef* objectDef);

    public delegate ulong CharaParameterBaseList__StaticCtor(void* list, uint* nameHash, delegate* unmanaged<ulong*> createCallback);
    public delegate ulong CharaParameterBaseList__GetByName(nint list, nint outputPtr, string namePtr);

    public delegate nint RegisterBehaviorTreeComponentFactories(nint a1);
    public delegate bool BehaviorTreeComponentObjectExists(string name);
    public delegate nint GetNewBehaviorTreeComponentByName(string name);

    public ReflectionHooks(IScanManager scanManager, IReloadedHooks hooks)
    {
        _scanManager = scanManager;
        _hooks = hooks;
    }

    public void Init(string groupSource)
    {
        // Character object params are created separately.
        _scanManager.AddScan(nameof(CharaParameterBaseList__StaticCtor), groupSource, result
            => FUNC_CharaParameterBaseList__StaticCtor = _hooks.CreateHook<CharaParameterBaseList__StaticCtor>(CharaParameterBaseList__StaticCtorImpl, result).Activate());
        _scanManager.AddScan(nameof(CharaParameterBaseList__GetByName), groupSource, result
            => FUNC_CharaParameterBaseList__GetByName = _hooks.CreateHook<CharaParameterBaseList__GetByName>(CharaParameterBaseList__GetByNameImpl, result).Activate());

        _scanManager.AddScan(nameof(RegisterBehaviorTreeComponentFactories), groupSource, result
            => HOOK_RegisterBehaviorTreeComponentFactories = _hooks.CreateHook<RegisterBehaviorTreeComponentFactories>(ReflectionRegisterObjectFactoriesImpl, result).Activate());
        _scanManager.AddScan(nameof(RegisterReflectionObjectDelegate), groupSource, result
            => HOOK_ReflectionAddObject = _hooks.CreateHook<RegisterReflectionObjectDelegate>(ss__reflection__AddObjectImpl, result).Activate());

        _scanManager.AddScan(nameof(BehaviorTreeComponentObjectExists), groupSource, result
            => FUNC_BehaviorTreeComponentObjectExists = _hooks.CreateWrapper<BehaviorTreeComponentObjectExists>(result, out _));
        _scanManager.AddScan(nameof(GetNewBehaviorTreeComponentByName), groupSource, result
            => FUNC_GetNewBehaviorTreeComponentByName = _hooks.CreateWrapper<GetNewBehaviorTreeComponentByName>(result, out _));
    }

    // Param objects (i.e. Em0001Param) only get reflection info once created, so create each one as it registers.
    // nameHash is XXHash32.
    private unsafe ulong CharaParameterBaseList__StaticCtorImpl(void* list, uint* nameHash, delegate* unmanaged<ulong*> createCallback)
    {
        var obj = createCallback(); // Calls AddObject, which is hooked.
        return FUNC_CharaParameterBaseList__StaticCtor.OriginalFunction(list, nameHash, createCallback);
    }

    private unsafe ulong CharaParameterBaseList__GetByNameImpl(nint listPtr, nint outputPtr, string name) // output ptr points to 0x00 object ptr, 0x08 = refcounter?
    {
        _charaParameterBaseList = listPtr;
        return FUNC_CharaParameterBaseList__GetByName.OriginalFunction(listPtr, outputPtr, name);
    }

    // Marks when everything is registered; ReflectionHasObjectByName segfaults before that.
    private nint ReflectionRegisterObjectFactoriesImpl(nint a1)
    {
        var res = HOOK_RegisterBehaviorTreeComponentFactories.OriginalFunction(a1);
        HasLoadedObjects = true;
        return res;
    }

    // Registers a new reflected object.
    private unsafe ulong ss__reflection__AddObjectImpl(ObjectDef* objectDef)
    {
        var res = HOOK_ReflectionAddObject.OriginalFunction(objectDef);

        lock (_knownObjects)
        {
            string objectName = Marshal.PtrToStringAnsi((nint)objectDef->pObjectType->pName);
            if (!_knownObjects.ContainsKey(objectName))
                RegisterType(objectDef->pObjectType);
        }

        return res;
    }

    public void DumpAll(string path)
    {
        using var writer = new StreamWriter(path, append: false);
        _out = writer;

        // Iterate in steps, as dumping may append to _knownObjects.
        int i = 0;
        while (true)
        {
            int n = _knownObjects.Count;
            for (; i < n; i++)
            {
                var objInfo = _knownObjects.ElementAt(i);
                DumpObjectReflectionInfo(objInfo.Value);
            }

            if (i == n)
                break;
        }

        _out = TextWriter.Null;
    }

    private void RegisterType(IObjectType* objectType)
    {
        string objectName = Marshal.PtrToStringAnsi((nint)objectType->pName);
        string inheritName = Marshal.PtrToStringAnsi((nint)objectType->pTypeName);

        var relinkObjectType = new RelinkObjectType();
        relinkObjectType.Name = objectName;
        relinkObjectType.InheritName = inheritName;
        relinkObjectType.ReflectionTypeInfoPtr = objectType;

        IAttributeList* attrList = objectType->pAttrList;
        int count = (int)(attrList->pEnd - attrList->pBegin);
        for (int i = 0; i < count; i++)
        {
            IAttribute* attr = attrList->pBegin[i];

            if (attr->field_0x08 == 1) // Swapped? weird
            {
                var attrName = Marshal.PtrToStringAnsi((nint)attr->pTypeName);
                var attrTypeName = Marshal.PtrToStringAnsi((nint)attr->pAttrName);

                relinkObjectType.Attributes.TryAdd(attrName, new RelinkObjectAttribute(attrName, attrTypeName, attr));
            }
            else
            {
                var attrName = Marshal.PtrToStringAnsi((nint)attr->pAttrName);
                var attrTypeName = Marshal.PtrToStringAnsi((nint)attr->pTypeName);

                relinkObjectType.Attributes.TryAdd(attrName, new RelinkObjectAttribute(attrName, attrTypeName, attr));
            }
        }

        _knownObjects.Add(objectName, relinkObjectType);
    }

    private void DumpObjectReflectionInfo(RelinkObjectType objectType)
    {
        nint defaultObjectPtr = 0;
        if (HasLoadedObjects && FUNC_BehaviorTreeComponentObjectExists(objectType.Name))
        {
            // This may create nested objects.
            defaultObjectPtr = FUNC_GetNewBehaviorTreeComponentByName(objectType.Name);
        }
        else if (_charaParameterBaseList != 0 && objectType.Name.EndsWith("Param"))
        {
            nint charaPtr = Marshal.AllocHGlobal(0x10);
            FUNC_CharaParameterBaseList__GetByName.OriginalFunction(_charaParameterBaseList, charaPtr, objectType.Name);
            if (*(nint*)charaPtr != 0)
                defaultObjectPtr = *(nint*)charaPtr;

            Marshal.FreeHGlobal(charaPtr);
        }

        if (!string.IsNullOrEmpty(objectType.InheritName))
            _out.WriteLine($"public class {objectType.Name} : {objectType.InheritName}");
        else
            _out.WriteLine($"public class {objectType.Name}");

        _out.WriteLine("{");

        RelinkObjectType inheritType = null;
        if (!string.IsNullOrWhiteSpace(objectType.InheritName))
            _knownObjects.TryGetValue(objectType.InheritName, out inheritType);

        _out.WriteLine("    [JsonIgnore]");
        _out.WriteLine($"    public override string ComponentName => nameof({objectType.Name});");
        _out.WriteLine("");

        foreach (KeyValuePair<string, RelinkObjectAttribute> attr in objectType.Attributes)
        {
            if (inheritType is null || !inheritType.Attributes.ContainsKey(attr.Key))
                AddProperty(attr.Value, defaultObjectPtr);
        }

        // Constructor with attributes from the inherited class.
        _out.WriteLine($"    public {objectType.Name}()");
        _out.WriteLine("    {");

        if (inheritType is not null)
        {
            foreach (KeyValuePair<string, RelinkObjectAttribute> attr in inheritType.Attributes)
            {
                string valStr = GetValueStr(attr.Value, defaultObjectPtr);
                if (!string.IsNullOrEmpty(valStr))
                {
                    string humanizedAttrName = attr.Key;
                    if (attr.Key.EndsWith('_'))
                    {
                        humanizedAttrName = attr.Key.Substring(0, attr.Key.Length - 1);
                        humanizedAttrName = FirstCharToUpper(humanizedAttrName);
                    }

                    _out.WriteLine($"        {humanizedAttrName}{valStr}");
                }
            }
        }
        _out.WriteLine("    }");

        _out.WriteLine("}");
        _out.WriteLine("\n");
    }

    private void AddProperty(RelinkObjectAttribute attribute, nint defaultObjectPtr)
    {
        string humanizedAttrName = attribute.Name;
        if (attribute.Name.EndsWith('_'))
            humanizedAttrName = attribute.Name.Substring(0, attribute.Name.Length - 1);

        humanizedAttrName = FirstCharToUpper(humanizedAttrName);
        string valueStr = GetValueStr(attribute, defaultObjectPtr);

        _out.WriteLine($"    [JsonPropertyName(\"{attribute.Name}\")]");
        _out.WriteLine($"    public {TypeToCSharpType(attribute.Type)} {humanizedAttrName} {{ get; set; }}{valueStr} " +
            $"// Offset 0x{(attribute.AttributePtr->field_0x08 == 1 ? attribute.AttributePtr->field_0x38 : attribute.AttributePtr->dwOffset):X}");

        _out.WriteLine("");
    }

    private static string GetValueStr(RelinkObjectAttribute attribute, nint defaultObjectPtr)
    {
        string csharpTypeName = TypeToCSharpType(attribute.Type);
        string valueStr = "";
        if (defaultObjectPtr != 0)
        {
            nint fieldOffset = defaultObjectPtr + attribute.AttributePtr->dwOffset;
            switch (csharpTypeName)
            {
                case "bool":
                    valueStr = $" = {(*(bool*)fieldOffset).ToString().ToLower()};";
                    break;
                case "short":
                    valueStr = $" = {*(short*)fieldOffset};";
                    break;
                case "sbyte":
                    valueStr = $" = {*(sbyte*)fieldOffset};";
                    break;
                case "byte":
                    valueStr = $" = {*(byte*)fieldOffset};";
                    break;
                case "float":
                    valueStr = $" = {*(float*)fieldOffset}f;";
                    break;
                case "int":
                    valueStr = $" = {*(int*)fieldOffset};";
                    break;
                case "uint":
                    {
                        uint val = *(uint*)fieldOffset;
                        valueStr = val == 0xFFFFFFFF ? $" = 0x{val:X};" : $" = {val};";
                    }
                    break;
                case "Vector4":
                    {
                        Vector4 val = *(Vector4*)fieldOffset;
                        valueStr = $" = new Vector4({val.X}f, {val.Y}f, {val.Z}f, {val.W}f);";
                    }
                    break;
                case "Vector3":
                    {
                        Vector3 val = *(Vector3*)fieldOffset;
                        valueStr = $" = new Vector3({val.X}f, {val.Y}f, {val.Z}f);";
                    }
                    break;
                case "Vector2":
                    {
                        Vector2 val = *(Vector2*)fieldOffset;
                        valueStr = $" = new Vector2({val.X}f, {val.Y}f);";
                    }
                    break;
            }
        }

        return valueStr;
    }

    public static string TypeToCSharpType(string type)
    {
        return type switch
        {
            "bool" => "bool",
            "cVec4" => "Vector4",
            "cVec3" => "Vector3",
            "cVec2" => "Vector2",
            "s16" => "short",
            "s8" => "sbyte",
            "u8" => "byte",
            "f32" => "float",
            "s32" => "int",
            "u32" => "uint",
            _ => type,
        };
    }

    public static string FirstCharToUpper(string input) =>
        input switch
        {
            null => throw new ArgumentNullException(nameof(input)),
            "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
            _ => string.Concat(input[0].ToString().ToUpper(), input.AsSpan(1))
        };
}

public class RelinkObjectType
{
    public string Name { get; set; }
    public string InheritName { get; set; }

    public unsafe IObjectType* ReflectionTypeInfoPtr { get; set; }
    public Dictionary<string, RelinkObjectAttribute> Attributes { get; set; } = [];
}

public class RelinkObjectAttribute
{
    public string Name { get; set; }
    public string Type { get; set; }
    public unsafe IAttribute* AttributePtr { get; set; }

    public unsafe RelinkObjectAttribute(string name, string type, IAttribute* attributePtr)
    {
        Name = name;
        Type = type;
        AttributePtr = attributePtr;
    }
}
