using System.ComponentModel;

using gbfr.qol.buildcard.Template.Configuration;

namespace gbfr.qol.buildcard.Configuration;

public class Config : Configurable<Config>
{
    [DisplayName("Exported File Format")]
    [Description("PNG: lossless, ~3 MB. JPEG: ~1 MB.")]
    [DefaultValue(CardFormat.Png)]
    public CardFormat CardFormat { get; set; } = CardFormat.Png;

    [DisplayName("Master Traits Palette")]
    [Description("Blue: mod default. Original: the game's colours.")]
    [DefaultValue(MasterTraitsPalette.Blue)]
    public MasterTraitsPalette MasterTraitsPalette { get; set; } = MasterTraitsPalette.Blue;

    [DisplayName("Show Player Name")]
    [Description("Shows a player's name on the card in multiplayer.")]
    [DefaultValue(true)]
    public bool ShowPlayerName { get; set; } = true;

    [DisplayName("Enable Steam Screenshots")]
    [Description("Adds saved cards to Steam screenshots.")]
    [DefaultValue(false)]
    public bool SteamScreenshots { get; set; } = false;
}

public enum CardFormat
{
    Png,
    Jpeg,
}

public enum MasterTraitsPalette
{
    Blue,
    Original,
}

public class ConfiguratorMixin : ConfiguratorMixinBase
{
}
