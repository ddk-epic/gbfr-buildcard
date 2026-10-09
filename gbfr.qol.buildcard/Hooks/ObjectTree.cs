namespace gbfr.qol.buildcard.Hooks;

// Finds status01's objects by their Id, walking the tree from the object CharaInfo is on.
public static unsafe class ObjectTree
{
    // docs/runtime-data.md
    private const int Owner = 0x10;
    private const int Children = 0x10;
    private const int NameHash = 0x1C4;
    private const int Id = 0x1CC;
    private const uint Status01 = 0xD54E236E;  // name hash
    private const int MaxObjects = 0x4000;

    // the objects by Id, or null when CharaInfo isn't on status01
    public static Dictionary<int, nint>? Find(nint charaInfo)
    {
        nint root = *(nint*)(charaInfo + Owner);
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
}
