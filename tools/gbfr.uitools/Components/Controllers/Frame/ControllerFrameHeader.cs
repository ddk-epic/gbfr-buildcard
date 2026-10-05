using GBFRDataTools.Files.UI.Types;

namespace gbfr.uitools.Components.Controllers.Frame;

// ui::component::ControllerFrameHeader
// Adds fields missing from GBFRDataTools' class.
public class ControllerFrameHeader : GBFRDataTools.Files.UI.Components.Controllers.Pause.Frame.ControllerFrameHeader
{
    public List<UIObjectRef> AnimationType { get; set; }
}
