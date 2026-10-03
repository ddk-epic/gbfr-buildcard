using GBFRDataTools.Files.UI.Types;

namespace gbfr.uitools.Components.Infos;

// ui::component::CharaInfo
// Adds fields missing from GBFRDataTools' class.
public class CharaInfo : GBFRDataTools.Files.UI.Components.Infos.CharaInfo
{
    public UIObjectRef CrossPlayInfo { get; set; }
    public List<UIObjectRef> _D47C490A { get; set; }
}
