using GBFRDataTools.Files.UI.Types;

namespace gbfr.uitools.Components.Infos;

// ui::component::WeaponInfo
// Adds fields missing from GBFRDataTools' class.
public class WeaponInfo : GBFRDataTools.Files.UI.Components.Infos.WeaponInfo
{
    public List<UIObjectRef> _3A835D43 { get; set; }
    public List<UIObjectRef> _A06B2ED6 { get; set; }
    public List<UIObjectRef> Images { get; set; }
    public UIObjectRef SkillList { get; set; }
}
