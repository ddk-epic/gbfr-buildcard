using GBFRDataTools.Files.UI.Types;

namespace gbfr.uitools.Components.Infos;

// ui::component::AbilityInfo
// Adds fields missing from GBFRDataTools' class.
public class AbilityInfo : GBFRDataTools.Files.UI.Components.Infos.AbilityInfo
{
    public List<UIObjectRef> Mode { get; set; }
}
