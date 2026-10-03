using GBFRDataTools.Files.UI.Types;

namespace gbfr.uitools.Components.Infos;

// ui::component::SkillInfo
// Adds fields missing from GBFRDataTools' class.
public class SkillInfo : GBFRDataTools.Files.UI.Components.Infos.SkillInfo
{
    public UIObjectRef ItemReplaceableSkillButton { get; set; }
    public UIObjectRef ItemSkillDifferenceButton { get; set; }
}
