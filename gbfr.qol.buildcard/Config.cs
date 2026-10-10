using System.ComponentModel;

using gbfr.qol.buildcard.Template.Configuration;

namespace gbfr.qol.buildcard.Configuration;

public class Config : Configurable<Config>
{
    [DisplayName("Add Cards to Steam Screenshots")]
    [Description("Also adds each saved build card to the game's screenshots in Steam.")]
    [DefaultValue(false)]
    public bool SteamScreenshots { get; set; } = false;
}

public class ConfiguratorMixin : ConfiguratorMixinBase
{
}
