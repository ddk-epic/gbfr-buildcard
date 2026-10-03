using GBFRDataTools.Files.UI.Components;
using GBFRDataTools.Files.UI.Types;

namespace gbfr.uitools.Components.Infos;

// ui::component::SkillStatusInfo
public class SkillStatusInfo : Component
{
    public List<UIObjectRef> Sets { get; set; }
    public List<UIObjectRef> Levels { get; set; }
    public List<UIObjectRef> Texts { get; set; }
    public List<UIObjectRef> Arrows { get; set; }
}
