using System.Runtime.InteropServices;

using gbfr.qol.buildcard.Interop.Native;

namespace gbfr.qol.buildcard.Interop.Game;

public readonly unsafe record struct UiObject(nint Address)
{
    private ref UiObjectData Data => ref *(UiObjectData*)Address;

    public uint NameHash => Data.NameHash;
    public int Id => Data.Id;
    public bool IsActive => Data.Active != 0;

    // 0 when the children vector is broken
    public int ChildCount => Data.Children.IsValid ? Data.Children.Count : 0;

    public UiObject? Child(int index) => Data.Children[index] is var child and not 0 ? new UiObject(child) : null;

    // 0 when the components vector is broken
    public int ComponentCount => Data.Components.IsValid ? Data.Components.Count : 0;

    public UiComponent? Component(int index) =>
        Data.Components[index].Component is var component and not 0 ? new UiComponent(component) : null;
}

// A component on a UI object.
public readonly unsafe record struct UiComponent(nint Address)
{
    private ref UiComponentData Data => ref *(UiComponentData*)Address;

    public nint Vtable => Data.Vtable;

    public UiObject? Object => Data.Object is var obj and not 0 ? new UiObject(obj) : null;
}

// A ui::component::Text component.
public readonly record struct TextComponent(nint Address);

// A ui::component::CharaInfo component.
public readonly unsafe record struct CharaInfo(nint Address)
{
    private ref CharaInfoData Data => ref *(CharaInfoData*)Address;

    public UiObject? Object => new UiComponent(Address).Object;

    // The first Names text, null when there is none
    public TextComponent? Name =>
        Data.Names is { IsValid: true, Count: > 0 } names && names[0].Component is var text and not 0 ? new TextComponent(text) : null;
}

[StructLayout(LayoutKind.Explicit)]
internal struct UiObjectData
{
    [FieldOffset(0x10)] public GameVector<nint> Children;
    [FieldOffset(0x28)] public GameVector<ComponentEntry> Components;
    [FieldOffset(0x1C4)] public uint NameHash;
    [FieldOffset(0x1CC)] public int Id;
    [FieldOffset(0x1D0)] public byte Active;
}

[StructLayout(LayoutKind.Explicit)]
internal struct CharaInfoData
{
    [FieldOffset(0x140)] public GameVector<ObjectRef> Names;
}

// A component's reference to another object's component.
[StructLayout(LayoutKind.Explicit, Size = 0x20)]
internal struct ObjectRef
{
    [FieldOffset(0x10)] public nint Component;
}

[StructLayout(LayoutKind.Explicit, Size = 0x20)]
internal struct ComponentEntry
{
    [FieldOffset(0x18)] public nint Component;
}

[StructLayout(LayoutKind.Explicit)]
internal struct UiComponentData
{
    [FieldOffset(0x0)] public nint Vtable;
    [FieldOffset(0x10)] public nint Object;
}
