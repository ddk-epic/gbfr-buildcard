using System.ComponentModel;

using gbfr.qol.buildcard.Template.Configuration;

namespace gbfr.qol.buildcard.Configuration;

public class Config : Configurable<Config>
{
    [DisplayName("Add Cards to Steam Screenshots")]
    [Description("Also adds each saved build card to the game's screenshots in Steam.")]
    [DefaultValue(false)]
    public bool SteamScreenshots { get; set; } = false;

    [DisplayName("Master Traits Palette")]
    [Description("The colours of the card's Master Traits section: Blue, matching the portrait, or the game's Original.")]
    [DefaultValue(MasterTraitsPalette.Blue)]
    public MasterTraitsPalette MasterTraitsPalette { get; set; } = MasterTraitsPalette.Blue;
}

public enum MasterTraitsPalette
{
    Blue,
    Original,
}

public class ConfiguratorMixin : ConfiguratorMixinBase
{
}
