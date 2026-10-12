namespace gbfr.qol.buildcard.Hooks;

public readonly unsafe record struct UiObject(nint Address)
{
    private const int ChildrenOffset = 0x10;
    private const int ComponentsOffset = 0x28;
    private const int NameHashOffset = 0x1C4;
    private const int IdOffset = 0x1CC;
    private const int ActiveOffset = 0x1D0;

    // component entry
    private const int EntrySize = 0x20;
    private const int EntryComponent = 0x18;

    public uint NameHash => *(uint*)(Address + NameHashOffset);
    public int Id => *(int*)(Address + IdOffset);
    public bool IsActive => *(byte*)(Address + ActiveOffset) != 0;

    // 0 when the children vector is broken
    public int ChildCount => VectorCount(ChildrenOffset, sizeof(nint));

    public UiObject? Child(int index) =>
        (*(nint**)(Address + ChildrenOffset))[index] is var child and not 0 ? new UiObject(child) : null;

    // 0 when the components vector is broken
    public int ComponentCount => VectorCount(ComponentsOffset, EntrySize);

    public UiComponent? Component(int index) =>
        *(nint*)(*(nint*)(Address + ComponentsOffset) + index * EntrySize + EntryComponent) is var component and not 0
            ? new UiComponent(component)
            : null;

    private int VectorCount(int offset, int elementSize)
    {
        nint* vector = (nint*)(Address + offset);
        return GameMemory.IsVector((nint)vector, elementSize) ? (int)((vector[1] - vector[0]) / elementSize) : 0;
    }
}

// A component on a UI object.
public readonly unsafe record struct UiComponent(nint Address)
{
    private const int ObjectOffset = 0x10;

    public nint Vtable => *(nint*)Address;

    public UiObject? Object => *(nint*)(Address + ObjectOffset) is var obj and not 0 ? new UiObject(obj) : null;
}

// A ui::component::Text component.
public readonly record struct TextComponent(nint Address);

// A ui::component::CharaInfo component.
public readonly record struct CharaInfo(nint Address)
{
    public UiObject? Object => new UiComponent(Address).Object;
}
