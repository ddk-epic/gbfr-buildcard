using GBFRDataTools.Files.UI.Components.Controllers;
using GBFRDataTools.Files.UI.Types;

namespace gbfr.uitools.Components.Controllers.Status;

// ui::component::ControllerStatusGuide
public class ControllerStatusGuide : Controller
{
    public UIObjectRef PrevChara { get; set; }
    public UIObjectRef NextChara { get; set; }
    public UIObjectRef LeftArrow { get; set; }
    public UIObjectRef RightArrow { get; set; }
}
