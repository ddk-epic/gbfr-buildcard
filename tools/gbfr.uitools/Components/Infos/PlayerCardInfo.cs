using GBFRDataTools.Files.UI.Types;

namespace gbfr.uitools.Components.Infos;

// ui::component::PlayerCardInfo
// Adds fields missing from GBFRDataTools' class.
public class PlayerCardInfo : GBFRDataTools.Files.UI.Components.Infos.PlayerCardInfo
{
    public UIObjectRef CrossPlayInfo { get; set; }
    public List<UIObjectRef> _B61460D1 { get; set; }
}
