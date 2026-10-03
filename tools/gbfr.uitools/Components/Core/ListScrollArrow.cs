using GBFRDataTools.Files.UI.Components;
using GBFRDataTools.Files.UI.Types;

namespace gbfr.uitools.Components.Core;

// ui::component::ListScrollArrow
// Replaces GBFRDataTools' class: Animator is a ref, not a bool.
public class ListScrollArrow : Component
{
    public UIObjectRef Animator { get; set; }
}
