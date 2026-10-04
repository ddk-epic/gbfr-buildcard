using GBFRDataTools.Files.UI.Types;

namespace gbfr.uitools.Components.Controllers.Equip;

// ui::component::ControllerEquipWeaponInfo
// Adds fields missing from GBFRDataTools' class.
public class ControllerEquipWeaponInfo : GBFRDataTools.Files.UI.Components.Controllers.Pause.Equip.ControllerEquipWeaponInfo
{
    public UIObjectRef RebuildWeaponInfo { get; set; }
}
