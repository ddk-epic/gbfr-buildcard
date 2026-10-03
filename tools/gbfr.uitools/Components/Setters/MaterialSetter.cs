using GBFRDataTools.Files.UI.Components;
using GBFRDataTools.Files.UI.Types;

namespace gbfr.uitools.Components.Setters;

// ui::component::MaterialSetter
// Replaces GBFRDataTools' class: Materials are strings, not refs.
public class MaterialSetter : Component
{
    public UIObjectRef Target { get; set; }
    public UIObjectRef TargetRaw { get; set; }
    public List<string> Materials { get; set; }
}
